using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using SpaceBook.Application.Interfaces;
using SpaceBook.Domain.Entities;

namespace SpaceBook.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IConfiguration configuration,
        ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    // =========================================================
    // GENERIC EMAIL SENDER - GMAIL API
    // =========================================================

    public async Task SendEmailAsync(
        string toEmail,
        string subject,
        string body,
        bool isHtml = true)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
        {
            throw new InvalidOperationException(
                "Email recipient is empty.");
        }

        // =====================================================
        // GMAIL API CONFIGURATION
        // =====================================================

        var clientId =
            _configuration["Gmail:ClientId"]
            ?? Environment.GetEnvironmentVariable(
                "Gmail__ClientId");

        var clientSecret =
            _configuration["Gmail:ClientSecret"]
            ?? Environment.GetEnvironmentVariable(
                "Gmail__ClientSecret");

        var refreshToken =
            _configuration["Gmail:RefreshToken"]
            ?? Environment.GetEnvironmentVariable(
                "Gmail__RefreshToken");

        var senderEmail =
            _configuration["Gmail:SenderEmail"]
            ?? Environment.GetEnvironmentVariable(
                "Gmail__SenderEmail");

        // =====================================================
        // SAFE CONFIGURATION LOG
        // =====================================================

        _logger.LogInformation(
            "Gmail API configuration loaded: " +
            "ClientId={ClientIdConfigured}, " +
            "ClientSecret={ClientSecretConfigured}, " +
            "RefreshToken={RefreshTokenConfigured}, " +
            "SenderEmail={SenderConfigured}",
            !string.IsNullOrWhiteSpace(clientId),
            !string.IsNullOrWhiteSpace(clientSecret),
            !string.IsNullOrWhiteSpace(refreshToken),
            !string.IsNullOrWhiteSpace(senderEmail));

        // =====================================================
        // VALIDATE CONFIGURATION
        // =====================================================

        if (string.IsNullOrWhiteSpace(clientId))
        {
            throw new InvalidOperationException(
                "Gmail ClientId is not configured.");
        }

        if (string.IsNullOrWhiteSpace(clientSecret))
        {
            throw new InvalidOperationException(
                "Gmail ClientSecret is not configured.");
        }

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new InvalidOperationException(
                "Gmail RefreshToken is not configured.");
        }

        if (string.IsNullOrWhiteSpace(senderEmail))
        {
            throw new InvalidOperationException(
                "Gmail SenderEmail is not configured.");
        }

        try
        {
            _logger.LogInformation(
                "Attempting Gmail API email. " +
                "From={From}, To={To}, Subject={Subject}",
                senderEmail,
                toEmail,
                subject);

            // =================================================
            // CREATE GOOGLE OAUTH FLOW
            // =================================================

            var flow =
                new GoogleAuthorizationCodeFlow(
                    new GoogleAuthorizationCodeFlow.Initializer
                    {
                        ClientSecrets =
                            new ClientSecrets
                            {
                                ClientId =
                                    clientId.Trim(),

                                ClientSecret =
                                    clientSecret.Trim()
                            }
                    });

            // =================================================
            // LOAD REFRESH TOKEN
            // =================================================

            var tokenResponse =
                new TokenResponse
                {
                    RefreshToken =
                        refreshToken.Trim()
                };

            var credential =
                new UserCredential(
                    flow,
                    senderEmail.Trim(),
                    tokenResponse);

            // =================================================
            // CREATE GMAIL SERVICE
            // =================================================

            using var gmailService =
                new GmailService(
                    new BaseClientService.Initializer
                    {
                        HttpClientInitializer =
                            credential,

                        ApplicationName =
                            "SpaceBook"
                    });

            // =================================================
            // BUILD EMAIL
            // =================================================

            var email =
                new MimeMessage();

            email.From.Add(
                new MailboxAddress(
                    "SpaceBook",
                    senderEmail.Trim()));

            email.To.Add(
                MailboxAddress.Parse(
                    toEmail.Trim()));

            email.Subject =
                subject;

            var bodyBuilder =
                new BodyBuilder();

            if (isHtml)
            {
                bodyBuilder.HtmlBody =
                    body;
            }
            else
            {
                bodyBuilder.TextBody =
                    body;
            }

            email.Body =
                bodyBuilder.ToMessageBody();

            // =================================================
            // CONVERT MIME MESSAGE TO BASE64URL
            // =================================================

            using var stream =
                new MemoryStream();

            await email.WriteToAsync(
                stream);

            var rawMessage =
                Convert
                    .ToBase64String(
                        stream.ToArray())
                    .Replace('+', '-')
                    .Replace('/', '_')
                    .TrimEnd('=');

            var gmailMessage =
                new Message
                {
                    Raw =
                        rawMessage
                };

            // =================================================
            // SEND THROUGH GMAIL API
            // =================================================

            var request =
                gmailService
                    .Users
                    .Messages
                    .Send(
                        gmailMessage,
                        "me");

            var result =
                await request.ExecuteAsync();

            _logger.LogInformation(
                "Gmail API email sent successfully. " +
                "To={To}, MessageId={MessageId}",
                toEmail,
                result.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Gmail API email failed. " +
                "To={To}, Subject={Subject}",
                toEmail,
                subject);

            throw;
        }
    }

    // =========================================================
    // SEND EMAIL TO MULTIPLE RECIPIENTS
    // =========================================================

    public async Task SendEmailsAsync(
        IEnumerable<string> toEmails,
        string subject,
        string body,
        bool isHtml = true)
    {
        var validEmails =
            toEmails?
                .Where(e =>
                    !string.IsNullOrWhiteSpace(e))
                .Select(e =>
                    e.Trim())
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .ToList();

        if (validEmails == null ||
            validEmails.Count == 0)
        {
            _logger.LogWarning(
                "Email recipient list is empty.");

            return;
        }

        foreach (var email in validEmails)
        {
            await SendEmailAsync(
                email,
                subject,
                body,
                isHtml);
        }
    }

    // =========================================================
    // NOTIFICATION 1
    // BOOKING CONFIRMATION
    // =========================================================

    public async Task SendBookingConfirmationAsync(
        Booking booking,
        Employee employee,
        Room room,
        IEnumerable<string> adminEmails)
    {
        var employeeName =
            !string.IsNullOrWhiteSpace(employee?.Name)
                ? employee.Name
                : "Colleague";

        var employeeEmail =
            employee?.Email;

        var roomName =
            GetRoomName(room);

        var meetingTitle =
            !string.IsNullOrWhiteSpace(
                booking.MeetingTitle)
                ? booking.MeetingTitle
                : "Room Booking";

        if (string.IsNullOrWhiteSpace(employeeEmail))
        {
            throw new InvalidOperationException(
                $"Employee email is missing for BookingId={booking.BookingId}.");
        }

        // =====================================================
        // EMPLOYEE CONFIRMATION
        // =====================================================

        var employeeSubject =
            $"SpaceBook Booking Confirmed - {meetingTitle}";

        var employeeBody =
            BuildConfirmationEmailHtml(
                employeeName,
                meetingTitle,
                roomName,
                booking.BookingDate,
                booking.StartTime,
                booking.EndTime,
                booking.ParticipantCount);

        await SendEmailAsync(
            employeeEmail,
            employeeSubject,
            employeeBody,
            true);

        // =====================================================
        // ADMIN CONFIRMATION
        // =====================================================

        var adminList =
            ResolveAdminEmails(
                adminEmails);

        if (adminList.Count > 0)
        {
            var adminSubject =
                $"[Admin Alert] SpaceBook Booking Confirmed - {meetingTitle}";

            var adminBody =
                BuildAdminConfirmationEmailHtml(
                    employeeName,
                    employeeEmail,
                    employee?.Department
                        ?? string.Empty,
                    meetingTitle,
                    roomName,
                    booking.BookingDate,
                    booking.StartTime,
                    booking.EndTime,
                    booking.ParticipantCount);

            await SendEmailsAsync(
                adminList,
                adminSubject,
                adminBody,
                true);
        }
        else
        {
            _logger.LogWarning(
                "Booking confirmation sent to employee but no Admin email recipients were configured. BookingId={BookingId}",
                booking.BookingId);
        }
    }

    // =========================================================
    // NOTIFICATION 2
    // 15-MINUTE START REMINDER (EMPLOYEE ONLY)
    // =========================================================

    public async Task SendBookingStartReminderAsync(
        Booking booking,
        Employee employee,
        Room room)
    {
        var employeeName =
            !string.IsNullOrWhiteSpace(employee?.Name)
                ? employee.Name
                : "Colleague";

        var employeeEmail =
            employee?.Email;

        var roomName =
            GetRoomName(room);

        var meetingTitle =
            !string.IsNullOrWhiteSpace(
                booking.MeetingTitle)
                ? booking.MeetingTitle
                : "Room Booking";

        if (string.IsNullOrWhiteSpace(employeeEmail))
        {
            throw new InvalidOperationException(
                $"Employee email is missing for BookingId={booking.BookingId}.");
        }

        // =====================================================
        // EMPLOYEE START REMINDER
        // =====================================================

        const string employeeSubject =
            "SpaceBook Reminder - Booking Starts in 15 Minutes";

        var employeeBody =
            BuildStartReminderEmailHtml(
                employeeName,
                meetingTitle,
                roomName,
                booking.BookingDate,
                booking.StartTime,
                booking.EndTime,
                booking.ParticipantCount);

        await SendEmailAsync(
            employeeEmail,
            employeeSubject,
            employeeBody,
            true);
    }

    // =========================================================
    // NOTIFICATION 3
    // 15-MINUTE END REMINDER (EMPLOYEE ONLY)
    // =========================================================

    public async Task SendBookingEndReminderAsync(
        Booking booking,
        Employee employee,
        Room room)
    {
        var employeeName =
            !string.IsNullOrWhiteSpace(employee?.Name)
                ? employee.Name
                : "Colleague";

        var employeeEmail =
            employee?.Email;

        var roomName =
            GetRoomName(room);

        var meetingTitle =
            !string.IsNullOrWhiteSpace(
                booking.MeetingTitle)
                ? booking.MeetingTitle
                : "Room Booking";

        if (string.IsNullOrWhiteSpace(employeeEmail))
        {
            throw new InvalidOperationException(
                $"Employee email is missing for BookingId={booking.BookingId}.");
        }

        // =====================================================
        // EMPLOYEE END REMINDER
        // =====================================================

        const string employeeSubject =
            "SpaceBook Reminder - Booking Ends in 15 Minutes";

        var employeeBody =
            BuildEndReminderEmailHtml(
                employeeName,
                meetingTitle,
                roomName,
                booking.BookingDate,
                booking.StartTime,
                booking.EndTime);

        await SendEmailAsync(
            employeeEmail,
            employeeSubject,
            employeeBody,
            true);
    }

    // =========================================================
    // ADMIN EMAIL RESOLUTION
    // =========================================================

    private List<string> ResolveAdminEmails(
        IEnumerable<string>? passedAdminEmails)
    {
        var list =
            new List<string>();

        if (passedAdminEmails != null)
        {
            list.AddRange(
                passedAdminEmails
                    .Where(e =>
                        !string.IsNullOrWhiteSpace(e))
                    .Select(e =>
                        e.Trim()));
        }

        var configAdminEmail =
            _configuration["Gmail:AdminEmail"]
            ?? Environment.GetEnvironmentVariable(
                "Gmail__AdminEmail");

        if (!string.IsNullOrWhiteSpace(
                configAdminEmail))
        {
            var configuredAdmins =
                configAdminEmail
                    .Split(
                        new[]
                        {
                            ';',
                            ','
                        },
                        StringSplitOptions.RemoveEmptyEntries)
                    .Select(x =>
                        x.Trim())
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x));

            list.AddRange(
                configuredAdmins);
        }

        return list
            .Distinct(
                StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    // =========================================================
    // ROOM NAME
    // =========================================================

    private static string GetRoomName(
        Room? room)
    {
        if (room == null)
        {
            return "Meeting Room";
        }

        if (!string.IsNullOrWhiteSpace(
                room.RoomName))
        {
            if (!string.IsNullOrWhiteSpace(
                    room.RoomNumber))
            {
                return
                    $"{room.RoomName} ({room.RoomNumber})";
            }

            return room.RoomName;
        }

        if (!string.IsNullOrWhiteSpace(
                room.RoomNumber))
        {
            return room.RoomNumber;
        }

        return "Meeting Room";
    }

    // =========================================================
    // TIME FORMAT
    // =========================================================

    private static string FormatTime(
        TimeOnly time)
    {
        return DateTime.Today
            .Add(
                time.ToTimeSpan())
            .ToString(
                "hh:mm tt");
    }

    // =========================================================
    // EMPLOYEE CONFIRMATION EMAIL
    // =========================================================

    private static string BuildConfirmationEmailHtml(
        string employeeName,
        string meetingTitle,
        string roomName,
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
            <title>SpaceBook Booking Confirmed</title>
        </head>

        <body style="
            font-family: Arial, sans-serif;
            background-color: #f4f6f9;
            padding: 24px;
            color: #1e293b;
            margin: 0;">

            <table
                align="center"
                width="100%"
                cellpadding="0"
                cellspacing="0"
                style="
                    max-width:580px;
                    background:#ffffff;
                    border-radius:12px;
                    overflow:hidden;
                    box-shadow:0 4px 12px rgba(0,0,0,0.06);">

                <tr>
                    <td style="
                        background:#059669;
                        padding:28px;
                        text-align:center;
                        color:#ffffff;">

                        <h1 style="margin:0;font-size:24px;">
                            SpaceBook
                        </h1>

                        <p style="margin:6px 0 0;">
                            Room Booking Confirmed
                        </p>
                    </td>
                </tr>

                <tr>
                    <td style="padding:28px;">

                        <h2 style="margin-top:0;">
                            Hello {employeeName},
                        </h2>

                        <p>
                            Your SpaceBook room booking has
                            been confirmed successfully.
                        </p>

                        <table
                            width="100%"
                            cellpadding="10"
                            cellspacing="0"
                            style="
                                background:#f8fafc;
                                border:1px solid #e2e8f0;
                                border-radius:8px;
                                margin:20px 0;">

                            <tr>
                                <td><strong>Meeting:</strong></td>
                                <td>{meetingTitle}</td>
                            </tr>

                            <tr>
                                <td><strong>Room:</strong></td>
                                <td>{roomName}</td>
                            </tr>

                            <tr>
                                <td><strong>Date:</strong></td>
                                <td>{bookingDate:MMMM dd, yyyy}</td>
                            </tr>

                            <tr>
                                <td><strong>Start Time:</strong></td>
                                <td>{FormatTime(startTime)}</td>
                            </tr>

                            <tr>
                                <td><strong>End Time:</strong></td>
                                <td>{FormatTime(endTime)}</td>
                            </tr>

                            <tr>
                                <td><strong>Participants:</strong></td>
                                <td>{participantCount}</td>
                            </tr>

                            <tr>
                                <td><strong>Status:</strong></td>
                                <td style="
                                    color:#059669;
                                    font-weight:bold;">
                                    Approved
                                </td>
                            </tr>

                        </table>

                        <p>
                            Your room has been successfully
                            reserved.
                        </p>

                        <p>
                            Regards,<br>
                            <strong>SpaceBook</strong>
                        </p>

                    </td>
                </tr>

                <tr>
                    <td style="
                        text-align:center;
                        padding:16px;
                        background:#f1f5f9;
                        color:#64748b;
                        font-size:12px;">

                        This is an automated notification from SpaceBook.

                    </td>
                </tr>

            </table>
        </body>
        </html>
        """;
    }

    // =========================================================
    // ADMIN CONFIRMATION EMAIL
    // =========================================================

    private static string BuildAdminConfirmationEmailHtml(
        string employeeName,
        string employeeEmail,
        string department,
        string meetingTitle,
        string roomName,
        DateOnly bookingDate,
        TimeOnly startTime,
        TimeOnly endTime,
        int participantCount)
    {
        var departmentRow =
            !string.IsNullOrWhiteSpace(department)
                ? $"""
                   <tr>
                       <td><strong>Department:</strong></td>
                       <td>{department}</td>
                   </tr>
                   """
                : string.Empty;

        return $"""
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <title>SpaceBook Booking Confirmed</title>
        </head>

        <body style="
            font-family: Arial, sans-serif;
            background-color: #f4f6f9;
            padding: 24px;
            color: #1e293b;
            margin: 0;">

            <table
                align="center"
                width="100%"
                cellpadding="0"
                cellspacing="0"
                style="
                    max-width:580px;
                    background:#ffffff;
                    border-radius:12px;
                    overflow:hidden;
                    box-shadow:0 4px 12px rgba(0,0,0,0.06);">

                <tr>
                    <td style="
                        background:#059669;
                        padding:28px;
                        text-align:center;
                        color:#ffffff;">

                        <h1 style="margin:0;font-size:24px;">
                            SpaceBook
                        </h1>

                        <p style="margin:6px 0 0;">
                            Room Booking Confirmed
                        </p>
                    </td>
                </tr>

                <tr>
                    <td style="padding:28px;">

                        <h2 style="margin-top:0;">
                            Hello Admin,
                        </h2>

                        <p>
                            A room booking has been automatically approved.
                        </p>

                        <table
                            width="100%"
                            cellpadding="10"
                            cellspacing="0"
                            style="
                                background:#f8fafc;
                                border:1px solid #e2e8f0;
                                border-radius:8px;
                                margin:20px 0;">

                            <tr>
                                <td><strong>Employee:</strong></td>
                                <td>{employeeName} ({employeeEmail})</td>
                            </tr>

                            {departmentRow}

                            <tr>
                                <td><strong>Meeting:</strong></td>
                                <td>{meetingTitle}</td>
                            </tr>

                            <tr>
                                <td><strong>Room:</strong></td>
                                <td>{roomName}</td>
                            </tr>

                            <tr>
                                <td><strong>Date:</strong></td>
                                <td>{bookingDate:MMMM dd, yyyy}</td>
                            </tr>

                            <tr>
                                <td><strong>Start Time:</strong></td>
                                <td>{FormatTime(startTime)}</td>
                            </tr>

                            <tr>
                                <td><strong>End Time:</strong></td>
                                <td>{FormatTime(endTime)}</td>
                            </tr>

                            <tr>
                                <td><strong>Participants:</strong></td>
                                <td>{participantCount}</td>
                            </tr>

                            <tr>
                                <td><strong>Status:</strong></td>
                                <td style="
                                    color:#059669;
                                    font-weight:bold;">
                                    Approved
                                </td>
                            </tr>

                        </table>

                        <p>
                            The room has been successfully reserved.
                        </p>

                        <p>
                            Regards,<br>
                            <strong>SpaceBook</strong>
                        </p>

                    </td>
                </tr>

                <tr>
                    <td style="
                        text-align:center;
                        padding:16px;
                        background:#f1f5f9;
                        color:#64748b;
                        font-size:12px;">

                        This is an automated notification from SpaceBook.

                    </td>
                </tr>

            </table>
        </body>
        </html>
        """;
    }

    // =========================================================
    // EMPLOYEE START REMINDER EMAIL
    // =========================================================

    private static string BuildStartReminderEmailHtml(
        string employeeName,
        string meetingTitle,
        string roomName,
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
            <title>SpaceBook Reminder - Booking Starts in 15 Minutes</title>
        </head>

        <body style="
            font-family: Arial, sans-serif;
            background-color: #f4f6f9;
            padding: 24px;
            color: #1e293b;
            margin: 0;">

            <table
                align="center"
                width="100%"
                cellpadding="0"
                cellspacing="0"
                style="
                    max-width:580px;
                    background:#ffffff;
                    border-radius:12px;
                    overflow:hidden;
                    box-shadow:0 4px 12px rgba(0,0,0,0.06);">

                <tr>
                    <td style="
                        background:#059669;
                        padding:28px;
                        text-align:center;
                        color:#ffffff;">

                        <h1 style="margin:0;font-size:24px;">
                            SpaceBook
                        </h1>

                        <p style="margin:6px 0 0;">
                            Booking Starts in 15 Minutes
                        </p>
                    </td>
                </tr>

                <tr>
                    <td style="padding:28px;">

                        <h2 style="margin-top:0;">
                            Hello {employeeName},
                        </h2>

                        <p>
                            This is a reminder that your SpaceBook room booking will start in <strong>15 minutes</strong>.
                        </p>

                        <table
                            width="100%"
                            cellpadding="10"
                            cellspacing="0"
                            style="
                                background:#f8fafc;
                                border:1px solid #e2e8f0;
                                border-radius:8px;
                                margin:20px 0;">

                            <tr>
                                <td><strong>Meeting:</strong></td>
                                <td>{meetingTitle}</td>
                            </tr>

                            <tr>
                                <td><strong>Room:</strong></td>
                                <td>{roomName}</td>
                            </tr>

                            <tr>
                                <td><strong>Date:</strong></td>
                                <td>{bookingDate:MMMM dd, yyyy}</td>
                            </tr>

                            <tr>
                                <td><strong>Start Time:</strong></td>
                                <td>{FormatTime(startTime)}</td>
                            </tr>

                            <tr>
                                <td><strong>End Time:</strong></td>
                                <td>{FormatTime(endTime)}</td>
                            </tr>

                            <tr>
                                <td><strong>Participants:</strong></td>
                                <td>{participantCount}</td>
                            </tr>

                        </table>

                        <p>
                            Please be ready for your booking.
                        </p>

                        <p>
                            Regards,<br>
                            <strong>SpaceBook</strong>
                        </p>

                    </td>
                </tr>

                <tr>
                    <td style="
                        text-align:center;
                        padding:16px;
                        background:#f1f5f9;
                        color:#64748b;
                        font-size:12px;">

                        This is an automated notification from SpaceBook.

                    </td>
                </tr>

            </table>
        </body>
        </html>
        """;
    }

    // =========================================================
    // EMPLOYEE END REMINDER EMAIL
    // =========================================================

    private static string BuildEndReminderEmailHtml(
        string employeeName,
        string meetingTitle,
        string roomName,
        DateOnly bookingDate,
        TimeOnly startTime,
        TimeOnly endTime)
    {
        return $"""
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <title>SpaceBook Reminder - Booking Ends in 15 Minutes</title>
        </head>

        <body style="
            font-family: Arial, sans-serif;
            background-color: #f4f6f9;
            padding: 24px;
            color: #1e293b;
            margin: 0;">

            <table
                align="center"
                width="100%"
                cellpadding="0"
                cellspacing="0"
                style="
                    max-width:580px;
                    background:#ffffff;
                    border-radius:12px;
                    overflow:hidden;
                    box-shadow:0 4px 12px rgba(0,0,0,0.06);">

                <tr>
                    <td style="
                        background:#059669;
                        padding:28px;
                        text-align:center;
                        color:#ffffff;">

                        <h1 style="margin:0;font-size:24px;">
                            SpaceBook
                        </h1>

                        <p style="margin:6px 0 0;">
                            Booking Ends in 15 Minutes
                        </p>
                    </td>
                </tr>

                <tr>
                    <td style="padding:28px;">

                        <h2 style="margin-top:0;">
                            Hello {employeeName},
                        </h2>

                        <p>
                            Your SpaceBook room booking will end in <strong>15 minutes</strong>.
                        </p>

                        <table
                            width="100%"
                            cellpadding="10"
                            cellspacing="0"
                            style="
                                background:#f8fafc;
                                border:1px solid #e2e8f0;
                                border-radius:8px;
                                margin:20px 0;">

                            <tr>
                                <td><strong>Meeting:</strong></td>
                                <td>{meetingTitle}</td>
                            </tr>

                            <tr>
                                <td><strong>Room:</strong></td>
                                <td>{roomName}</td>
                            </tr>

                            <tr>
                                <td><strong>Date:</strong></td>
                                <td>{bookingDate:MMMM dd, yyyy}</td>
                            </tr>

                            <tr>
                                <td><strong>Start Time:</strong></td>
                                <td>{FormatTime(startTime)}</td>
                            </tr>

                            <tr>
                                <td><strong>End Time:</strong></td>
                                <td>{FormatTime(endTime)}</td>
                            </tr>

                        </table>

                        <p>
                            Please complete your meeting and vacate the room on time.
                        </p>

                        <p>
                            Regards,<br>
                            <strong>SpaceBook</strong>
                        </p>

                    </td>
                </tr>

                <tr>
                    <td style="
                        text-align:center;
                        padding:16px;
                        background:#f1f5f9;
                        color:#64748b;
                        font-size:12px;">

                        This is an automated notification from SpaceBook.

                    </td>
                </tr>

            </table>
        </body>
        </html>
        """;
    }
}