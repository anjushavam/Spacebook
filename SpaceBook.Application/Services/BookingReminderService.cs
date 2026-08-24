using Microsoft.Extensions.Logging;
using SpaceBook.Application.Interfaces;
using SpaceBook.Domain.Entities;
using SpaceBook.Domain.Enums;

namespace SpaceBook.Application.Services;

public class BookingReminderService : IBookingReminderService
{
    private readonly IBookingReminderRepository _reminderRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IEmailService _emailService;
    private readonly ILogger<BookingReminderService> _logger;

    public BookingReminderService(
        IBookingReminderRepository reminderRepository,
        INotificationRepository notificationRepository,
        IEmailService emailService,
        ILogger<BookingReminderService> logger)
    {
        _reminderRepository = reminderRepository;
        _notificationRepository = notificationRepository;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task ProcessBookingRemindersAsync(
        CancellationToken cancellationToken = default)
    {
        // =========================================================
        // IMPORTANT:
        // Render server may run in UTC.
        // SpaceBook booking times are India time.
        // =========================================================

        var indiaTimeZone = GetIndiaTimeZone();

        var now = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.UtcNow,
            indiaTimeZone);

        var today = DateOnly.FromDateTime(now);

        _logger.LogInformation(
            "Booking reminder check running. India Time: {Now}, Date: {Today}",
            now,
            today);

        // =========================================================
        // GET BOOKINGS & ADMIN EMAILS
        // =========================================================

        var bookings = await _reminderRepository
            .GetTodayBookingsNeedingRemindersAsync(
                today,
                cancellationToken);

        if (bookings == null || bookings.Count == 0)
        {
            _logger.LogInformation(
                "No approved bookings found for {Today}.",
                today);
            return;
        }

        var adminEmails = await _reminderRepository
            .GetAdminEmailsAsync(cancellationToken);

        _logger.LogInformation(
            "Found {Count} approved booking(s) for reminder evaluation. Admin recipients: {AdminCount}",
            bookings.Count,
            adminEmails.Count);

        var stateChanged = false;

        // =========================================================
        // PROCESS EACH BOOKING
        // =========================================================

        foreach (var booking in bookings)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            // Only Approved bookings receive reminders
            if (!string.Equals(booking.Status, "Approved", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var roomName = booking.Room != null
                ? (!string.IsNullOrWhiteSpace(booking.Room.RoomName) ? booking.Room.RoomName : booking.Room.RoomNumber)
                : "Meeting Room";

            var meetingTitle = !string.IsNullOrWhiteSpace(booking.MeetingTitle)
                ? booking.MeetingTitle
                : "Room Booking";

            var employeeEmail = booking.Employee?.Email;

            // =====================================================
            // CREATE ACTUAL START / END DATE TIME
            // =====================================================

            var bookingStartDateTime = booking.BookingDate.ToDateTime(booking.StartTime);
            var bookingEndDateTime = booking.BookingDate.ToDateTime(booking.EndTime);

            var timeUntilStart = bookingStartDateTime - now;
            var timeUntilEnd = bookingEndDateTime - now;

            _logger.LogInformation(
                "Evaluating Booking {BookingId}: Start={Start}, End={End}, " +
                "MinsUntilStart={StartMins:F2}, MinsUntilEnd={EndMins:F2}, " +
                "EmployeeEmail={Email}",
                booking.BookingId,
                bookingStartDateTime,
                bookingEndDateTime,
                timeUntilStart.TotalMinutes,
                timeUntilEnd.TotalMinutes,
                employeeEmail ?? "NULL");

            // =========================================================
            // 1. START REMINDER (15 MINUTES BEFORE START TIME)
            // =========================================================

            var hasStartNotification = await _reminderRepository
                .HasNotificationBeenSentAsync(
                    booking.BookingId,
                    BookingNotificationType.StartReminder15Minutes,
                    cancellationToken)
                || booking.StartReminderSent;

            if (!hasStartNotification &&
                timeUntilStart.TotalMinutes > 0 &&
                timeUntilStart.TotalMinutes <= 15)
            {
                _logger.LogInformation(
                    "15-minute start reminder triggered for Booking {BookingId}.",
                    booking.BookingId);

                // In-App Notification for Employee
                var startNotification = new Notification
                {
                    EmployeeId = booking.EmployeeId,
                    BookingId = booking.BookingId,
                    Message =
                        $"Reminder: Your booking '{meetingTitle}' in {roomName} " +
                        $"starts in approximately 15 minutes at {booking.StartTime:hh\\:mm tt}.",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                await _notificationRepository.AddAsync(startNotification);

                // Dispatch Start Reminder Email to Employee + Admins
                try
                {
                    await _emailService.SendBookingStartReminderAsync(
                        booking,
                        booking.Employee ?? new Employee { Name = "Colleague", Email = employeeEmail ?? string.Empty },
                        booking.Room ?? new Room { RoomName = roomName },
                        adminEmails);

                    // Record in tracking table for duplicate prevention
                    await _reminderRepository.RecordNotificationSentAsync(
                        booking.BookingId,
                        BookingNotificationType.StartReminder15Minutes,
                        "Sent",
                        cancellationToken);

                    booking.StartReminderSent = true;
                    stateChanged = true;

                    _logger.LogInformation(
                        "Start reminder emails dispatched and recorded for Booking {BookingId}.",
                        booking.BookingId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Start reminder email dispatch failed for Booking {BookingId}.",
                        booking.BookingId);
                }
            }

            // =========================================================
            // 2. END REMINDER (15 MINUTES BEFORE END TIME)
            // =========================================================

            var hasEndNotification = await _reminderRepository
                .HasNotificationBeenSentAsync(
                    booking.BookingId,
                    BookingNotificationType.EndReminder15Minutes,
                    cancellationToken)
                || booking.EndReminderSent;

            if (!hasEndNotification &&
                timeUntilEnd.TotalMinutes > 0 &&
                timeUntilEnd.TotalMinutes <= 15)
            {
                _logger.LogInformation(
                    "15-minute end reminder triggered for Booking {BookingId}.",
                    booking.BookingId);

                // In-App Notification for Employee
                var endNotification = new Notification
                {
                    EmployeeId = booking.EmployeeId,
                    BookingId = booking.BookingId,
                    Message =
                        $"Reminder: Your booking '{meetingTitle}' in {roomName} " +
                        $"will end in approximately 15 minutes at {booking.EndTime:hh\\:mm tt}.",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                await _notificationRepository.AddAsync(endNotification);

                // Dispatch End Reminder Email to Employee + Admins
                try
                {
                    await _emailService.SendBookingEndReminderAsync(
                        booking,
                        booking.Employee ?? new Employee { Name = "Colleague", Email = employeeEmail ?? string.Empty },
                        booking.Room ?? new Room { RoomName = roomName },
                        adminEmails);

                    // Record in tracking table for duplicate prevention
                    await _reminderRepository.RecordNotificationSentAsync(
                        booking.BookingId,
                        BookingNotificationType.EndReminder15Minutes,
                        "Sent",
                        cancellationToken);

                    booking.EndReminderSent = true;
                    stateChanged = true;

                    _logger.LogInformation(
                        "End reminder emails dispatched and recorded for Booking {BookingId}.",
                        booking.BookingId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "End reminder email dispatch failed for Booking {BookingId}.",
                        booking.BookingId);
                }
            }
        }

        // =========================================================
        // SAVE CHANGES
        // =========================================================

        if (stateChanged)
        {
            await _reminderRepository.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Reminder state changes saved successfully.");
        }
    }

    private static TimeZoneInfo GetIndiaTimeZone()
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