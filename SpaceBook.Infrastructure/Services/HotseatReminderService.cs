using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SpaceBook.Application.Interfaces;
using SpaceBook.Domain.Entities;
using SpaceBook.Infrastructure.Data;

namespace SpaceBook.Infrastructure.Services;

public class HotseatReminderService : IHotseatReminderService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly INotificationRepository _notificationRepository;
    private readonly ILogger<HotseatReminderService> _logger;

    public HotseatReminderService(
        ApplicationDbContext context,
        IEmailService emailService,
        INotificationRepository notificationRepository,
        ILogger<HotseatReminderService> logger)
    {
        _context = context;
        _emailService = emailService;
        _notificationRepository = notificationRepository;
        _logger = logger;
    }

    private static TimeZoneInfo IndiaTimeZone
    {
        get
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            }
        }
    }

    public async Task ProcessHotseatRemindersAndExpirationsAsync(
        CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;
        var indiaNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, IndiaTimeZone);
        var today = DateOnly.FromDateTime(indiaNow);

        _logger.LogInformation(
            "Hotseat reminder/expiry background job running. UTC={UtcNow}, IST={IndiaNow}, Date={Today}",
            utcNow,
            indiaNow,
            today);

        // =========================================================
        // 1. PROCESS 1-HOUR CHECK-IN REMINDERS
        // =========================================================
        await ProcessRemindersAsync(today, indiaNow, cancellationToken);

        // =========================================================
        // 2. PROCESS OVERDUE BOOKINGS (AUTO-EXPIRY & SEAT RELEASE)
        // =========================================================
        await ProcessExpirationsAsync(today, utcNow, cancellationToken);
    }

    private async Task ProcessRemindersAsync(
        DateOnly today,
        DateTime indiaNow,
        CancellationToken cancellationToken)
    {
        try
        {
            var upcomingBookings = await _context.HotseatBookings
                .Include(b => b.Employee)
                .Include(b => b.Seat)
                    .ThenInclude(s => s!.Module)
                        .ThenInclude(m => m!.Office)
                            .ThenInclude(o => o!.Location)
                .Where(b => b.BookingDate == today &&
                            b.BookingStatus == "Confirmed" &&
                            b.CheckInTime == null)
                .ToListAsync(cancellationToken);

            foreach (var booking in upcomingBookings)
            {
                if (cancellationToken.IsCancellationRequested) break;

                DateTime localStartTime = booking.CheckInDeadline.HasValue
                    ? TimeZoneInfo.ConvertTimeFromUtc(booking.CheckInDeadline.Value, IndiaTimeZone)
                    : booking.BookingDate.ToDateTime(new TimeOnly(9, 0, 0));

                DateTime reminderWindowStart = localStartTime.AddHours(-1);

                // Reminder window: within 1 hour before booking start time
                if (indiaNow >= reminderWindowStart && indiaNow < localStartTime)
                {
                    // Check idempotency: check if reminder notification was already sent
                    var alreadySent = await _context.Notifications
                        .AnyAsync(n => n.HotseatBookingId == booking.HotseatBookingId &&
                                       (n.Message.StartsWith("Reminder:") ||
                                        n.Message.Contains("check-in window", StringComparison.OrdinalIgnoreCase) ||
                                        n.Message.Contains("starts at", StringComparison.OrdinalIgnoreCase)),
                                  cancellationToken);

                    if (alreadySent) continue;

                    if (booking.Employee != null && !string.IsNullOrWhiteSpace(booking.Employee.Email))
                    {
                        try
                        {
                            await _emailService.SendHotseatCheckInReminderAsync(
                                booking,
                                booking.Employee,
                                booking.Seat ?? new Seat { SeatId = booking.SeatId, SeatNumber = $"Seat {booking.SeatId}" });

                            _logger.LogInformation(
                                "Hotseat check-in reminder email sent for HotseatBookingId={BookingId} to {Email}",
                                booking.HotseatBookingId,
                                booking.Employee.Email);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(
                                ex,
                                "Failed to send hotseat reminder email for HotseatBookingId={BookingId}",
                                booking.HotseatBookingId);
                        }
                    }

                    // Create in-app notification
                    var seatNumber = booking.Seat?.SeatNumber ?? $"Seat {booking.SeatId}";
                    var moduleName = booking.Seat?.Module?.ModuleName ?? "Module";
                    var startTimeFormatted = localStartTime.ToString("hh:mm tt");

                    var notification = new Notification
                    {
                        EmployeeId = booking.EmployeeId,
                        HotseatBookingId = booking.HotseatBookingId,
                        Message = $"Reminder: Your hotseat booking for {seatNumber} in {moduleName} starts at {startTimeFormatted}. Please check in within the permitted check-in window to retain your seat.",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _notificationRepository.AddAsync(notification);
                    await _notificationRepository.SaveChangesAsync();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during hotseat reminder processing.");
        }
    }

    private async Task ProcessExpirationsAsync(
        DateOnly today,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        try
        {
            var overdueBookings = await _context.HotseatBookings
                .Include(b => b.Employee)
                .Include(b => b.Seat)
                    .ThenInclude(s => s!.Module)
                        .ThenInclude(m => m!.Office)
                            .ThenInclude(o => o!.Location)
                .Where(b => b.BookingStatus == "Confirmed" &&
                            b.CheckInTime == null &&
                            (b.BookingDate < today ||
                             (b.BookingDate == today && b.CheckInDeadline.HasValue && b.CheckInDeadline.Value <= utcNow)))
                .ToListAsync(cancellationToken);

            if (overdueBookings.Count == 0) return;

            _logger.LogInformation(
                "Found {Count} overdue hotseat bookings to expire.",
                overdueBookings.Count);

            foreach (var booking in overdueBookings)
            {
                if (cancellationToken.IsCancellationRequested) break;

                booking.BookingStatus = "Expired";
                booking.RecordModifiedBy = "System (Auto-Expired)";
                booking.RecordModifiedOn = utcNow;

                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update booking status to Expired for BookingId={BookingId}", booking.HotseatBookingId);
                    continue;
                }

                // Send Expiration Email to Employee
                if (booking.Employee != null && !string.IsNullOrWhiteSpace(booking.Employee.Email))
                {
                    try
                    {
                        await _emailService.SendHotseatBookingExpiredAsync(
                            booking,
                            booking.Employee,
                            booking.Seat ?? new Seat { SeatId = booking.SeatId, SeatNumber = $"Seat {booking.SeatId}" });

                        _logger.LogInformation(
                            "Hotseat expiration email sent for HotseatBookingId={BookingId} to {Email}",
                            booking.HotseatBookingId,
                            booking.Employee.Email);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send hotseat expiration email for HotseatBookingId={BookingId}", booking.HotseatBookingId);
                    }
                }

                // Create in-app notification
                var seatNumber = booking.Seat?.SeatNumber ?? $"Seat {booking.SeatId}";
                var moduleName = booking.Seat?.Module?.ModuleName ?? "Module";

                var notification = new Notification
                {
                    EmployeeId = booking.EmployeeId,
                    HotseatBookingId = booking.HotseatBookingId,
                    Message = $"Hotseat Booking Expired: You did not check in within the permitted time for {seatNumber} in {moduleName}. Your reservation has expired and the seat has been released.",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                try
                {
                    await _notificationRepository.AddAsync(notification);
                    await _notificationRepository.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create in-app expiration notification for HotseatBookingId={BookingId}", booking.HotseatBookingId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during hotseat expiration processing.");
        }
    }
}
