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
            padding: 12px;
            color: #1e293b;
            margin: 0;">

            <table
                align="center"
                width="100%"
                cellpadding="0"
                cellspacing="0"
                style="
                    max-width: 520px;
                    background: #ffffff;
                    border-radius: 8px;
                    overflow: hidden;
                    box-shadow: 0 2px 6px rgba(0,0,0,0.05);">

                <tr>
                    <td style="
                        background: #059669;
                        padding: 14px 18px;
                        text-align: center;
                        color: #ffffff;">

                        <h1 style="margin:0;font-size:19px;font-weight:700;">
                            SpaceBook
                        </h1>

                        <p style="margin:3px 0 0;font-size:13px;opacity:0.95;">
                            Room Booking Confirmed
                        </p>
                    </td>
                </tr>

                <tr>
                    <td style="padding: 16px 20px;">

                        <h2 style="margin: 0 0 6px 0; font-size: 16px; color: #0f172a;">
                            Hello {employeeName},
                        </h2>

                        <p style="margin: 0 0 10px 0; font-size: 13px; line-height: 1.4; color: #334155;">
                            Your SpaceBook room booking has been confirmed successfully.
                        </p>

                        <table
                            width="100%"
                            cellpadding="0"
                            cellspacing="0"
                            style="
                                background: #f8fafc;
                                border: 1px solid #e2e8f0;
                                border-radius: 6px;
                                margin: 10px 0;
                                font-size: 13px;">

                            <tr>
                                <td style="padding: 6px 10px; color: #64748b; width: 35%;"><strong>Meeting:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a;">{meetingTitle}</td>
                            </tr>

                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>Room:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a;">{roomName}</td>
                            </tr>

                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>Date:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a;">{bookingDate:MMMM dd, yyyy}</td>
                            </tr>

                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>Start Time:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a;">{FormatTime(startTime)}</td>
                            </tr>

                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>End Time:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a;">{FormatTime(endTime)}</td>
                            </tr>

                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>Participants:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a;">{participantCount}</td>
                            </tr>

                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>Status:</strong></td>
                                <td style="padding: 6px 10px; color: #059669; font-weight: bold;">
                                    Approved
                                </td>
                            </tr>

                        </table>

                        <p style="margin: 8px 0 6px 0; font-size: 12px; color: #475569;">
                            Your room has been successfully reserved.
                        </p>

                        <p style="margin: 8px 0 0 0; font-size: 13px; color: #334155;">
                            Regards,<br>
                            <strong>SpaceBook</strong>
                        </p>

                    </td>
                </tr>

                <tr>
                    <td style="
                        text-align: center;
                        padding: 8px 14px;
                        background: #f1f5f9;
                        color: #64748b;
                        font-size: 11px;">

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
                       <td style="padding: 6px 10px; color: #64748b;"><strong>Department:</strong></td>
                       <td style="padding: 6px 10px; color: #0f172a;">{department}</td>
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
            padding: 12px;
            color: #1e293b;
            margin: 0;">

            <table
                align="center"
                width="100%"
                cellpadding="0"
                cellspacing="0"
                style="
                    max-width: 520px;
                    background: #ffffff;
                    border-radius: 8px;
                    overflow: hidden;
                    box-shadow: 0 2px 6px rgba(0,0,0,0.05);">

                <tr>
                    <td style="
                        background: #059669;
                        padding: 14px 18px;
                        text-align: center;
                        color: #ffffff;">

                        <h1 style="margin:0;font-size:19px;font-weight:700;">
                            SpaceBook
                        </h1>

                        <p style="margin:3px 0 0;font-size:13px;opacity:0.95;">
                            Room Booking Confirmed
                        </p>
                    </td>
                </tr>

                <tr>
                    <td style="padding: 16px 20px;">

                        <h2 style="margin: 0 0 6px 0; font-size: 16px; color: #0f172a;">
                            Hello Admin,
                        </h2>

                        <p style="margin: 0 0 10px 0; font-size: 13px; line-height: 1.4; color: #334155;">
                            A room booking has been automatically approved.
                        </p>

                        <table
                            width="100%"
                            cellpadding="0"
                            cellspacing="0"
                            style="
                                background: #f8fafc;
                                border: 1px solid #e2e8f0;
                                border-radius: 6px;
                                margin: 10px 0;
                                font-size: 13px;">

                            <tr>
                                <td style="padding: 6px 10px; color: #64748b; width: 35%;"><strong>Employee:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a;">{employeeName} ({employeeEmail})</td>
                            </tr>

                            {departmentRow}

                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>Meeting:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a;">{meetingTitle}</td>
                            </tr>

                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>Room:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a;">{roomName}</td>
                            </tr>

                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>Date:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a;">{bookingDate:MMMM dd, yyyy}</td>
                            </tr>

                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>Start Time:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a;">{FormatTime(startTime)}</td>
                            </tr>

                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>End Time:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a;">{FormatTime(endTime)}</td>
                            </tr>

                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>Participants:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a;">{participantCount}</td>
                            </tr>

                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>Status:</strong></td>
                                <td style="padding: 6px 10px; color: #059669; font-weight: bold;">
                                    Approved
                                </td>
                            </tr>

                        </table>

                        <p style="margin: 8px 0 6px 0; font-size: 12px; color: #475569;">
                            The room has been successfully reserved.
                        </p>

                        <p style="margin: 8px 0 0 0; font-size: 13px; color: #334155;">
                            Regards,<br>
                            <strong>SpaceBook</strong>
                        </p>

                    </td>
                </tr>

                <tr>
                    <td style="
                        text-align: center;
                        padding: 8px 14px;
                        background: #f1f5f9;
                        color: #64748b;
                        font-size: 11px;">

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
            padding: 12px;
            color: #1e293b;
            margin: 0;">

            <table
                align="center"
                width="100%"
                cellpadding="0"
                cellspacing="0"
                style="
                    max-width: 520px;
                    background: #ffffff;
                    border-radius: 8px;
                    overflow: hidden;
                    box-shadow: 0 2px 6px rgba(0,0,0,0.05);">

                <tr>
                    <td style="
                        background: #059669;
                        padding: 14px 18px;
                        text-align: center;
                        color: #ffffff;">

                        <h1 style="margin:0;font-size:19px;font-weight:700;">
                            SpaceBook
                        </h1>

                        <p style="margin:3px 0 0;font-size:13px;opacity:0.95;">
                            Booking Starts in 15 Minutes
                        </p>
                    </td>
                </tr>

                <tr>
                    <td style="padding: 16px 20px;">

                        <h2 style="margin: 0 0 6px 0; font-size: 16px; color: #0f172a;">
                            Hello {employeeName},
                        </h2>

                        <p style="margin: 0 0 10px 0; font-size: 13px; line-height: 1.4; color: #334155;">
                            This is a reminder that your SpaceBook room booking will start in <strong>15 minutes</strong>.
                        </p>

                        <table
                            width="100%"
                            cellpadding="0"
                            cellspacing="0"
                            style="
                                background: #f8fafc;
                                border: 1px solid #e2e8f0;
                                border-radius: 6px;
                                margin: 10px 0;
                                font-size: 13px;">

                            <tr>
                                <td style="padding: 6px 10px; color: #64748b; width: 35%;"><strong>Meeting:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a;">{meetingTitle}</td>
                            </tr>

                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>Room:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a;">{roomName}</td>
                            </tr>

                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>Date:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a;">{bookingDate:MMMM dd, yyyy}</td>
                            </tr>

                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>Start Time:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a;">{FormatTime(startTime)}</td>
                            </tr>

                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>End Time:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a;">{FormatTime(endTime)}</td>
                            </tr>

                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>Participants:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a;">{participantCount}</td>
                            </tr>

                        </table>

                        <p style="margin: 8px 0 6px 0; font-size: 12px; color: #475569;">
                            Please be ready for your booking.
                        </p>

                        <p style="margin: 8px 0 0 0; font-size: 13px; color: #334155;">
                            Regards,<br>
                            <strong>SpaceBook</strong>
                        </p>

                    </td>
                </tr>

                <tr>
                    <td style="
                        text-align: center;
                        padding: 8px 14px;
                        background: #f1f5f9;
                        color: #64748b;
                        font-size: 11px;">

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
            padding: 12px;
            color: #1e293b;
            margin: 0;">

            <table
                align="center"
                width="100%"
                cellpadding="0"
                cellspacing="0"
                style="
                    max-width: 520px;
                    background: #ffffff;
                    border-radius: 8px;
                    overflow: hidden;
                    box-shadow: 0 2px 6px rgba(0,0,0,0.05);">

                <tr>
                    <td style="
                        background: #059669;
                        padding: 14px 18px;
                        text-align: center;
                        color: #ffffff;">

                        <h1 style="margin:0;font-size:19px;font-weight:700;">
                            SpaceBook
                        </h1>

                        <p style="margin:3px 0 0;font-size:13px;opacity:0.95;">
                            Booking Ends in 15 Minutes
                        </p>
                    </td>
                </tr>

                <tr>
                    <td style="padding: 16px 20px;">

                        <h2 style="margin: 0 0 6px 0; font-size: 16px; color: #0f172a;">
                            Hello {employeeName},
                        </h2>

                        <p style="margin: 0 0 10px 0; font-size: 13px; line-height: 1.4; color: #334155;">
                            Your SpaceBook room booking will end in <strong>15 minutes</strong>.
                        </p>

                        <table
                            width="100%"
                            cellpadding="0"
                            cellspacing="0"
                            style="
                                background: #f8fafc;
                                border: 1px solid #e2e8f0;
                                border-radius: 6px;
                                margin: 10px 0;
                                font-size: 13px;">

                            <tr>
                                <td style="padding: 6px 10px; color: #64748b; width: 35%;"><strong>Meeting:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a;">{meetingTitle}</td>
                            </tr>

                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>Room:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a;">{roomName}</td>
                            </tr>

                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>Date:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a;">{bookingDate:MMMM dd, yyyy}</td>
                            </tr>

                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>Start Time:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a;">{FormatTime(startTime)}</td>
                            </tr>

                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>End Time:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a;">{FormatTime(endTime)}</td>
                            </tr>

                        </table>

                        <p style="margin: 8px 0 6px 0; font-size: 12px; color: #475569;">
                            Please complete your meeting and vacate the room on time.
                        </p>

                        <p style="margin: 8px 0 0 0; font-size: 13px; color: #334155;">
                            Regards,<br>
                            <strong>SpaceBook</strong>
                        </p>

                    </td>
                </tr>

                <tr>
                    <td style="
                        text-align: center;
                        padding: 8px 14px;
                        background: #f1f5f9;
                        color: #64748b;
                        font-size: 11px;">

                        This is an automated notification from SpaceBook.

                    </td>
                </tr>

            </table>
        </body>
        </html>
        """;
    }

    // =========================================================
    // HOTSEAT NOTIFICATION 1: CONFIRMATION
    // =========================================================

    public async Task SendHotseatBookingConfirmationAsync(
        HotseatBooking booking,
        Employee employee,
        Seat seat,
        IEnumerable<string>? adminEmails = null)
    {
        var employeeName = !string.IsNullOrWhiteSpace(employee?.Name) ? employee.Name : "Colleague";
        var employeeEmail = employee?.Email;

        if (string.IsNullOrWhiteSpace(employeeEmail))
        {
            throw new InvalidOperationException($"Employee email is missing for HotseatBookingId={booking.HotseatBookingId}.");
        }

        var seatNumber = seat?.SeatNumber ?? $"Seat {booking.SeatId}";
        var moduleName = seat?.Module?.ModuleName ?? "Module";
        var officeName = seat?.Module?.Office?.OfficeName ?? "Office";
        var cityName = seat?.Module?.Office?.Location?.LocationName ?? "Location";

        DateTime localStartTime = booking.CheckInDeadline.HasValue
            ? TimeZoneInfo.ConvertTimeFromUtc(booking.CheckInDeadline.Value, IndiaTimeZone)
            : booking.BookingDate.ToDateTime(new TimeOnly(9, 0, 0));

        var startTimeFormatted = localStartTime.ToString("hh:mm tt");
        var checkInOpensFormatted = localStartTime.AddHours(-1).ToString("hh:mm tt");

        var employeeSubject = $"SpaceBook Booking Confirmed - Hotseat ({seatNumber})";
        var employeeBody = BuildHotseatConfirmationEmailHtml(
            employeeName,
            booking.HotseatBookingId,
            seatNumber,
            moduleName,
            officeName,
            cityName,
            booking.BookingDate,
            startTimeFormatted,
            checkInOpensFormatted);

        await SendEmailAsync(employeeEmail, employeeSubject, employeeBody, true);

        // Admin Alert
        var adminList = ResolveAdminEmails(adminEmails);
        if (adminList.Count > 0)
        {
            var adminSubject = $"[Admin Alert] SpaceBook Hotseat Booking Confirmed - {employeeName} (Seat {seatNumber})";
            var adminBody = BuildAdminHotseatConfirmationEmailHtml(
                employeeName,
                employeeEmail,
                employee?.Department ?? string.Empty,
                booking.HotseatBookingId,
                seatNumber,
                moduleName,
                officeName,
                cityName,
                booking.BookingDate,
                startTimeFormatted);

            await SendEmailsAsync(adminList, adminSubject, adminBody, true);
        }
    }

    // =========================================================
    // HOTSEAT NOTIFICATION 2: 1-HOUR CHECK-IN REMINDER
    // =========================================================

    public async Task SendHotseatCheckInReminderAsync(
        HotseatBooking booking,
        Employee employee,
        Seat seat)
    {
        var employeeName = !string.IsNullOrWhiteSpace(employee?.Name) ? employee.Name : "Colleague";
        var employeeEmail = employee?.Email;

        if (string.IsNullOrWhiteSpace(employeeEmail))
        {
            throw new InvalidOperationException($"Employee email is missing for HotseatBookingId={booking.HotseatBookingId}.");
        }

        var seatNumber = seat?.SeatNumber ?? $"Seat {booking.SeatId}";
        var moduleName = seat?.Module?.ModuleName ?? "Module";
        var officeName = seat?.Module?.Office?.OfficeName ?? "Office";
        var cityName = seat?.Module?.Office?.Location?.LocationName ?? "Location";

        DateTime localStartTime = booking.CheckInDeadline.HasValue
            ? TimeZoneInfo.ConvertTimeFromUtc(booking.CheckInDeadline.Value, IndiaTimeZone)
            : booking.BookingDate.ToDateTime(new TimeOnly(9, 0, 0));

        var startTimeFormatted = localStartTime.ToString("hh:mm tt");

        var subject = $"SpaceBook Reminder - Hotseat Check-In Window Open (Seat {seatNumber})";
        var body = BuildHotseatReminderEmailHtml(
            employeeName,
            booking.HotseatBookingId,
            seatNumber,
            moduleName,
            officeName,
            cityName,
            booking.BookingDate,
            startTimeFormatted);

        await SendEmailAsync(employeeEmail, subject, body, true);
    }

    // =========================================================
    // HOTSEAT NOTIFICATION 3: EXPIRED & SEAT RELEASED
    // =========================================================

    public async Task SendHotseatBookingExpiredAsync(
        HotseatBooking booking,
        Employee employee,
        Seat seat)
    {
        var employeeName = !string.IsNullOrWhiteSpace(employee?.Name) ? employee.Name : "Colleague";
        var employeeEmail = employee?.Email;

        if (string.IsNullOrWhiteSpace(employeeEmail))
        {
            throw new InvalidOperationException($"Employee email is missing for HotseatBookingId={booking.HotseatBookingId}.");
        }

        var seatNumber = seat?.SeatNumber ?? $"Seat {booking.SeatId}";
        var moduleName = seat?.Module?.ModuleName ?? "Module";
        var officeName = seat?.Module?.Office?.OfficeName ?? "Office";
        var cityName = seat?.Module?.Office?.Location?.LocationName ?? "Location";

        DateTime localStartTime = booking.CheckInDeadline.HasValue
            ? TimeZoneInfo.ConvertTimeFromUtc(booking.CheckInDeadline.Value, IndiaTimeZone)
            : booking.BookingDate.ToDateTime(new TimeOnly(9, 0, 0));

        var startTimeFormatted = localStartTime.ToString("hh:mm tt");

        const string subject = "Hotseat Booking Expired – Seat Released";
        var body = BuildHotseatExpiredEmailHtml(
            employeeName,
            booking.HotseatBookingId,
            seatNumber,
            moduleName,
            officeName,
            cityName,
            booking.BookingDate,
            startTimeFormatted);

        await SendEmailAsync(employeeEmail, subject, body, true);
    }

    // =========================================================
    // HOTSEAT NOTIFICATION 3: RESCHEDULED
    // =========================================================

    public async Task SendHotseatBookingRescheduledAsync(
        HotseatBooking booking,
        Employee employee,
        Seat seat,
        IEnumerable<string>? adminEmails = null)
    {
        var employeeName = !string.IsNullOrWhiteSpace(employee?.Name) ? employee.Name : "Colleague";
        var employeeEmail = employee?.Email;

        if (string.IsNullOrWhiteSpace(employeeEmail))
        {
            throw new InvalidOperationException($"Employee email is missing for HotseatBookingId={booking.HotseatBookingId}.");
        }

        var seatNumber = seat?.SeatNumber ?? $"Seat {booking.SeatId}";
        var moduleName = seat?.Module?.ModuleName ?? "Module";
        var officeName = seat?.Module?.Office?.OfficeName ?? "Office";
        var cityName = seat?.Module?.Office?.Location?.LocationName ?? "Location";

        DateTime localStartTime = booking.CheckInDeadline.HasValue
            ? TimeZoneInfo.ConvertTimeFromUtc(booking.CheckInDeadline.Value, IndiaTimeZone)
            : booking.BookingDate.ToDateTime(new TimeOnly(9, 0, 0));

        var startTimeFormatted = localStartTime.ToString("hh:mm tt");
        var checkInOpensFormatted = localStartTime.AddHours(-1).ToString("hh:mm tt");

        var employeeSubject = $"SpaceBook Hotseat Booking Rescheduled - Seat {seatNumber}";
        var employeeBody = BuildHotseatRescheduledEmailHtml(
            employeeName,
            booking.HotseatBookingId,
            seatNumber,
            moduleName,
            officeName,
            cityName,
            booking.BookingDate,
            startTimeFormatted,
            checkInOpensFormatted);

        await SendEmailAsync(employeeEmail, employeeSubject, employeeBody, true);

        // Admin Alert
        var adminList = ResolveAdminEmails(adminEmails);
        if (adminList.Count > 0)
        {
            var adminSubject = $"[Admin Alert] SpaceBook Hotseat Booking Rescheduled - {employeeName} (Seat {seatNumber})";
            var adminBody = BuildAdminHotseatRescheduledEmailHtml(
                employeeName,
                employeeEmail,
                employee?.Department ?? string.Empty,
                booking.HotseatBookingId,
                seatNumber,
                moduleName,
                officeName,
                cityName,
                booking.BookingDate,
                startTimeFormatted);

            await SendEmailsAsync(adminList, adminSubject, adminBody, true);
        }
    }

    // =========================================================
    // HOTSEAT NOTIFICATION 4: CANCELLED
    // =========================================================

    public async Task SendHotseatBookingCancelledAsync(
        HotseatBooking booking,
        Employee employee,
        Seat seat,
        IEnumerable<string>? adminEmails = null,
        string? cancellationReason = null)
    {
        var employeeName = !string.IsNullOrWhiteSpace(employee?.Name) ? employee.Name : "Colleague";
        var employeeEmail = employee?.Email;

        if (string.IsNullOrWhiteSpace(employeeEmail))
        {
            throw new InvalidOperationException($"Employee email is missing for HotseatBookingId={booking.HotseatBookingId}.");
        }

        var seatNumber = seat?.SeatNumber ?? $"Seat {booking.SeatId}";
        var moduleName = seat?.Module?.ModuleName ?? "Module";
        var officeName = seat?.Module?.Office?.OfficeName ?? "Office";
        var cityName = seat?.Module?.Office?.Location?.LocationName ?? "Location";

        DateTime localStartTime = booking.CheckInDeadline.HasValue
            ? TimeZoneInfo.ConvertTimeFromUtc(booking.CheckInDeadline.Value, IndiaTimeZone)
            : booking.BookingDate.ToDateTime(new TimeOnly(9, 0, 0));

        var startTimeFormatted = localStartTime.ToString("hh:mm tt");
        var reason = !string.IsNullOrWhiteSpace(cancellationReason) ? cancellationReason : "Cancelled by user";

        var subject = $"SpaceBook Hotseat Booking Cancelled - Seat {seatNumber}";
        var body = BuildHotseatCancelledEmailHtml(
            employeeName,
            booking.HotseatBookingId,
            seatNumber,
            moduleName,
            officeName,
            cityName,
            booking.BookingDate,
            startTimeFormatted,
            reason);

        await SendEmailAsync(employeeEmail, subject, body, true);

        // Admin Alert
        var adminList = ResolveAdminEmails(adminEmails);
        if (adminList.Count > 0)
        {
            var adminSubject = $"[Admin Alert] SpaceBook Hotseat Booking Cancelled - {employeeName} (Seat {seatNumber})";
            var adminBody = BuildAdminHotseatCancelledEmailHtml(
                employeeName,
                employeeEmail,
                employee?.Department ?? string.Empty,
                booking.HotseatBookingId,
                seatNumber,
                moduleName,
                officeName,
                cityName,
                booking.BookingDate,
                startTimeFormatted,
                reason);

            await SendEmailsAsync(adminList, adminSubject, adminBody, true);
        }
    }

    // =========================================================
    // HTML BUILDERS FOR HOTSEAT EMAILS
    // =========================================================

    private static string BuildHotseatConfirmationEmailHtml(
        string employeeName,
        int bookingId,
        string seatNumber,
        string moduleName,
        string officeName,
        string cityName,
        DateOnly bookingDate,
        string startTimeFormatted,
        string checkInOpensFormatted)
    {
        return $"""
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <title>SpaceBook Hotseat Booking Confirmed</title>
        </head>

        <body style="
            font-family: Arial, sans-serif;
            background-color: #f4f6f9;
            padding: 12px;
            color: #1e293b;
            margin: 0;">

            <table
                align="center"
                width="100%"
                cellpadding="0"
                cellspacing="0"
                style="
                    max-width: 520px;
                    background: #ffffff;
                    border-radius: 8px;
                    overflow: hidden;
                    box-shadow: 0 2px 6px rgba(0,0,0,0.05);">

                <tr>
                    <td style="
                        background: #059669;
                        padding: 14px 18px;
                        text-align: center;
                        color: #ffffff;">

                        <h1 style="margin:0;font-size:19px;font-weight:700;">
                            SpaceBook
                        </h1>

                        <p style="margin:3px 0 0;font-size:13px;opacity:0.95;">
                            Hotseat Booking Confirmed
                        </p>
                    </td>
                </tr>

                <tr>
                    <td style="padding: 16px 20px;">

                        <h2 style="margin: 0 0 6px 0; font-size: 16px; color: #0f172a;">
                            Hello {employeeName},
                        </h2>

                        <p style="margin: 0 0 10px 0; font-size: 13px; line-height: 1.4; color: #334155;">
                            Your SpaceBook hotseat booking has been confirmed successfully.
                        </p>

                        <table
                            width="100%"
                            cellpadding="0"
                            cellspacing="0"
                            style="
                                background: #f8fafc;
                                border: 1px solid #e2e8f0;
                                border-radius: 6px;
                                margin: 10px 0;
                                font-size: 13px;">

                            <tr>
                                <td style="padding: 6px 10px; color: #64748b; width: 35%;"><strong>Space Type:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a;">Hot Seat</td>
                            </tr>

                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>Seat:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a; font-weight: bold;">{seatNumber}</td>
                            </tr>

                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>Module:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a;">{moduleName}</td>
                            </tr>

                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>Office / Location:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a;">{officeName} ({cityName})</td>
                            </tr>

                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>Date:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a;">{bookingDate:MMMM dd, yyyy}</td>
                            </tr>

                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>Start Time:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a;">{startTimeFormatted}</td>
                            </tr>

                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>Check-In Window:</strong></td>
                                <td style="padding: 6px 10px; color: #059669; font-weight: 600;">Opens at {checkInOpensFormatted} (1 hr before start)</td>
                            </tr>

                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>Booking ID:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a;">#{bookingId}</td>
                            </tr>

                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>Status:</strong></td>
                                <td style="padding: 6px 10px; color: #059669; font-weight: bold;">
                                    Confirmed
                                </td>
                            </tr>

                        </table>

                        <p style="margin: 8px 0 6px 0; font-size: 12px; color: #475569;">
                            Your hotseat has been successfully reserved. Check-in is available within 1 hour before the booking start time.
                        </p>

                        <p style="margin: 8px 0 0 0; font-size: 13px; color: #334155;">
                            Regards,<br>
                            <strong>SpaceBook</strong>
                        </p>

                    </td>
                </tr>

                <tr>
                    <td style="
                        text-align: center;
                        padding: 8px 14px;
                        background: #f1f5f9;
                        color: #64748b;
                        font-size: 11px;">

                        This is an automated notification from SpaceBook.

                    </td>
                </tr>

            </table>
        </body>
        </html>
        """;
    }

    private static string BuildAdminHotseatConfirmationEmailHtml(
        string employeeName,
        string employeeEmail,
        string department,
        int bookingId,
        string seatNumber,
        string moduleName,
        string officeName,
        string cityName,
        DateOnly bookingDate,
        string startTimeFormatted)
    {
        return $"""
        <!DOCTYPE html>
        <html>
        <head><meta charset="utf-8"><title>[Admin Alert] SpaceBook Hotseat Booking</title></head>
        <body style="font-family: Arial, sans-serif; background-color: #f4f6f9; padding: 12px; color: #1e293b; margin: 0;">
            <table align="center" width="100%" cellpadding="0" cellspacing="0" style="max-width: 520px; background: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 6px rgba(0,0,0,0.05);">
                <tr>
                    <td style="background: #0284c7; padding: 14px 18px; text-align: center; color: #ffffff;">
                        <h1 style="margin:0;font-size:19px;font-weight:700;">SpaceBook Admin Alert</h1>
                        <p style="margin:3px 0 0;font-size:13px;opacity:0.95;">New Hotseat Reservation Confirmed</p>
                    </td>
                </tr>
                <tr>
                    <td style="padding: 16px 20px;">
                        <h2 style="margin: 0 0 6px 0; font-size: 16px; color: #0f172a;">Administrator,</h2>
                        <p style="margin: 0 0 10px 0; font-size: 13px; line-height: 1.4; color: #334155;">
                            An employee has confirmed a hotseat reservation on SpaceBook.
                        </p>
                        <table width="100%" cellpadding="0" cellspacing="0" style="background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 6px; margin: 10px 0; font-size: 13px;">
                            <tr>
                                <td style="padding: 6px 10px; color: #64748b; width: 35%;"><strong>Employee:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a;">{employeeName} ({employeeEmail})</td>
                            </tr>
                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>Department:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a;">{department}</td>
                            </tr>
                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>Booking ID:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a;">#{bookingId}</td>
                            </tr>
                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>Seat:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a; font-weight: bold;">{seatNumber}</td>
                            </tr>
                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>Module:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a;">{moduleName}</td>
                            </tr>
                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>Office / Location:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a;">{officeName} ({cityName})</td>
                            </tr>
                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>Date:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a;">{bookingDate:MMMM dd, yyyy}</td>
                            </tr>
                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>Start Time:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a;">{startTimeFormatted}</td>
                            </tr>
                        </table>
                        <p style="margin: 8px 0 0 0; font-size: 13px; color: #334155;">Regards,<br><strong>SpaceBook Notification Service</strong></p>
                    </td>
                </tr>
                <tr>
                    <td style="text-align: center; padding: 8px 14px; background: #f1f5f9; color: #64748b; font-size: 11px;">
                        SpaceBook Workspace Administration
                    </td>
                </tr>
            </table>
        </body>
        </html>
        """;
    }

    private static string BuildHotseatReminderEmailHtml(
        string employeeName,
        int bookingId,
        string seatNumber,
        string moduleName,
        string officeName,
        string cityName,
        DateOnly bookingDate,
        string startTimeFormatted)
    {
        return $"""
        <!DOCTYPE html>
        <html>
        <head><meta charset="utf-8"><title>SpaceBook Hotseat Check-In Reminder</title></head>
        <body style="font-family: Arial, sans-serif; background-color: #f4f6f9; padding: 12px; color: #1e293b; margin: 0;">
            <table align="center" width="100%" cellpadding="0" cellspacing="0" style="max-width: 520px; background: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 6px rgba(0,0,0,0.05);">
                <tr>
                    <td style="background: #eab308; padding: 14px 18px; text-align: center; color: #ffffff;">
                        <h1 style="margin:0;font-size:19px;font-weight:700;">SpaceBook Reminder</h1>
                        <p style="margin:3px 0 0;font-size:13px;opacity:0.95;">Hotseat Check-In Window Is Now Open</p>
                    </td>
                </tr>
                <tr>
                    <td style="padding: 16px 20px;">
                        <h2 style="margin: 0 0 6px 0; font-size: 16px; color: #0f172a;">Hello {employeeName},</h2>
                        <p style="margin: 0 0 10px 0; font-size: 13px; line-height: 1.4; color: #334155;">
                            Your hotseat booking starts at <strong>{startTimeFormatted}</strong>. Please check in within the permitted check-in window to retain your seat.
                        </p>
                        <table width="100%" cellpadding="0" cellspacing="0" style="background: #fefce8; border: 1px solid #fef08a; border-radius: 6px; margin: 10px 0; font-size: 13px;">
                            <tr>
                                <td style="padding: 6px 10px; color: #854d0e; width: 35%;"><strong>Seat:</strong></td>
                                <td style="padding: 6px 10px; color: #713f12; font-weight: bold;">{seatNumber}</td>
                            </tr>
                            <tr>
                                <td style="padding: 6px 10px; color: #854d0e;"><strong>Module:</strong></td>
                                <td style="padding: 6px 10px; color: #713f12;">{moduleName}</td>
                            </tr>
                            <tr>
                                <td style="padding: 6px 10px; color: #854d0e;"><strong>Office / City:</strong></td>
                                <td style="padding: 6px 10px; color: #713f12;">{officeName} ({cityName})</td>
                            </tr>
                            <tr>
                                <td style="padding: 6px 10px; color: #854d0e;"><strong>Date:</strong></td>
                                <td style="padding: 6px 10px; color: #713f12;">{bookingDate:MMMM dd, yyyy}</td>
                            </tr>
                            <tr>
                                <td style="padding: 6px 10px; color: #854d0e;"><strong>Booking Time:</strong></td>
                                <td style="padding: 6px 10px; color: #713f12; font-weight: bold;">{startTimeFormatted}</td>
                            </tr>
                        </table>
                        <p style="margin: 8px 0 6px 0; font-size: 12px; color: #713f12;">
                            <strong>Note:</strong> If you do not check in by the scheduled start time, your reservation will automatically expire and the seat will be released for other colleagues.
                        </p>
                        <p style="margin: 8px 0 0 0; font-size: 13px; color: #334155;">Regards,<br><strong>SpaceBook Team</strong></p>
                    </td>
                </tr>
                <tr>
                    <td style="text-align: center; padding: 8px 14px; background: #f1f5f9; color: #64748b; font-size: 11px;">
                        This is an automated notification from SpaceBook.
                    </td>
                </tr>
            </table>
        </body>
        </html>
        """;
    }

    private static string BuildHotseatExpiredEmailHtml(
        string employeeName,
        int bookingId,
        string seatNumber,
        string moduleName,
        string officeName,
        string cityName,
        DateOnly bookingDate,
        string startTimeFormatted)
    {
        return $"""
        <!DOCTYPE html>
        <html>
        <head><meta charset="utf-8"><title>Hotseat Booking Expired – Seat Released</title></head>
        <body style="font-family: Arial, sans-serif; background-color: #f4f6f9; padding: 12px; color: #1e293b; margin: 0;">
            <table align="center" width="100%" cellpadding="0" cellspacing="0" style="max-width: 520px; background: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 6px rgba(0,0,0,0.05);">
                <tr>
                    <td style="background: #dc2626; padding: 14px 18px; text-align: center; color: #ffffff;">
                        <h1 style="margin:0;font-size:19px;font-weight:700;">SpaceBook Alert</h1>
                        <p style="margin:3px 0 0;font-size:13px;opacity:0.95;">Hotseat Booking Expired – Seat Released</p>
                    </td>
                </tr>
                <tr>
                    <td style="padding: 16px 20px;">
                        <h2 style="margin: 0 0 6px 0; font-size: 16px; color: #0f172a;">Hello {employeeName},</h2>
                        <p style="margin: 0 0 10px 0; font-size: 13px; line-height: 1.4; color: #334155;">
                            Your hotseat reservation was not checked in within the permitted time. The reservation has now expired and the seat has been released.
                        </p>
                        <table width="100%" cellpadding="0" cellspacing="0" style="background: #fef2f2; border: 1px solid #fee2e2; border-radius: 6px; margin: 10px 0; font-size: 13px;">
                            <tr>
                                <td style="padding: 6px 10px; color: #991b1b; width: 35%;"><strong>Booking ID:</strong></td>
                                <td style="padding: 6px 10px; color: #7f1d1d;">#{bookingId}</td>
                            </tr>
                            <tr>
                                <td style="padding: 6px 10px; color: #991b1b;"><strong>Seat:</strong></td>
                                <td style="padding: 6px 10px; color: #7f1d1d; font-weight: bold;">{seatNumber}</td>
                            </tr>
                            <tr>
                                <td style="padding: 6px 10px; color: #991b1b;"><strong>Module:</strong></td>
                                <td style="padding: 6px 10px; color: #7f1d1d;">{moduleName}</td>
                            </tr>
                            <tr>
                                <td style="padding: 6px 10px; color: #991b1b;"><strong>Office:</strong></td>
                                <td style="padding: 6px 10px; color: #7f1d1d;">{officeName} ({cityName})</td>
                            </tr>
                            <tr>
                                <td style="padding: 6px 10px; color: #991b1b;"><strong>Booking Date:</strong></td>
                                <td style="padding: 6px 10px; color: #7f1d1d;">{bookingDate:MMMM dd, yyyy}</td>
                            </tr>
                            <tr>
                                <td style="padding: 6px 10px; color: #991b1b;"><strong>Booking Time:</strong></td>
                                <td style="padding: 6px 10px; color: #7f1d1d;">{startTimeFormatted}</td>
                            </tr>
                            <tr>
                                <td style="padding: 6px 10px; color: #991b1b;"><strong>Status:</strong></td>
                                <td style="padding: 6px 10px; color: #dc2626; font-weight: bold;">Expired</td>
                            </tr>
                        </table>
                        <p style="margin: 8px 0 6px 0; font-size: 12px; color: #475569;">
                            Please make a new hotseat reservation if you still require a workspace.
                        </p>
                        <p style="margin: 8px 0 0 0; font-size: 13px; color: #334155;">Regards,<br><strong>SpaceBook Team</strong></p>
                    </td>
                </tr>
                <tr>
                    <td style="text-align: center; padding: 8px 14px; background: #f1f5f9; color: #64748b; font-size: 11px;">
                        This is an automated notification from SpaceBook.
                    </td>
                </tr>
            </table>
        </body>
        </html>
        """;
    }

    private static string BuildHotseatRescheduledEmailHtml(
        string employeeName,
        int bookingId,
        string seatNumber,
        string moduleName,
        string officeName,
        string cityName,
        DateOnly bookingDate,
        string startTimeFormatted,
        string checkInOpensFormatted)
    {
        return $"""
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <title>SpaceBook Hotseat Booking Rescheduled</title>
        </head>
        <body style="font-family: Arial, sans-serif; background-color: #f4f6f9; padding: 12px; color: #1e293b; margin: 0;">
            <table align="center" width="100%" cellpadding="0" cellspacing="0" style="max-width: 520px; background: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 6px rgba(0,0,0,0.05);">
                <tr>
                    <td style="background: #2563eb; padding: 14px 18px; text-align: center; color: #ffffff;">
                        <h1 style="margin:0;font-size:19px;font-weight:700;">SpaceBook</h1>
                        <p style="margin:3px 0 0;font-size:13px;opacity:0.95;">Hotseat Booking Rescheduled</p>
                    </td>
                </tr>
                <tr>
                    <td style="padding: 16px 20px;">
                        <h2 style="margin: 0 0 6px 0; font-size: 16px; color: #0f172a;">Hello {employeeName},</h2>
                        <p style="margin: 0 0 10px 0; font-size: 13px; line-height: 1.4; color: #334155;">
                            Your SpaceBook hotseat booking has been successfully rescheduled.
                        </p>
                        <table width="100%" cellpadding="0" cellspacing="0" style="background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 6px; margin: 10px 0; font-size: 13px;">
                            <tr>
                                <td style="padding: 6px 10px; color: #64748b; width: 35%;"><strong>Space Type:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a;">Hot Seat</td>
                            </tr>
                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>Seat:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a; font-weight: bold;">{seatNumber}</td>
                            </tr>
                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>Module:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a;">{moduleName}</td>
                            </tr>
                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>Office / Location:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a;">{officeName} ({cityName})</td>
                            </tr>
                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>Rescheduled Date:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a; font-weight: bold;">{bookingDate:MMMM dd, yyyy}</td>
                            </tr>
                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>Start Time:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a; font-weight: bold;">{startTimeFormatted}</td>
                            </tr>
                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>Check-In Window:</strong></td>
                                <td style="padding: 6px 10px; color: #2563eb; font-weight: 600;">Opens at {checkInOpensFormatted} (1 hr before start)</td>
                            </tr>
                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>Booking ID:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a;">#{bookingId}</td>
                            </tr>
                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>Status:</strong></td>
                                <td style="padding: 6px 10px; color: #2563eb; font-weight: bold;">Confirmed (Rescheduled)</td>
                            </tr>
                        </table>
                        <p style="margin: 8px 0 6px 0; font-size: 12px; color: #475569;">
                            Please check in to your workspace during the check-in window on your rescheduled booking date.
                        </p>
                        <p style="margin: 8px 0 0 0; font-size: 13px; color: #334155;">Regards,<br><strong>SpaceBook Team</strong></p>
                    </td>
                </tr>
                <tr>
                    <td style="text-align: center; padding: 8px 14px; background: #f1f5f9; color: #64748b; font-size: 11px;">
                        This is an automated notification from SpaceBook.
                    </td>
                </tr>
            </table>
        </body>
        </html>
        """;
    }

    private static string BuildAdminHotseatRescheduledEmailHtml(
        string employeeName,
        string employeeEmail,
        string department,
        int bookingId,
        string seatNumber,
        string moduleName,
        string officeName,
        string cityName,
        DateOnly bookingDate,
        string startTimeFormatted)
    {
        return $"""
        <!DOCTYPE html>
        <html>
        <head><meta charset="utf-8"><title>[Admin Alert] SpaceBook Hotseat Rescheduled</title></head>
        <body style="font-family: Arial, sans-serif; background-color: #f4f6f9; padding: 12px; color: #1e293b; margin: 0;">
            <table align="center" width="100%" cellpadding="0" cellspacing="0" style="max-width: 520px; background: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 6px rgba(0,0,0,0.05);">
                <tr>
                    <td style="background: #2563eb; padding: 14px 18px; text-align: center; color: #ffffff;">
                        <h1 style="margin:0;font-size:19px;font-weight:700;">SpaceBook Admin Alert</h1>
                        <p style="margin:3px 0 0;font-size:13px;opacity:0.95;">Hotseat Reservation Rescheduled</p>
                    </td>
                </tr>
                <tr>
                    <td style="padding: 16px 20px;">
                        <h2 style="margin: 0 0 6px 0; font-size: 16px; color: #0f172a;">Administrator,</h2>
                        <p style="margin: 0 0 10px 0; font-size: 13px; line-height: 1.4; color: #334155;">
                            An employee has rescheduled a hotseat reservation on SpaceBook.
                        </p>
                        <table width="100%" cellpadding="0" cellspacing="0" style="background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 6px; margin: 10px 0; font-size: 13px;">
                            <tr>
                                <td style="padding: 6px 10px; color: #64748b; width: 35%;"><strong>Employee:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a;">{employeeName} ({employeeEmail})</td>
                            </tr>
                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>Department:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a;">{department}</td>
                            </tr>
                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>Booking ID:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a;">#{bookingId}</td>
                            </tr>
                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>Seat:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a; font-weight: bold;">{seatNumber}</td>
                            </tr>
                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>Module:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a;">{moduleName}</td>
                            </tr>
                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>Office / Location:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a;">{officeName} ({cityName})</td>
                            </tr>
                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>Rescheduled Date:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a; font-weight: bold;">{bookingDate:MMMM dd, yyyy}</td>
                            </tr>
                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>Start Time:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a;">{startTimeFormatted}</td>
                            </tr>
                        </table>
                        <p style="margin: 8px 0 0 0; font-size: 13px; color: #334155;">Regards,<br><strong>SpaceBook Notification Service</strong></p>
                    </td>
                </tr>
                <tr>
                    <td style="text-align: center; padding: 8px 14px; background: #f1f5f9; color: #64748b; font-size: 11px;">
                        SpaceBook Workspace Administration
                    </td>
                </tr>
            </table>
        </body>
        </html>
        """;
    }

    private static string BuildHotseatCancelledEmailHtml(
        string employeeName,
        int bookingId,
        string seatNumber,
        string moduleName,
        string officeName,
        string cityName,
        DateOnly bookingDate,
        string startTimeFormatted,
        string cancellationReason)
    {
        return $"""
        <!DOCTYPE html>
        <html>
        <head><meta charset="utf-8"><title>SpaceBook Hotseat Booking Cancelled</title></head>
        <body style="font-family: Arial, sans-serif; background-color: #f4f6f9; padding: 12px; color: #1e293b; margin: 0;">
            <table align="center" width="100%" cellpadding="0" cellspacing="0" style="max-width: 520px; background: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 6px rgba(0,0,0,0.05);">
                <tr>
                    <td style="background: #64748b; padding: 14px 18px; text-align: center; color: #ffffff;">
                        <h1 style="margin:0;font-size:19px;font-weight:700;">SpaceBook</h1>
                        <p style="margin:3px 0 0;font-size:13px;opacity:0.95;">Hotseat Booking Cancelled</p>
                    </td>
                </tr>
                <tr>
                    <td style="padding: 16px 20px;">
                        <h2 style="margin: 0 0 6px 0; font-size: 16px; color: #0f172a;">Hello {employeeName},</h2>
                        <p style="margin: 0 0 10px 0; font-size: 13px; line-height: 1.4; color: #334155;">
                            Your hotseat reservation has been cancelled.
                        </p>
                        <table width="100%" cellpadding="0" cellspacing="0" style="background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 6px; margin: 10px 0; font-size: 13px;">
                            <tr>
                                <td style="padding: 6px 10px; color: #64748b; width: 35%;"><strong>Booking ID:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a;">#{bookingId}</td>
                            </tr>
                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>Seat:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a;">{seatNumber}</td>
                            </tr>
                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>Module:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a;">{moduleName}</td>
                            </tr>
                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>Office:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a;">{officeName} ({cityName})</td>
                            </tr>
                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>Date:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a;">{bookingDate:MMMM dd, yyyy}</td>
                            </tr>
                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>Scheduled Time:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a;">{startTimeFormatted}</td>
                            </tr>
                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>Reason:</strong></td>
                                <td style="padding: 6px 10px; color: #64748b;">{cancellationReason}</td>
                            </tr>
                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>Status:</strong></td>
                                <td style="padding: 6px 10px; color: #64748b; font-weight: bold;">Cancelled</td>
                            </tr>
                        </table>
                        <p style="margin: 8px 0 0 0; font-size: 13px; color: #334155;">Regards,<br><strong>SpaceBook Team</strong></p>
                    </td>
                </tr>
                <tr>
                    <td style="text-align: center; padding: 8px 14px; background: #f1f5f9; color: #64748b; font-size: 11px;">
                        This is an automated notification from SpaceBook.
                    </td>
                </tr>
            </table>
        </body>
        </html>
        """;
    }

    private static string BuildAdminHotseatCancelledEmailHtml(
        string employeeName,
        string employeeEmail,
        string department,
        int bookingId,
        string seatNumber,
        string moduleName,
        string officeName,
        string cityName,
        DateOnly bookingDate,
        string startTimeFormatted,
        string cancellationReason)
    {
        return $"""
        <!DOCTYPE html>
        <html>
        <head><meta charset="utf-8"><title>[Admin Alert] SpaceBook Hotseat Cancelled</title></head>
        <body style="font-family: Arial, sans-serif; background-color: #f4f6f9; padding: 12px; color: #1e293b; margin: 0;">
            <table align="center" width="100%" cellpadding="0" cellspacing="0" style="max-width: 520px; background: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 6px rgba(0,0,0,0.05);">
                <tr>
                    <td style="background: #e11d48; padding: 14px 18px; text-align: center; color: #ffffff;">
                        <h1 style="margin:0;font-size:19px;font-weight:700;">SpaceBook Admin Alert</h1>
                        <p style="margin:3px 0 0;font-size:13px;opacity:0.95;">Hotseat Reservation Cancelled</p>
                    </td>
                </tr>
                <tr>
                    <td style="padding: 16px 20px;">
                        <h2 style="margin: 0 0 6px 0; font-size: 16px; color: #0f172a;">Administrator,</h2>
                        <p style="margin: 0 0 10px 0; font-size: 13px; line-height: 1.4; color: #334155;">
                            A hotseat reservation has been cancelled by an employee.
                        </p>
                        <table width="100%" cellpadding="0" cellspacing="0" style="background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 6px; margin: 10px 0; font-size: 13px;">
                            <tr>
                                <td style="padding: 6px 10px; color: #64748b; width: 35%;"><strong>Employee:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a;">{employeeName} ({employeeEmail})</td>
                            </tr>
                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>Department:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a;">{department}</td>
                            </tr>
                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>Booking ID:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a;">#{bookingId}</td>
                            </tr>
                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>Seat:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a; font-weight: bold;">{seatNumber}</td>
                            </tr>
                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>Module:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a;">{moduleName}</td>
                            </tr>
                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>Office / Location:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a;">{officeName} ({cityName})</td>
                            </tr>
                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>Booking Date:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a;">{bookingDate:MMMM dd, yyyy}</td>
                            </tr>
                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>Scheduled Time:</strong></td>
                                <td style="padding: 6px 10px; color: #0f172a;">{startTimeFormatted}</td>
                            </tr>
                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>Reason:</strong></td>
                                <td style="padding: 6px 10px; color: #e11d48; font-weight: 600;">{cancellationReason}</td>
                            </tr>
                            <tr>
                                <td style="padding: 6px 10px; color: #64748b;"><strong>Status:</strong></td>
                                <td style="padding: 6px 10px; color: #e11d48; font-weight: bold;">Cancelled</td>
                            </tr>
                        </table>
                        <p style="margin: 8px 0 0 0; font-size: 13px; color: #334155;">Regards,<br><strong>SpaceBook Notification Service</strong></p>
                    </td>
                </tr>
                <tr>
                    <td style="text-align: center; padding: 8px 14px; background: #f1f5f9; color: #64748b; font-size: 11px;">
                        SpaceBook Workspace Administration
                    </td>
                </tr>
            </table>
        </body>
        </html>
        """;
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
}