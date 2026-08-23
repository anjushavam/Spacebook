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

    public async Task ProcessBookingRemindersAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.Now;
        var today = DateOnly.FromDateTime(now);

        _logger.LogDebug("Running booking reminder check at {Now} for date {Today}", now, today);

        var bookings = await _reminderRepository.GetTodayBookingsNeedingRemindersAsync(today, cancellationToken);
        if (bookings == null || bookings.Count == 0)
        {
            return;
        }

        bool stateChanged = false;

        foreach (var booking in bookings)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            var roomName = booking.Room != null
                ? (!string.IsNullOrWhiteSpace(booking.Room.RoomName) ? booking.Room.RoomName : booking.Room.RoomNumber)
                : "Meeting Room";

            var roomNumber = booking.Room?.RoomNumber ?? string.Empty;

            var meetingTitle = !string.IsNullOrWhiteSpace(booking.MeetingTitle)
                ? booking.MeetingTitle
                : "Room Booking";

            var employeeName = booking.Employee?.Name ?? "Colleague";
            var employeeEmail = booking.Employee?.Email;

            var bookingStartDateTime = booking.BookingDate.ToDateTime(booking.StartTime);
            var bookingEndDateTime = booking.BookingDate.ToDateTime(booking.EndTime);

            // =========================================================
            // 1. START REMINDER (15 minutes before StartTime)
            // =========================================================
            if (!booking.StartReminderSent)
            {
                var timeUntilStart = bookingStartDateTime - now;

                // Send reminder if starting within 15 minutes (with 5-minute grace window)
                if (timeUntilStart <= TimeSpan.FromMinutes(15) && timeUntilStart >= TimeSpan.FromMinutes(-5))
                {
                    _logger.LogInformation(
                        "Triggering 15-min start reminder for Booking ID {BookingId} ('{Title}') to Employee {EmployeeId}",
                        booking.BookingId, meetingTitle, booking.EmployeeId);

                    // A. Create In-App Notification
                    var startNotification = new Notification
                    {
                        EmployeeId = booking.EmployeeId,
                        BookingId = booking.BookingId,
                        Message = $"Reminder: Your booking '{meetingTitle}' in {roomName} starts in 15 minutes at {booking.StartTime:hh\\:mm tt}.",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _notificationRepository.AddAsync(startNotification);

                    // B. Send Email Reminder (if email exists)
                    if (!string.IsNullOrWhiteSpace(employeeEmail))
                    {
                        var emailSubject = $"Reminder: Your meeting '{meetingTitle}' starts in 15 minutes";
                        var emailBody = BuildStartReminderEmailHtml(
                            employeeName,
                            meetingTitle,
                            roomName,
                            roomNumber,
                            booking.BookingDate,
                            booking.StartTime,
                            booking.EndTime,
                            booking.ParticipantCount);

                        try
                        {
                            await _emailService.SendEmailAsync(
                                employeeEmail,
                                emailSubject,
                                emailBody,
                                isHtml: true);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to send start reminder email for booking ID {BookingId} to {Email}", booking.BookingId, employeeEmail);
                        }
                    }

                    // C. Mark Start Reminder as Sent
                    booking.StartReminderSent = true;
                    stateChanged = true;
                }
            }

            // =========================================================
            // 2. END REMINDER (15 minutes before EndTime)
            // =========================================================
            if (!booking.EndReminderSent)
            {
                var timeUntilEnd = bookingEndDateTime - now;

                // Send reminder if ending within 15 minutes (with 5-minute grace window)
                if (timeUntilEnd <= TimeSpan.FromMinutes(15) && timeUntilEnd >= TimeSpan.FromMinutes(-5))
                {
                    _logger.LogInformation(
                        "Triggering 15-min end reminder for Booking ID {BookingId} ('{Title}') to Employee {EmployeeId}",
                        booking.BookingId, meetingTitle, booking.EmployeeId);

                    // A. Create In-App Notification
                    var endNotification = new Notification
                    {
                        EmployeeId = booking.EmployeeId,
                        BookingId = booking.BookingId,
                        Message = $"Reminder: Your booking '{meetingTitle}' in {roomName} will end in 15 minutes at {booking.EndTime:hh\\:mm tt}. Please prepare to conclude.",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _notificationRepository.AddAsync(endNotification);

                    // B. Send Email Reminder (if email exists)
                    if (!string.IsNullOrWhiteSpace(employeeEmail))
                    {
                        var emailSubject = $"Reminder: Your meeting '{meetingTitle}' ends in 15 minutes";
                        var emailBody = BuildEndReminderEmailHtml(
                            employeeName,
                            meetingTitle,
                            roomName,
                            roomNumber,
                            booking.BookingDate,
                            booking.EndTime);

                        try
                        {
                            await _emailService.SendEmailAsync(
                                employeeEmail,
                                emailSubject,
                                emailBody,
                                isHtml: true);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to send end reminder email for booking ID {BookingId} to {Email}", booking.BookingId, employeeEmail);
                        }
                    }

                    // C. Mark End Reminder as Sent
                    booking.EndReminderSent = true;
                    stateChanged = true;
                }
            }
        }

        if (stateChanged)
        {
            await _reminderRepository.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Saved updated reminder statuses to database.");
        }
    }

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
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <title>Meeting Starting Soon</title>
</head>
<body style=""font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background-color: #f4f6f9; margin: 0; padding: 24px; color: #1e293b;"">
    <table align=""center"" border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""max-width: 580px; background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 12px rgba(0, 0, 0, 0.06);"">
        <!-- Header -->
        <tr>
            <td style=""background: linear-gradient(135deg, #3b82f6 0%, #1d4ed8 100%); padding: 32px 28px; text-align: center;"">
                <h1 style=""color: #ffffff; margin: 0; font-size: 22px; font-weight: 700; letter-spacing: -0.5px;"">SpaceBook</h1>
                <p style=""color: #dbeafe; margin: 6px 0 0; font-size: 14px;"">Room Booking Reminder</p>
            </td>
        </tr>
        <!-- Content -->
        <tr>
            <td style=""padding: 28px;"">
                <h2 style=""color: #0f172a; margin: 0 0 12px; font-size: 18px;"">Hello {employeeName},</h2>
                <p style=""color: #475569; font-size: 15px; line-height: 1.5; margin: 0 0 20px;"">
                    Your scheduled room booking will start in <strong>15 minutes</strong>. Here are your booking details:
                </p>

                <!-- Details Card -->
                <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; margin-bottom: 24px;"">
                    <tr>
                        <td style=""padding: 16px 20px;"">
                            <table width=""100%"" cellpadding=""4"" cellspacing=""0"">
                                <tr>
                                    <td width=""35%"" style=""color: #64748b; font-size: 13px; font-weight: 600;"">Meeting Title</td>
                                    <td style=""color: #0f172a; font-size: 14px; font-weight: 600;"">{meetingTitle}</td>
                                </tr>
                                <tr>
                                    <td style=""color: #64748b; font-size: 13px; font-weight: 600;"">Room</td>
                                    <td style=""color: #0f172a; font-size: 14px;"">{roomName} {(string.IsNullOrWhiteSpace(roomNumber) ? "" : $"({roomNumber})")}</td>
                                </tr>
                                <tr>
                                    <td style=""color: #64748b; font-size: 13px; font-weight: 600;"">Date</td>
                                    <td style=""color: #0f172a; font-size: 14px;"">{bookingDate:MMMM dd, yyyy}</td>
                                </tr>
                                <tr>
                                    <td style=""color: #64748b; font-size: 13px; font-weight: 600;"">Time Window</td>
                                    <td style=""color: #2563eb; font-size: 14px; font-weight: 600;"">{startTime:hh\\:mm tt} - {endTime:hh\\:mm tt}</td>
                                </tr>
                                {(participantCount > 0 ? $@"
                                <tr>
                                    <td style=""color: #64748b; font-size: 13px; font-weight: 600;"">Attendees</td>
                                    <td style=""color: #0f172a; font-size: 14px;"">{participantCount} people</td>
                                </tr>" : "")}
                            </table>
                        </td>
                    </tr>
                </table>

                <p style=""color: #64748b; font-size: 13px; margin: 0; line-height: 1.5;"">
                    Please ensure you check in when entering the room to confirm your attendance.
                </p>
            </td>
        </tr>
        <!-- Footer -->
        <tr>
            <td style=""background-color: #f1f5f9; padding: 18px 28px; text-align: center; border-top: 1px solid #e2e8f0;"">
                <p style=""color: #94a3b8; font-size: 12px; margin: 0;"">
                    This is an automated notification from SpaceBook.
                </p>
            </td>
        </tr>
    </table>
</body>
</html>";
    }

    private static string BuildEndReminderEmailHtml(
        string employeeName,
        string meetingTitle,
        string roomName,
        string roomNumber,
        DateOnly bookingDate,
        TimeOnly endTime)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <title>Meeting Ending Soon</title>
</head>
<body style=""font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background-color: #f4f6f9; margin: 0; padding: 24px; color: #1e293b;"">
    <table align=""center"" border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""max-width: 580px; background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 12px rgba(0, 0, 0, 0.06);"">
        <!-- Header -->
        <tr>
            <td style=""background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%); padding: 32px 28px; text-align: center;"">
                <h1 style=""color: #ffffff; margin: 0; font-size: 22px; font-weight: 700; letter-spacing: -0.5px;"">SpaceBook</h1>
                <p style=""color: #fef3c7; margin: 6px 0 0; font-size: 14px;"">Meeting Wrap-up Reminder</p>
            </td>
        </tr>
        <!-- Content -->
        <tr>
            <td style=""padding: 28px;"">
                <h2 style=""color: #0f172a; margin: 0 0 12px; font-size: 18px;"">Hello {employeeName},</h2>
                <p style=""color: #475569; font-size: 15px; line-height: 1.5; margin: 0 0 20px;"">
                    Your meeting <strong>'{meetingTitle}'</strong> in <strong>{roomName}</strong> will conclude in <strong>15 minutes</strong> at <strong>{endTime:hh\\:mm tt}</strong>.
                </p>

                <!-- Info Box -->
                <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color: #fffbeb; border: 1px solid #fef3c7; border-radius: 8px; margin-bottom: 24px;"">
                    <tr>
                        <td style=""padding: 16px 20px; color: #92400e; font-size: 14px; line-height: 1.5;"">
                            Please prepare to wrap up your discussion and leave the room ready for the next scheduled booking.
                        </td>
                    </tr>
                </table>

                <p style=""color: #64748b; font-size: 13px; margin: 0; line-height: 1.5;"">
                    Thank you for using SpaceBook. Have a great day!
                </p>
            </td>
        </tr>
        <!-- Footer -->
        <tr>
            <td style=""background-color: #f1f5f9; padding: 18px 28px; text-align: center; border-top: 1px solid #e2e8f0;"">
                <p style=""color: #94a3b8; font-size: 12px; margin: 0;"">
                    This is an automated notification from SpaceBook.
                </p>
            </td>
        </tr>
    </table>
</body>
</html>";
    }
}
