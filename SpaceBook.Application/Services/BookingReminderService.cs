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
        // SPACEBOOK TIME ZONE
        // =========================================================
        // Server/Render normally uses UTC.
        // SpaceBook booking times are stored/used as India time.
        // Therefore, convert current UTC time to IST before
        // comparing with BookingDate + StartTime/EndTime.
        // =========================================================

        var indiaTimeZone = GetIndiaTimeZone();

        var utcNow = DateTime.UtcNow;

        var now = TimeZoneInfo.ConvertTimeFromUtc(
            utcNow,
            indiaTimeZone);

        // Explicitly remove any DateTime kind ambiguity.
        // BookingDate + StartTime are treated as India local time.
        now = DateTime.SpecifyKind(
            now,
            DateTimeKind.Unspecified);

        var today = DateOnly.FromDateTime(now);

        _logger.LogInformation(
            "Booking reminder check running. UTC: {UtcNow}, India Time: {IndiaNow}, Date: {Today}",
            utcNow,
            now,
            today);

        // =========================================================
        // GET TODAY'S BOOKINGS
        // =========================================================

        var bookings = await _reminderRepository
            .GetTodayBookingsNeedingRemindersAsync(
                today,
                cancellationToken);

        if (bookings == null || bookings.Count == 0)
        {
            _logger.LogInformation(
                "No bookings found for reminder evaluation on {Today}.",
                today);

            return;
        }

        // =========================================================
        // GET ADMIN EMAILS
        // =========================================================

        var adminEmails = await _reminderRepository
            .GetAdminEmailsAsync(cancellationToken);

        _logger.LogInformation(
            "Found {BookingCount} booking(s) for reminder evaluation. " +
            "Admin recipients: {AdminCount}.",
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
                    "Skipping Booking {BookingId} because status is {Status}.",
                    booking.BookingId,
                    booking.Status);

                continue;
            }

            // =====================================================
            // ROOM DETAILS
            // =====================================================

            var roomName = booking.Room != null
                ? (!string.IsNullOrWhiteSpace(booking.Room.RoomName)
                    ? booking.Room.RoomName
                    : booking.Room.RoomNumber)
                : "Meeting Room";

            if (string.IsNullOrWhiteSpace(roomName))
            {
                roomName = "Meeting Room";
            }

            // =====================================================
            // MEETING TITLE
            // =====================================================

            var meetingTitle =
                !string.IsNullOrWhiteSpace(booking.MeetingTitle)
                    ? booking.MeetingTitle
                    : "Room Booking";

            // =====================================================
            // EMPLOYEE EMAIL
            // =====================================================

            var employeeEmail = booking.Employee?.Email;

            if (string.IsNullOrWhiteSpace(employeeEmail))
            {
                _logger.LogWarning(
                    "Booking {BookingId} has no employee email. " +
                    "Reminder email may not be sent.",
                    booking.BookingId);
            }

            // =====================================================
            // CREATE BOOKING START/END DATETIME
            // =====================================================
            // BookingDate + StartTime/EndTime represent India local
            // time in SpaceBook.
            //
            // Explicitly use DateTimeKind.Unspecified so that these
            // values are not accidentally interpreted as UTC.
            // =====================================================

            var bookingStartDateTime =
                DateTime.SpecifyKind(
                    booking.BookingDate.ToDateTime(booking.StartTime),
                    DateTimeKind.Unspecified);

            var bookingEndDateTime =
                DateTime.SpecifyKind(
                    booking.BookingDate.ToDateTime(booking.EndTime),
                    DateTimeKind.Unspecified);

            // =====================================================
            // CALCULATE TIME REMAINING
            // =====================================================

            var timeUntilStart =
                bookingStartDateTime - now;

            var timeUntilEnd =
                bookingEndDateTime - now;

            _logger.LogInformation(
                "Evaluating Booking {BookingId}: " +
                "Start={Start}, End={End}, " +
                "MinutesUntilStart={StartMinutes:F2}, " +
                "MinutesUntilEnd={EndMinutes:F2}, " +
                "EmployeeEmail={EmployeeEmail}",
                booking.BookingId,
                bookingStartDateTime,
                bookingEndDateTime,
                timeUntilStart.TotalMinutes,
                timeUntilEnd.TotalMinutes,
                employeeEmail ?? "NULL");

            // =====================================================
            // 1. START REMINDER
            // =====================================================
            // Send once when the booking is within 15 minutes of
            // starting.
            //
            // Example:
            // Booking = 5:00 PM
            // Reminder window = 4:45 PM to 5:00 PM
            // =====================================================

            var hasStartNotification =
                await _reminderRepository
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
                    "15-minute START reminder triggered for Booking {BookingId}.",
                    booking.BookingId);

                // =================================================
                // CREATE IN-APP NOTIFICATION
                // =================================================

                var startNotification = new Notification
                {
                    EmployeeId = booking.EmployeeId,
                    BookingId = booking.BookingId,

                    Message =
                        $"Reminder: Your booking '{meetingTitle}' " +
                        $"in {roomName} starts in approximately " +
                        $"15 minutes at {booking.StartTime:hh\\:mm tt}.",

                    IsRead = false,

                    // Database timestamptz should receive UTC.
                    CreatedAt = DateTime.UtcNow
                };

                await _notificationRepository.AddAsync(
                    startNotification);

                // =================================================
                // SEND EMAIL
                // =================================================

                try
                {
                    var employee =
                        booking.Employee
                        ?? new Employee
                        {
                            Name = "Colleague",
                            Email = employeeEmail ?? string.Empty
                        };

                    var room =
                        booking.Room
                        ?? new Room
                        {
                            RoomName = roomName
                        };

                    await _emailService.SendBookingStartReminderAsync(
                        booking,
                        employee,
                        room,
                        adminEmails);

                    // =================================================
                    // RECORD SUCCESSFUL EMAIL
                    // =================================================

                    await _reminderRepository
                        .RecordNotificationSentAsync(
                            booking.BookingId,
                            BookingNotificationType.StartReminder15Minutes,
                            "Sent",
                            cancellationToken);

                    // =================================================
                    // MARK BOOKING FLAG
                    // =================================================

                    booking.StartReminderSent = true;

                    stateChanged = true;

                    _logger.LogInformation(
                        "15-minute START reminder successfully sent " +
                        "for Booking {BookingId}.",
                        booking.BookingId);
                }
                catch (Exception ex)
                {
                    // IMPORTANT:
                    // Do NOT mark the reminder as sent when email fails.
                    // The next scheduler execution can retry it.
                    _logger.LogError(
                        ex,
                        "Failed to send 15-minute START reminder " +
                        "for Booking {BookingId}.",
                        booking.BookingId);
                }
            }

            // =====================================================
            // 2. END REMINDER
            // =====================================================
            // Send once when the booking is within 15 minutes of
            // ending.
            //
            // Example:
            // Booking ends = 6:00 PM
            // Reminder window = 5:45 PM to 6:00 PM
            // =====================================================

            var hasEndNotification =
                await _reminderRepository
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
                    "15-minute END reminder triggered for Booking {BookingId}.",
                    booking.BookingId);

                // =================================================
                // CREATE IN-APP NOTIFICATION
                // =================================================

                var endNotification = new Notification
                {
                    EmployeeId = booking.EmployeeId,
                    BookingId = booking.BookingId,

                    Message =
                        $"Reminder: Your booking '{meetingTitle}' " +
                        $"in {roomName} will end in approximately " +
                        $"15 minutes at {booking.EndTime:hh\\:mm tt}.",

                    IsRead = false,

                    // Database timestamptz should receive UTC.
                    CreatedAt = DateTime.UtcNow
                };

                await _notificationRepository.AddAsync(
                    endNotification);

                // =================================================
                // SEND EMAIL
                // =================================================

                try
                {
                    var employee =
                        booking.Employee
                        ?? new Employee
                        {
                            Name = "Colleague",
                            Email = employeeEmail ?? string.Empty
                        };

                    var room =
                        booking.Room
                        ?? new Room
                        {
                            RoomName = roomName
                        };

                    await _emailService.SendBookingEndReminderAsync(
                        booking,
                        employee,
                        room,
                        adminEmails);

                    // =================================================
                    // RECORD SUCCESSFUL EMAIL
                    // =================================================

                    await _reminderRepository
                        .RecordNotificationSentAsync(
                            booking.BookingId,
                            BookingNotificationType.EndReminder15Minutes,
                            "Sent",
                            cancellationToken);

                    // =================================================
                    // MARK BOOKING FLAG
                    // =================================================

                    booking.EndReminderSent = true;

                    stateChanged = true;

                    _logger.LogInformation(
                        "15-minute END reminder successfully sent " +
                        "for Booking {BookingId}.",
                        booking.BookingId);
                }
                catch (Exception ex)
                {
                    // IMPORTANT:
                    // Do NOT mark as sent when email fails.
                    _logger.LogError(
                        ex,
                        "Failed to send 15-minute END reminder " +
                        "for Booking {BookingId}.",
                        booking.BookingId);
                }
            }
        }

        // =========================================================
        // SAVE BOOKING REMINDER STATE
        // =========================================================

        if (stateChanged)
        {
            await _reminderRepository
                .SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Booking reminder state changes saved successfully.");
        }
        else
        {
            _logger.LogDebug(
                "No booking reminder state changes required.");
        }
    }

    // =============================================================
    // INDIA TIME ZONE
    // =============================================================

    private static TimeZoneInfo GetIndiaTimeZone()
    {
        // Linux / Render
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(
                "Asia/Kolkata");
        }
        catch (TimeZoneNotFoundException)
        {
            // Windows
            return TimeZoneInfo.FindSystemTimeZoneById(
                "India Standard Time");
        }
        catch (InvalidTimeZoneException)
        {
            throw;
        }
    }
}