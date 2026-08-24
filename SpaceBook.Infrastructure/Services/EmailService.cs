using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MailKit.Net.Smtp;
using MailKit.Security;
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
    // GENERIC EMAIL SENDER
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
        // 1. TRY RESEND HTTPS API
        // =====================================================

        var resendApiKey =
            _configuration["Resend:ApiKey"]
            ?? Environment.GetEnvironmentVariable("Resend__ApiKey")
            ?? _configuration["RESEND_API_KEY"]
            ?? Environment.GetEnvironmentVariable("RESEND_API_KEY");

        if (!string.IsNullOrWhiteSpace(resendApiKey))
        {
            try
            {
                _logger.LogInformation(
                    "Attempting email through Resend API. To={To}, Subject={Subject}",
                    toEmail,
                    subject);

                var resendFrom =
                    _configuration["Resend:From"]
                    ?? Environment.GetEnvironmentVariable("Resend__From")
                    ?? "SpaceBook <onboarding@resend.dev>";

                using var httpClient = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(15)
                };

                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue(
                        "Bearer",
                        resendApiKey.Trim());

                var payload = new
                {
                    from = resendFrom,
                    to = new[]
                    {
                        toEmail.Trim()
                    },
                    subject,
                    html = isHtml ? body : null,
                    text = !isHtml ? body : null
                };

                using var jsonContent =
                    new StringContent(
                        JsonSerializer.Serialize(payload),
                        Encoding.UTF8,
                        "application/json");

                var response =
                    await httpClient.PostAsync(
                        "https://api.resend.com/emails",
                        jsonContent);

                var responseBody =
                    await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation(
                        "Email successfully sent to {To} via Resend. Response={Response}",
                        toEmail,
                        responseBody);

                    return;
                }

                _logger.LogWarning(
                    "Resend failed. Status={StatusCode}, Response={Response}. Falling back to SMTP.",
                    response.StatusCode,
                    responseBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Resend email failed for {To}. Falling back to SMTP.",
                    toEmail);
            }
        }

        // =====================================================
        // 2. SMTP CONFIGURATION
        // =====================================================
        //
        // Supports:
        //
        // Render:
        // Smtp__Host
        // Smtp__Port
        // Smtp__Username
        // Smtp__Password
        // Smtp__From
        // Smtp__SenderName
        // Smtp__EnableSsl
        //
        // Also supports:
        // EmailSettings__...
        //
        // =====================================================

        var host =
            _configuration["Smtp:Host"]
            ?? Environment.GetEnvironmentVariable("Smtp__Host")
            ?? _configuration["EmailSettings:Host"]
            ?? Environment.GetEnvironmentVariable("EmailSettings__Host");

        var portValue =
            _configuration["Smtp:Port"]
            ?? Environment.GetEnvironmentVariable("Smtp__Port")
            ?? _configuration["EmailSettings:Port"]
            ?? Environment.GetEnvironmentVariable("EmailSettings__Port");

        var username =
            _configuration["Smtp:Username"]
            ?? Environment.GetEnvironmentVariable("Smtp__Username")
            ?? _configuration["EmailSettings:Username"]
            ?? Environment.GetEnvironmentVariable("EmailSettings__Username");

        var password =
            _configuration["Smtp:Password"]
            ?? Environment.GetEnvironmentVariable("Smtp__Password")
            ?? _configuration["EmailSettings:Password"]
            ?? Environment.GetEnvironmentVariable("EmailSettings__Password");

        var fromEmail =
            _configuration["Smtp:From"]
            ?? Environment.GetEnvironmentVariable("Smtp__From")
            ?? _configuration["EmailSettings:FromEmail"]
            ?? Environment.GetEnvironmentVariable("EmailSettings__FromEmail");

        var senderName =
            _configuration["Smtp:SenderName"]
            ?? Environment.GetEnvironmentVariable("Smtp__SenderName")
            ?? _configuration["EmailSettings:FromName"]
            ?? Environment.GetEnvironmentVariable("EmailSettings__FromName")
            ?? "SpaceBook";

        var enableSslValue =
            _configuration["Smtp:EnableSsl"]
            ?? Environment.GetEnvironmentVariable("Smtp__EnableSsl")
            ?? _configuration["EmailSettings:EnableSsl"]
            ?? Environment.GetEnvironmentVariable("EmailSettings__EnableSsl");

        // =====================================================
        // SAFE CONFIGURATION DIAGNOSTIC
        // =====================================================
        //
        // Does NOT print passwords or actual secret values.
        // =====================================================

        _logger.LogInformation(
            "SMTP configuration loaded: " +
            "Host={HostConfigured}, " +
            "Port={PortConfigured}, " +
            "Username={UsernameConfigured}, " +
            "Password={PasswordConfigured}, " +
            "From={FromConfigured}, " +
            "SSL={SslConfigured}",
            !string.IsNullOrWhiteSpace(host),
            !string.IsNullOrWhiteSpace(portValue),
            !string.IsNullOrWhiteSpace(username),
            !string.IsNullOrWhiteSpace(password),
            !string.IsNullOrWhiteSpace(fromEmail),
            !string.IsNullOrWhiteSpace(enableSslValue));

        // =====================================================
        // VALIDATE SMTP CONFIGURATION
        // =====================================================

        if (string.IsNullOrWhiteSpace(host))
        {
            throw new InvalidOperationException(
                "SMTP Host is not configured.");
        }

        if (string.IsNullOrWhiteSpace(username))
        {
            throw new InvalidOperationException(
                "SMTP Username is not configured.");
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "SMTP Password is not configured.");
        }

        if (string.IsNullOrWhiteSpace(fromEmail))
        {
            fromEmail = username;
        }

        // =====================================================
        // PORT
        // =====================================================

        var port = 587;

        if (!string.IsNullOrWhiteSpace(portValue) &&
            int.TryParse(
                portValue,
                out var configuredPort))
        {
            port = configuredPort;
        }

        // =====================================================
        // SSL
        // =====================================================

        var enableSsl = true;

        if (!string.IsNullOrWhiteSpace(enableSslValue) &&
            bool.TryParse(
                enableSslValue,
                out var configuredSsl))
        {
            enableSsl = configuredSsl;
        }

        // =====================================================
        // SEND USING SMTP
        // =====================================================

        try
        {
            _logger.LogInformation(
                "Attempting SMTP email. To={To}, Host={Host}, Port={Port}",
                toEmail,
                host,
                port);

            var cleanPassword =
                password
                    .Replace(" ", "")
                    .Trim();

            var message = new MimeMessage();

            message.From.Add(
                new MailboxAddress(
                    senderName,
                    fromEmail));

            message.To.Add(
                MailboxAddress.Parse(
                    toEmail.Trim()));

            message.Subject = subject;

            var bodyBuilder = new BodyBuilder();

            if (isHtml)
            {
                bodyBuilder.HtmlBody = body;
            }
            else
            {
                bodyBuilder.TextBody = body;
            }

            message.Body =
                bodyBuilder.ToMessageBody();

            using var client =
                new SmtpClient();

            client.Timeout = 15000;

            // Gmail:
            // Port 587 -> STARTTLS
            // Port 465 -> SSL on connect
            var secureSocketOption =
                !enableSsl
                    ? SecureSocketOptions.None
                    : port == 465
                        ? SecureSocketOptions.SslOnConnect
                        : SecureSocketOptions.StartTls;

            await client.ConnectAsync(
                host,
                port,
                secureSocketOption);

            await client.AuthenticateAsync(
                username.Trim(),
                cleanPassword);

            await client.SendAsync(
                message);

            await client.DisconnectAsync(
                true);

            _logger.LogInformation(
                "Email successfully sent to {To}. Subject={Subject}",
                toEmail,
                subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "SMTP email failed. To={To}, Host={Host}, Port={Port}",
                toEmail,
                host,
                port);

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
            // Do not swallow exceptions.
            // Caller must know if delivery failed.
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
            !string.IsNullOrWhiteSpace(
                employee?.Name)
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

        var purpose =
            booking.Purpose
            ?? string.Empty;

        // =====================================================
        // EMPLOYEE CONFIRMATION
        // =====================================================

        if (string.IsNullOrWhiteSpace(employeeEmail))
        {
            throw new InvalidOperationException(
                $"Employee email is missing for BookingId={booking.BookingId}.");
        }

        var employeeSubject =
            $"SpaceBook Booking Confirmed - {meetingTitle}";

        var employeeBody =
            BuildConfirmationEmailHtml(
                employeeName,
                meetingTitle,
                purpose,
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
                    purpose,
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
    // 15-MINUTE START REMINDER
    // =========================================================

    public async Task SendBookingStartReminderAsync(
        Booking booking,
        Employee employee,
        Room room,
        IEnumerable<string> adminEmails)
    {
        var employeeName =
            !string.IsNullOrWhiteSpace(
                employee?.Name)
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

        var purpose =
            booking.Purpose
            ?? string.Empty;

        // =====================================================
        // EMPLOYEE START REMINDER
        // =====================================================

        if (string.IsNullOrWhiteSpace(employeeEmail))
        {
            throw new InvalidOperationException(
                $"Employee email is missing for BookingId={booking.BookingId}.");
        }

        const string employeeSubject =
            "SpaceBook Reminder - Booking Starts in 15 Minutes";

        var employeeBody =
            BuildStartReminderEmailHtml(
                employeeName,
                meetingTitle,
                purpose,
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
        // ADMIN START REMINDER
        // =====================================================

        var adminList =
            ResolveAdminEmails(
                adminEmails);

        if (adminList.Count > 0)
        {
            var adminSubject =
                $"[Admin Alert] SpaceBook Reminder - Booking Starts in 15 Minutes: {meetingTitle}";

            var adminBody =
                BuildAdminStartReminderEmailHtml(
                    employeeName,
                    employeeEmail,
                    employee?.Department
                        ?? string.Empty,
                    meetingTitle,
                    purpose,
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
                "Start reminder sent to employee but no Admin recipient was configured. BookingId={BookingId}",
                booking.BookingId);
        }
    }

    // =========================================================
    // NOTIFICATION 3
    // 15-MINUTE END REMINDER
    // =========================================================

    public async Task SendBookingEndReminderAsync(
        Booking booking,
        Employee employee,
        Room room,
        IEnumerable<string> adminEmails)
    {
        var employeeName =
            !string.IsNullOrWhiteSpace(
                employee?.Name)
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

        // =====================================================
        // EMPLOYEE END REMINDER
        // =====================================================

        if (string.IsNullOrWhiteSpace(employeeEmail))
        {
            throw new InvalidOperationException(
                $"Employee email is missing for BookingId={booking.BookingId}.");
        }

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

        // =====================================================
        // ADMIN END REMINDER
        // =====================================================

        var adminList =
            ResolveAdminEmails(
                adminEmails);

        if (adminList.Count > 0)
        {
            var adminSubject =
                $"[Admin Alert] SpaceBook Reminder - Booking Ends in 15 Minutes: {meetingTitle}";

            var adminBody =
                BuildAdminEndReminderEmailHtml(
                    employeeName,
                    employeeEmail,
                    meetingTitle,
                    roomName,
                    booking.BookingDate,
                    booking.StartTime,
                    booking.EndTime);

            await SendEmailsAsync(
                adminList,
                adminSubject,
                adminBody,
                true);
        }
        else
        {
            _logger.LogWarning(
                "End reminder sent to employee but no Admin recipient was configured. BookingId={BookingId}",
                booking.BookingId);
        }
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
            _configuration["Smtp:AdminEmail"]
            ?? Environment.GetEnvironmentVariable("Smtp__AdminEmail")
            ?? _configuration["EmailSettings:AdminEmail"]
            ?? Environment.GetEnvironmentVariable("EmailSettings__AdminEmail")
            ?? _configuration["Resend:AdminEmail"]
            ?? Environment.GetEnvironmentVariable("Resend__AdminEmail");

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
        string purpose,
        string roomName,
        DateOnly bookingDate,
        TimeOnly startTime,
        TimeOnly endTime,
        int participantCount)
    {
        var purposeRow =
            !string.IsNullOrWhiteSpace(purpose)
                ? $"""
                   <tr>
                       <td><strong>Purpose:</strong></td>
                       <td>{purpose}</td>
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

                            {purposeRow}

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
        string purpose,
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

        var purposeRow =
            !string.IsNullOrWhiteSpace(purpose)
                ? $"""
                   <tr>
                       <td><strong>Purpose:</strong></td>
                       <td>{purpose}</td>
                   </tr>
                   """
                : string.Empty;

        return $"""
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <title>SpaceBook Admin Booking Alert</title>
        </head>

        <body style="
            font-family:Arial,sans-serif;
            background:#f4f6f9;
            padding:24px;">

            <div style="
                max-width:580px;
                margin:0 auto;
                background:#ffffff;
                border-radius:10px;
                padding:24px;
                border-left:4px solid #10b981;">

                <h2 style="
                    color:#047857;
                    margin-top:0;">
                    Room Booking Confirmed
                </h2>

                <p>
                    A room booking has been automatically approved.
                </p>

                <table
                    width="100%"
                    cellpadding="7"
                    cellspacing="0"
                    style="
                        background:#f8fafc;
                        border:1px solid #e2e8f0;">

                    <tr>
                        <td><strong>Employee:</strong></td>
                        <td>
                            {employeeName}
                            ({employeeEmail})
                        </td>
                    </tr>

                    {departmentRow}

                    <tr>
                        <td><strong>Meeting:</strong></td>
                        <td>{meetingTitle}</td>
                    </tr>

                    {purposeRow}

                    <tr>
                        <td><strong>Room:</strong></td>
                        <td>{roomName}</td>
                    </tr>

                    <tr>
                        <td><strong>Date:</strong></td>
                        <td>{bookingDate:MMMM dd, yyyy}</td>
                    </tr>

                    <tr>
                        <td><strong>Start:</strong></td>
                        <td>{FormatTime(startTime)}</td>
                    </tr>

                    <tr>
                        <td><strong>End:</strong></td>
                        <td>{FormatTime(endTime)}</td>
                    </tr>

                    <tr>
                        <td><strong>Participants:</strong></td>
                        <td>{participantCount}</td>
                    </tr>

                    <tr>
                        <td><strong>Status:</strong></td>
                        <td>Approved</td>
                    </tr>

                </table>

                <p>
                    Regards,<br>
                    <strong>SpaceBook</strong>
                </p>
            </div>
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
        string purpose,
        string roomName,
        DateOnly bookingDate,
        TimeOnly startTime,
        TimeOnly endTime,
        int participantCount)
    {
        var purposeRow =
            !string.IsNullOrWhiteSpace(purpose)
                ? $"""
                   <tr>
                       <td><strong>Purpose:</strong></td>
                       <td>{purpose}</td>
                   </tr>
                   """
                : string.Empty;

        return $"""
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <title>
                SpaceBook Reminder - Booking Starts in 15 Minutes
            </title>
        </head>

        <body style="
            font-family:Arial,sans-serif;
            background:#f4f6f9;
            padding:24px;">

            <div style="
                max-width:580px;
                margin:0 auto;
                background:#ffffff;
                padding:28px;
                border-radius:12px;">

                <h1>SpaceBook</h1>

                <h2>
                    Hello {employeeName},
                </h2>

                <p>
                    This is a reminder that your SpaceBook room booking
                    will start in <strong>15 minutes</strong>.
                </p>

                <table
                    width="100%"
                    cellpadding="8"
                    cellspacing="0"
                    style="
                        background:#f8fafc;
                        border:1px solid #e2e8f0;">

                    <tr>
                        <td><strong>Meeting:</strong></td>
                        <td>{meetingTitle}</td>
                    </tr>

                    {purposeRow}

                    <tr>
                        <td><strong>Room:</strong></td>
                        <td>{roomName}</td>
                    </tr>

                    <tr>
                        <td><strong>Date:</strong></td>
                        <td>{bookingDate:MMMM dd, yyyy}</td>
                    </tr>

                    <tr>
                        <td><strong>Start:</strong></td>
                        <td>{FormatTime(startTime)}</td>
                    </tr>

                    <tr>
                        <td><strong>End:</strong></td>
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
            </div>
        </body>
        </html>
        """;
    }

    // =========================================================
    // ADMIN START REMINDER EMAIL
    // =========================================================

    private static string BuildAdminStartReminderEmailHtml(
        string employeeName,
        string employeeEmail,
        string department,
        string meetingTitle,
        string purpose,
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

        var purposeRow =
            !string.IsNullOrWhiteSpace(purpose)
                ? $"""
                   <tr>
                       <td><strong>Purpose:</strong></td>
                       <td>{purpose}</td>
                   </tr>
                   """
                : string.Empty;

        return $"""
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <title>SpaceBook Start Reminder</title>
        </head>

        <body style="
            font-family:Arial,sans-serif;
            background:#f4f6f9;
            padding:24px;">

            <div style="
                max-width:580px;
                margin:auto;
                background:#ffffff;
                padding:24px;
                border-left:4px solid #2563eb;">

                <h2>
                    Booking Starts in 15 Minutes
                </h2>

                <table
                    width="100%"
                    cellpadding="7">

                    <tr>
                        <td><strong>Employee:</strong></td>
                        <td>
                            {employeeName}
                            ({employeeEmail})
                        </td>
                    </tr>

                    {departmentRow}

                    <tr>
                        <td><strong>Meeting:</strong></td>
                        <td>{meetingTitle}</td>
                    </tr>

                    {purposeRow}

                    <tr>
                        <td><strong>Room:</strong></td>
                        <td>{roomName}</td>
                    </tr>

                    <tr>
                        <td><strong>Date:</strong></td>
                        <td>{bookingDate:MMMM dd, yyyy}</td>
                    </tr>

                    <tr>
                        <td><strong>Start:</strong></td>
                        <td>{FormatTime(startTime)}</td>
                    </tr>

                    <tr>
                        <td><strong>End:</strong></td>
                        <td>{FormatTime(endTime)}</td>
                    </tr>

                    <tr>
                        <td><strong>Participants:</strong></td>
                        <td>{participantCount}</td>
                    </tr>

                </table>

                <p>
                    Regards,<br>
                    <strong>SpaceBook</strong>
                </p>

            </div>
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
            <title>
                SpaceBook Reminder - Booking Ends in 15 Minutes
            </title>
        </head>

        <body style="
            font-family:Arial,sans-serif;
            background:#f4f6f9;
            padding:24px;">

            <div style="
                max-width:580px;
                margin:auto;
                background:#ffffff;
                padding:28px;
                border-radius:12px;">

                <h1>
                    SpaceBook
                </h1>

                <h2>
                    Hello {employeeName},
                </h2>

                <p>
                    Your SpaceBook room booking will end in
                    <strong>15 minutes</strong>.
                </p>

                <table
                    width="100%"
                    cellpadding="8">

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
                        <td><strong>Start:</strong></td>
                        <td>{FormatTime(startTime)}</td>
                    </tr>

                    <tr>
                        <td><strong>End:</strong></td>
                        <td>{FormatTime(endTime)}</td>
                    </tr>

                </table>

                <p>
                    Please complete your meeting and
                    vacate the room on time.
                </p>

                <p>
                    Regards,<br>
                    <strong>SpaceBook</strong>
                </p>

            </div>
        </body>
        </html>
        """;
    }

    // =========================================================
    // ADMIN END REMINDER EMAIL
    // =========================================================

    private static string BuildAdminEndReminderEmailHtml(
        string employeeName,
        string employeeEmail,
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
            <title>SpaceBook End Reminder</title>
        </head>

        <body style="
            font-family:Arial,sans-serif;
            background:#f4f6f9;
            padding:24px;">

            <div style="
                max-width:580px;
                margin:auto;
                background:#ffffff;
                padding:24px;
                border-left:4px solid #d97706;">

                <h2>
                    Booking Ends in 15 Minutes
                </h2>

                <table
                    width="100%"
                    cellpadding="7">

                    <tr>
                        <td><strong>Employee:</strong></td>
                        <td>
                            {employeeName}
                            ({employeeEmail})
                        </td>
                    </tr>

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
                        <td><strong>Start:</strong></td>
                        <td>{FormatTime(startTime)}</td>
                    </tr>

                    <tr>
                        <td><strong>End:</strong></td>
                        <td>{FormatTime(endTime)}</td>
                    </tr>

                </table>

                <p>
                    Regards,<br>
                    <strong>SpaceBook</strong>
                </p>

            </div>
        </body>
        </html>
        """;
    }
}