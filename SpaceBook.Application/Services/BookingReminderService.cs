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
        // 1. GET CURRENT INDIA TIME
        // =========================================================

        var indiaTimeZone = GetIndiaTimeZone();

        var utcNow = DateTime.UtcNow;

        var indiaNow = TimeZoneInfo.ConvertTimeFromUtc(
            utcNow,
            indiaTimeZone);

        // BookingDate + StartTime/EndTime are local India values.
        var now = DateTime.SpecifyKind(
            indiaNow,
            DateTimeKind.Unspecified);

        var today = DateOnly.FromDateTime(now);

        _logger.LogInformation(
            "Booking reminder service running. UTC={UtcNow}, IST={IndiaNow}, Date={Today}",
            utcNow,
            now,
            today);

        // =========================================================
        // 2. GET TODAY'S BOOKINGS NEEDING REMINDERS
        // =========================================================

        var bookings = await _reminderRepository
            .GetTodayBookingsNeedingRemindersAsync(
                today,
                cancellationToken);

        if (bookings == null || bookings.Count == 0)
        {
            _logger.LogInformation(
                "No approved bookings require reminder evaluation for {Today}.",
                today);

            return;
        }

        // =========================================================
        // 3. GET ADMIN EMAILS
        // =========================================================

        var adminEmails = await _reminderRepository
            .GetAdminEmailsAsync(cancellationToken);

        adminEmails ??= new List<string>();

        _logger.LogInformation(
            "Found {BookingCount} booking(s). Admin email count={AdminCount}.",
            bookings.Count,
            adminEmails.Count);

        var stateChanged = false;

        // =========================================================
        // 4. PROCESS BOOKINGS
        // =========================================================

        foreach (var booking in bookings)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation(
                    "Booking reminder processing cancelled.");

                break;
            }

            // =====================================================
            // ONLY APPROVED BOOKINGS
            // =====================================================

            if (!string.Equals(
                    booking.Status,
                    "Approved",
                    StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug(
                    "Skipping BookingId={BookingId}. Status={Status}",
                    booking.BookingId,
                    booking.Status);

                continue;
            }

            // =====================================================
            // EMPLOYEE VALIDATION
            // =====================================================

            if (booking.Employee == null)
            {
                _logger.LogWarning(
                    "Skipping BookingId={BookingId}. Employee data was not loaded.",
                    booking.BookingId);

                continue;
            }

            if (string.IsNullOrWhiteSpace(booking.Employee.Email))
            {
                _logger.LogWarning(
                    "Skipping email reminder for BookingId={BookingId}. " +
                    "Employee {EmployeeId} does not have an email address.",
                    booking.BookingId,
                    booking.EmployeeId);

                continue;
            }

            // =====================================================
            // ROOM DETAILS
            // =====================================================

            var room = booking.Room;

            if (room == null)
            {
                _logger.LogWarning(
                    "BookingId={BookingId} has no Room navigation data.",
                    booking.BookingId);

                room = new Room
                {
                    RoomName = "Meeting Room"
                };
            }

            var roomName =
                !string.IsNullOrWhiteSpace(room.RoomName)
                    ? room.RoomName
                    : !string.IsNullOrWhiteSpace(room.RoomNumber)
                        ? room.RoomNumber
                        : "Meeting Room";

            // =====================================================
            // MEETING TITLE
            // =====================================================

            var meetingTitle =
                !string.IsNullOrWhiteSpace(booking.MeetingTitle)
                    ? booking.MeetingTitle
                    : "Room Booking";

            // =====================================================
            // CREATE LOCAL BOOKING DATE/TIME
            // =====================================================

            var bookingStartDateTime =
                DateTime.SpecifyKind(
                    booking.BookingDate.ToDateTime(
                        booking.StartTime),
                    DateTimeKind.Unspecified);

            var bookingEndDateTime =
                DateTime.SpecifyKind(
                    booking.BookingDate.ToDateTime(
                        booking.EndTime),
                    DateTimeKind.Unspecified);

            var minutesUntilStart =
                (bookingStartDateTime - now).TotalMinutes;

            var minutesUntilEnd =
                (bookingEndDateTime - now).TotalMinutes;

            _logger.LogInformation(
                "BookingId={BookingId}, Start={Start}, End={End}, " +
                "MinutesUntilStart={MinutesUntilStart:F2}, " +
                "MinutesUntilEnd={MinutesUntilEnd:F2}, " +
                "Email={Email}",
                booking.BookingId,
                bookingStartDateTime,
                bookingEndDateTime,
                minutesUntilStart,
                minutesUntilEnd,
                booking.Employee.Email);

            // =====================================================
            // 5. START REMINDER
            // =====================================================

            var startAlreadySent =
                booking.StartReminderSent ||
                await _reminderRepository
                    .HasNotificationBeenSentAsync(
                        booking.BookingId,
                        BookingNotificationType.StartReminder15Minutes,
                        cancellationToken);

            if (!startAlreadySent &&
                minutesUntilStart > 0 &&
                minutesUntilStart <= 15)
            {
                var startSuccess =
                    await ProcessStartReminderAsync(
                        booking,
                        booking.Employee,
                        room,
                        roomName,
                        meetingTitle,
                        adminEmails,
                        cancellationToken);

                if (startSuccess)
                {
                    booking.StartReminderSent = true;
                    stateChanged = true;
                }
            }

            // =====================================================
            // 6. END REMINDER
            // =====================================================

            var endAlreadySent =
                booking.EndReminderSent ||
                await _reminderRepository
                    .HasNotificationBeenSentAsync(
                        booking.BookingId,
                        BookingNotificationType.EndReminder15Minutes,
                        cancellationToken);

            if (!endAlreadySent &&
                minutesUntilEnd > 0 &&
                minutesUntilEnd <= 15)
            {
                var endSuccess =
                    await ProcessEndReminderAsync(
                        booking,
                        booking.Employee,
                        room,
                        roomName,
                        meetingTitle,
                        adminEmails,
                        cancellationToken);

                if (endSuccess)
                {
                    booking.EndReminderSent = true;
                    stateChanged = true;
                }
            }
        }

        // =========================================================
        // 7. SAVE BOOKING FLAGS
        // =========================================================

        if (stateChanged)
        {
            await _reminderRepository
                .SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Booking reminder flags saved successfully.");
        }
        else
        {
            _logger.LogDebug(
                "No booking reminder flags required updating.");
        }
    }

    // =============================================================
    // START REMINDER
    // =============================================================

    private async Task<bool> ProcessStartReminderAsync(
        Booking booking,
        Employee employee,
        Room room,
        string roomName,
        string meetingTitle,
        List<string> adminEmails,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "START reminder triggered for BookingId={BookingId}.",
                booking.BookingId);

            // =====================================================
            // SEND EMAIL FIRST
            // =====================================================

            await _emailService.SendBookingStartReminderAsync(
                booking,
                employee,
                room,
                adminEmails);

            _logger.LogInformation(
                "START reminder email sent successfully for BookingId={BookingId}.",
                booking.BookingId);

            // =====================================================
            // CREATE IN-APP NOTIFICATION
            // =====================================================

            var notification = new Notification
            {
                EmployeeId = booking.EmployeeId,
                BookingId = booking.BookingId,

                Message =
                    $"Reminder: Your booking '{meetingTitle}' " +
                    $"in {roomName} starts in approximately " +
                    $"15 minutes at {FormatTime(booking.StartTime)}.",

                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _notificationRepository
                .AddAsync(notification);

            // =====================================================
            // RECORD EMAIL HISTORY
            // =====================================================

            await _reminderRepository
                .RecordNotificationSentAsync(
                    booking.BookingId,
                    BookingNotificationType.StartReminder15Minutes,
                    "Sent",
                    cancellationToken);

            _logger.LogInformation(
                "START reminder completed successfully for BookingId={BookingId}.",
                booking.BookingId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "START reminder failed for BookingId={BookingId}. " +
                "It will be retried while the booking remains inside " +
                "the 15-minute reminder window.",
                booking.BookingId);

            return false;
        }
    }

    // =============================================================
    // END REMINDER
    // =============================================================

    private async Task<bool> ProcessEndReminderAsync(
        Booking booking,
        Employee employee,
        Room room,
        string roomName,
        string meetingTitle,
        List<string> adminEmails,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "END reminder triggered for BookingId={BookingId}.",
                booking.BookingId);

            // =====================================================
            // SEND EMAIL FIRST
            // =====================================================

            await _emailService.SendBookingEndReminderAsync(
                booking,
                employee,
                room,
                adminEmails);

            _logger.LogInformation(
                "END reminder email sent successfully for BookingId={BookingId}.",
                booking.BookingId);

            // =====================================================
            // CREATE IN-APP NOTIFICATION
            // =====================================================

            var notification = new Notification
            {
                EmployeeId = booking.EmployeeId,
                BookingId = booking.BookingId,

                Message =
                    $"Reminder: Your booking '{meetingTitle}' " +
                    $"in {roomName} will end in approximately " +
                    $"15 minutes at {FormatTime(booking.EndTime)}.",

                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _notificationRepository
                .AddAsync(notification);

            // =====================================================
            // RECORD EMAIL HISTORY
            // =====================================================

            await _reminderRepository
                .RecordNotificationSentAsync(
                    booking.BookingId,
                    BookingNotificationType.EndReminder15Minutes,
                    "Sent",
                    cancellationToken);

            _logger.LogInformation(
                "END reminder completed successfully for BookingId={BookingId}.",
                booking.BookingId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "END reminder failed for BookingId={BookingId}. " +
                "It will be retried while the booking remains inside " +
                "the 15-minute reminder window.",
                booking.BookingId);

            return false;
        }
    }

    // =============================================================
    // FORMAT TIME
    // =============================================================

    private static string FormatTime(TimeOnly time)
    {
        return DateTime.Today
            .Add(time.ToTimeSpan())
            .ToString("hh:mm tt");
    }

    // =============================================================
    // INDIA TIME ZONE
    // =============================================================

    private static TimeZoneInfo GetIndiaTimeZone()
    {
        try
        {
            // Linux / Render
            return TimeZoneInfo.FindSystemTimeZoneById(
                "Asia/Kolkata");
        }
        catch (TimeZoneNotFoundException)
        {
            // Windows
            return TimeZoneInfo.FindSystemTimeZoneById(
                "India Standard Time");
        }
    }
}