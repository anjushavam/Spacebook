using Microsoft.Extensions.Logging;
using SpaceBook.Application.Interfaces;
using SpaceBook.Domain.Entities;

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

        var indiaTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");

        var now =
            TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                indiaTimeZone);

        var today = DateOnly.FromDateTime(now);

        _logger.LogInformation(
            "Booking reminder check running. India Time: {Now}, Date: {Today}",
            now,
            today);

        // =========================================================
        // GET BOOKINGS
        // =========================================================

        var bookings =
            await _reminderRepository
                .GetTodayBookingsNeedingRemindersAsync(
                    today,
                    cancellationToken);

        if (bookings == null || bookings.Count == 0)
        {
            _logger.LogInformation(
                "No bookings requiring reminders found for {Today}.",
                today);

            return;
        }

        _logger.LogInformation(
            "Found {Count} booking(s) requiring reminder evaluation.",
            bookings.Count);

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

            var roomName =
                booking.Room != null
                    ? !string.IsNullOrWhiteSpace(
                        booking.Room.RoomName)
                        ? booking.Room.RoomName
                        : booking.Room.RoomNumber
                    : "Meeting Room";

            var roomNumber =
                booking.Room?.RoomNumber ?? string.Empty;

            var meetingTitle =
                !string.IsNullOrWhiteSpace(
                    booking.MeetingTitle)
                    ? booking.MeetingTitle
                    : "Room Booking";

            var employeeName =
                booking.Employee?.Name ?? "Colleague";

            var employeeEmail =
                booking.Employee?.Email;

            // =====================================================
            // CREATE ACTUAL START / END DATE TIME
            // =====================================================

            var bookingStartDateTime =
                booking.BookingDate.ToDateTime(
                    booking.StartTime);

            var bookingEndDateTime =
                booking.BookingDate.ToDateTime(
                    booking.EndTime);

            var timeUntilStart =
                bookingStartDateTime - now;

            var timeUntilEnd =
                bookingEndDateTime - now;

            _logger.LogInformation(
                "Booking {BookingId}: Start={Start}, End={End}, " +
                "MinutesUntilStart={StartMinutes:F2}, " +
                "MinutesUntilEnd={EndMinutes:F2}, " +
                "StartSent={StartSent}, EndSent={EndSent}, " +
                "EmployeeEmail={EmployeeEmail}",
                booking.BookingId,
                bookingStartDateTime,
                bookingEndDateTime,
                timeUntilStart.TotalMinutes,
                timeUntilEnd.TotalMinutes,
                booking.StartReminderSent,
                booking.EndReminderSent,
                employeeEmail ?? "NULL");

            // =========================================================
            // START REMINDER
            // =========================================================

            if (!booking.StartReminderSent &&
                timeUntilStart.TotalMinutes > 0 &&
                timeUntilStart.TotalMinutes <= 15)
            {
                _logger.LogInformation(
                    "Start reminder triggered for Booking {BookingId}.",
                    booking.BookingId);

                // -------------------------------------------------
                // CREATE IN-APP NOTIFICATION
                // -------------------------------------------------

                var startNotification =
                    new Notification
                    {
                        EmployeeId = booking.EmployeeId,
                        BookingId = booking.BookingId,

                        Message =
                            $"Reminder: Your booking " +
                            $"'{meetingTitle}' in {roomName} " +
                            $"starts in approximately 15 minutes " +
                            $"at {booking.StartTime:hh\\:mm tt}.",

                        IsRead = false,

                        CreatedAt = DateTime.UtcNow
                    };

                await _notificationRepository
                    .AddAsync(startNotification);

                // -------------------------------------------------
                // EMAIL
                // -------------------------------------------------

                if (string.IsNullOrWhiteSpace(employeeEmail))
                {
                    _logger.LogWarning(
                        "Booking {BookingId} has no employee email. " +
                        "Start reminder email was not sent.",
                        booking.BookingId);
                }
                else
                {
                    try
                    {
                        var subject =
                            $"SpaceBook Reminder: {meetingTitle} starts soon";

                        var body =
                            BuildStartReminderEmailHtml(
                                employeeName,
                                meetingTitle,
                                roomName,
                                roomNumber,
                                booking.BookingDate,
                                booking.StartTime,
                                booking.EndTime,
                                booking.ParticipantCount);

                        await _emailService.SendEmailAsync(
                            employeeEmail,
                            subject,
                            body,
                            true);

                        // IMPORTANT:
                        // Only mark true AFTER successful email
                        booking.StartReminderSent = true;

                        stateChanged = true;

                        _logger.LogInformation(
                            "Start reminder email sent successfully. " +
                            "BookingId={BookingId}, Email={Email}",
                            booking.BookingId,
                            employeeEmail);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Start reminder email failed. " +
                            "BookingId={BookingId}, Email={Email}. " +
                            "StartReminderSent remains false.",
                            booking.BookingId,
                            employeeEmail);

                        // DO NOT mark reminder as sent
                    }
                }
            }

            // =========================================================
            // END REMINDER
            // =========================================================

            if (!booking.EndReminderSent &&
                timeUntilEnd.TotalMinutes > 0 &&
                timeUntilEnd.TotalMinutes <= 15)
            {
                _logger.LogInformation(
                    "End reminder triggered for Booking {BookingId}.",
                    booking.BookingId);

                // -------------------------------------------------
                // CREATE IN-APP NOTIFICATION
                // -------------------------------------------------

                var endNotification =
                    new Notification
                    {
                        EmployeeId = booking.EmployeeId,
                        BookingId = booking.BookingId,

                        Message =
                            $"Reminder: Your booking " +
                            $"'{meetingTitle}' in {roomName} " +
                            $"will end in approximately 15 minutes " +
                            $"at {booking.EndTime:hh\\:mm tt}.",

                        IsRead = false,

                        CreatedAt = DateTime.UtcNow
                    };

                await _notificationRepository
                    .AddAsync(endNotification);

                // -------------------------------------------------
                // EMAIL
                // -------------------------------------------------

                if (string.IsNullOrWhiteSpace(employeeEmail))
                {
                    _logger.LogWarning(
                        "Booking {BookingId} has no employee email. " +
                        "End reminder email was not sent.",
                        booking.BookingId);
                }
                else
                {
                    try
                    {
                        var subject =
                            $"SpaceBook Reminder: {meetingTitle} ends soon";

                        var body =
                            BuildEndReminderEmailHtml(
                                employeeName,
                                meetingTitle,
                                roomName,
                                roomNumber,
                                booking.BookingDate,
                                booking.EndTime);

                        await _emailService.SendEmailAsync(
                            employeeEmail,
                            subject,
                            body,
                            true);

                        booking.EndReminderSent = true;

                        stateChanged = true;

                        _logger.LogInformation(
                            "End reminder email sent successfully. " +
                            "BookingId={BookingId}, Email={Email}",
                            booking.BookingId,
                            employeeEmail);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "End reminder email failed. " +
                            "BookingId={BookingId}, Email={Email}. " +
                            "EndReminderSent remains false.",
                            booking.BookingId,
                            employeeEmail);
                    }
                }
            }
        }

        // =========================================================
        // SAVE FLAGS
        // =========================================================

        if (stateChanged)
        {
            await _reminderRepository
                .SaveChangesAsync(
                    cancellationToken);

            _logger.LogInformation(
                "Reminder status changes saved successfully.");
        }
    }

    // =============================================================
    // START REMINDER EMAIL
    // =============================================================

    private static string BuildStartReminderEmailHtml(
        string employeeName,
        string meetingTitle,
        string roomName,
        string roomNumber,
        DateOnly bookingDate,
        TimeOnly startTime,
        TimeOnly endTime,
        int participantCount)
    {
        return $"""
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <title>Meeting Starting Soon</title>
        </head>

        <body style="
            font-family: Arial, sans-serif;
            background-color: #f4f6f9;
            padding: 24px;
            color: #1e293b;">

            <table
                align="center"
                width="100%"
                cellpadding="0"
                cellspacing="0"
                style="
                    max-width:580px;
                    background:#ffffff;
                    border-radius:12px;">

                <tr>
                    <td style="
                        background:#2563eb;
                        padding:28px;
                        text-align:center;
                        color:white;">

                        <h1 style="margin:0;">
                            SpaceBook
                        </h1>

                        <p>
                            Room Booking Reminder
                        </p>

                    </td>
                </tr>

                <tr>
                    <td style="padding:28px;">

                        <h2>
                            Hello {employeeName},
                        </h2>

                        <p>
                            Your scheduled room booking will start
                            in approximately
                            <strong>15 minutes</strong>.
                        </p>

                        <table
                            width="100%"
                            cellpadding="8"
                            style="
                                background:#f8fafc;
                                border:1px solid #e2e8f0;
                                border-radius:8px;">

                            <tr>
                                <td><strong>Meeting</strong></td>
                                <td>{meetingTitle}</td>
                            </tr>

                            <tr>
                                <td><strong>Room</strong></td>
                                <td>
                                    {roomName}
                                    {(string.IsNullOrWhiteSpace(roomNumber)
                                        ? ""
                                        : $" ({roomNumber})")}
                                </td>
                            </tr>

                            <tr>
                                <td><strong>Date</strong></td>
                                <td>
                                    {bookingDate:MMMM dd, yyyy}
                                </td>
                            </tr>

                            <tr>
                                <td><strong>Start</strong></td>
                                <td>
                                    {startTime:hh\\:mm tt}
                                </td>
                            </tr>

                            <tr>
                                <td><strong>End</strong></td>
                                <td>
                                    {endTime:hh\\:mm tt}
                                </td>
                            </tr>

                            <tr>
                                <td><strong>Attendees</strong></td>
                                <td>{participantCount}</td>
                            </tr>

                        </table>

                        <p>
                            Please arrive on time and check in
                            when entering the room.
                        </p>

                    </td>
                </tr>

                <tr>
                    <td style="
                        text-align:center;
                        padding:18px;
                        background:#f1f5f9;
                        color:#64748b;
                        font-size:12px;">

                        This is an automated notification
                        from SpaceBook.

                    </td>
                </tr>

            </table>
        </body>
        </html>
        """;
    }

    // =============================================================
    // END REMINDER EMAIL
    // =============================================================

    private static string BuildEndReminderEmailHtml(
        string employeeName,
        string meetingTitle,
        string roomName,
        string roomNumber,
        DateOnly bookingDate,
        TimeOnly endTime)
    {
        return $"""
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <title>Meeting Ending Soon</title>
        </head>

        <body style="
            font-family:Arial,sans-serif;
            background:#f4f6f9;
            padding:24px;">

            <table
                align="center"
                width="100%"
                cellpadding="0"
                cellspacing="0"
                style="
                    max-width:580px;
                    background:#ffffff;
                    border-radius:12px;">

                <tr>
                    <td style="
                        background:#d97706;
                        padding:28px;
                        text-align:center;
                        color:white;">

                        <h1>SpaceBook</h1>

                        <p>
                            Meeting Wrap-up Reminder
                        </p>

                    </td>
                </tr>

                <tr>
                    <td style="padding:28px;">

                        <h2>
                            Hello {employeeName},
                        </h2>

                        <p>
                            Your meeting
                            <strong>{meetingTitle}</strong>
                            in
                            <strong>{roomName}</strong>
                            will end in approximately
                            <strong>15 minutes</strong>.
                        </p>

                        <p>
                            <strong>Date:</strong>
                            {bookingDate:MMMM dd, yyyy}
                        </p>

                        <p>
                            <strong>End Time:</strong>
                            {endTime:hh\\:mm tt}
                        </p>

                        <p>
                            Please prepare to conclude your
                            meeting and leave the room ready
                            for the next booking.
                        </p>

                    </td>
                </tr>

                <tr>
                    <td style="
                        text-align:center;
                        padding:18px;
                        background:#f1f5f9;
                        color:#64748b;
                        font-size:12px;">

                        This is an automated notification
                        from SpaceBook.

                    </td>
                </tr>

            </table>

        </body>
        </html>
        """;
    }
}