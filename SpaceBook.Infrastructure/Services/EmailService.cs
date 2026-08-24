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

    public async Task SendEmailAsync(
        string toEmail,
        string subject,
        string body,
        bool isHtml = true)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
        {
            _logger.LogWarning(
                "Email recipient is empty. Email was not sent.");
            return;
        }

        // =====================================================
        // 1. TRY RESEND HTTPS API (PORT 443 - NEVER BLOCKED)
        // =====================================================
        var resendApiKey = _configuration["Resend:ApiKey"]
                           ?? _configuration["Resend__ApiKey"]
                           ?? _configuration["RESEND_API_KEY"]
                           ?? _configuration["Resend_ApiKey"]
                           ?? Environment.GetEnvironmentVariable("Resend__ApiKey")
                           ?? Environment.GetEnvironmentVariable("Resend_ApiKey")
                           ?? Environment.GetEnvironmentVariable("RESEND_API_KEY");

        if (!string.IsNullOrWhiteSpace(resendApiKey))
        {
            try
            {
                _logger.LogInformation(
                    "Attempting to send email via Resend HTTPS API. To={To}, Subject={Subject}",
                    toEmail,
                    subject);

                var resendFrom = _configuration["Resend:From"] ?? "SpaceBook <onboarding@resend.dev>";
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(15);
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", resendApiKey.Trim());

                var payload = new
                {
                    from = resendFrom,
                    to = new[] { toEmail.Trim() },
                    subject = subject,
                    html = isHtml ? body : null,
                    text = !isHtml ? body : null
                };

                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json");

                var response = await httpClient.PostAsync(
                    "https://api.resend.com/emails",
                    jsonContent);

                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation(
                        "Email successfully sent to {To} via Resend HTTPS API. Response: {Response}",
                        toEmail,
                        responseBody);
                    return;
                }

                _logger.LogWarning(
                    "Resend API returned status {StatusCode}: {Error}. Attempting SMTP fallback.",
                    response.StatusCode,
                    responseBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Resend API dispatch failed. Attempting SMTP fallback.");
            }
        }

        // =====================================================
        // 2. SMTP FALLBACK (MAILKIT)
        // =====================================================
        var host = _configuration["EmailSettings:Host"] ?? _configuration["Smtp:Host"];
        var portValue = _configuration["EmailSettings:Port"] ?? _configuration["Smtp:Port"];
        var username = _configuration["EmailSettings:Username"] ?? _configuration["Smtp:Username"];
        var password = _configuration["EmailSettings:Password"] ?? _configuration["Smtp:Password"];
        var fromEmail = _configuration["EmailSettings:FromEmail"] ?? _configuration["Smtp:From"];
        var senderName = _configuration["EmailSettings:FromName"]
                         ?? _configuration["Smtp:SenderName"]
                         ?? "SpaceBook";

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning(
                "SMTP is not configured (Host/Username/Password missing). Skipping SMTP delivery.");
            return;
        }

        if (string.IsNullOrWhiteSpace(fromEmail))
        {
            fromEmail = username;
        }

        var port = 587;
        if (!string.IsNullOrWhiteSpace(portValue) && int.TryParse(portValue, out var configuredPort))
        {
            port = configuredPort;
        }

        if (host.Contains("gmail.com", StringComparison.OrdinalIgnoreCase) && port == 587)
        {
            port = 465;
        }

        try
        {
            _logger.LogInformation(
                "Attempting to send email via SMTP. To={To}, Host={Host}, Port={Port}",
                toEmail,
                host,
                port);

            var cleanPassword = password.Replace(" ", "").Trim();

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(senderName, fromEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
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

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            client.Timeout = 10000;

            var secureSocketOption = port == 465
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.StartTls;

            await client.ConnectAsync(host, port, secureSocketOption);
            await client.AuthenticateAsync(username.Trim(), cleanPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation(
                "Email successfully sent to {To}. Subject={Subject}",
                toEmail,
                subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send email to {To}. SMTP={Host}:{Port}",
                toEmail,
                host,
                port);
            throw;
        }
    }

    public async Task SendEmailsAsync(
        IEnumerable<string> toEmails,
        string subject,
        string body,
        bool isHtml = true)
    {
        var validEmails = toEmails?
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Select(e => e.Trim())
            .Distinct()
            .ToList();

        if (validEmails == null || validEmails.Count == 0)
        {
            _logger.LogWarning("Email recipient list is empty. Skipping email dispatch.");
            return;
        }

        foreach (var email in validEmails)
        {
            try
            {
                await SendEmailAsync(email, subject, body, isHtml);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send batch email to {To}", email);
            }
        }
    }

    // =========================================================
    // 1. NOTIFICATION 1: BOOKING CONFIRMATION
    // =========================================================

    public async Task SendBookingConfirmationAsync(
        Booking booking,
        Employee employee,
        Room room,
        IEnumerable<string> adminEmails)
    {
        var employeeName = employee?.Name ?? "Colleague";
        var employeeEmail = employee?.Email;
        var roomName = !string.IsNullOrWhiteSpace(room?.RoomName) ? room.RoomName : (room?.RoomNumber ?? "Meeting Room");
        var meetingTitle = !string.IsNullOrWhiteSpace(booking.MeetingTitle) ? booking.MeetingTitle : "Room Booking";
        var purpose = booking.Purpose ?? string.Empty;

        // 1. Send confirmation to Employee
        if (!string.IsNullOrWhiteSpace(employeeEmail))
        {
            try
            {
                var empSubject = $"SpaceBook Booking Confirmed - {meetingTitle}";
                var empBody = BuildConfirmationEmailHtml(
                    employeeName,
                    meetingTitle,
                    purpose,
                    roomName,
                    booking.BookingDate,
                    booking.StartTime,
                    booking.EndTime,
                    booking.ParticipantCount);

                await SendEmailAsync(employeeEmail, empSubject, empBody, isHtml: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send booking confirmation email to employee {Email}", employeeEmail);
            }
        }

        // 2. Send alert to Admins
        var adminList = ResolveAdminEmails(adminEmails);
        if (adminList.Count > 0)
        {
            try
            {
                var adminSubject = $"[Admin Alert] SpaceBook Booking Confirmed - {meetingTitle}";
                var adminBody = BuildAdminConfirmationEmailHtml(
                    employeeName,
                    employeeEmail ?? string.Empty,
                    employee?.Department ?? string.Empty,
                    meetingTitle,
                    purpose,
                    roomName,
                    booking.BookingDate,
                    booking.StartTime,
                    booking.EndTime,
                    booking.ParticipantCount);

                await SendEmailsAsync(adminList, adminSubject, adminBody, isHtml: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send booking confirmation alert to admins");
            }
        }
    }

    // =========================================================
    // 2. NOTIFICATION 2: 15-MINUTE START REMINDER
    // =========================================================

    public async Task SendBookingStartReminderAsync(
        Booking booking,
        Employee employee,
        Room room,
        IEnumerable<string> adminEmails)
    {
        var employeeName = employee?.Name ?? "Colleague";
        var employeeEmail = employee?.Email;
        var roomName = !string.IsNullOrWhiteSpace(room?.RoomName) ? room.RoomName : (room?.RoomNumber ?? "Meeting Room");
        var meetingTitle = !string.IsNullOrWhiteSpace(booking.MeetingTitle) ? booking.MeetingTitle : "Room Booking";
        var purpose = booking.Purpose ?? string.Empty;

        // 1. Send start reminder to Employee
        if (!string.IsNullOrWhiteSpace(employeeEmail))
        {
            try
            {
                var empSubject = "SpaceBook Reminder - Booking Starts in 15 Minutes";
                var empBody = BuildStartReminderEmailHtml(
                    employeeName,
                    meetingTitle,
                    purpose,
                    roomName,
                    booking.BookingDate,
                    booking.StartTime,
                    booking.EndTime,
                    booking.ParticipantCount);

                await SendEmailAsync(employeeEmail, empSubject, empBody, isHtml: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send 15-minute start reminder email to employee {Email}", employeeEmail);
            }
        }

        // 2. Send start reminder to Admins
        var adminList = ResolveAdminEmails(adminEmails);
        if (adminList.Count > 0)
        {
            try
            {
                var adminSubject = $"[Admin Alert] SpaceBook Reminder - Booking Starts in 15 Minutes: {meetingTitle}";
                var adminBody = BuildAdminStartReminderEmailHtml(
                    employeeName,
                    employeeEmail ?? string.Empty,
                    employee?.Department ?? string.Empty,
                    meetingTitle,
                    purpose,
                    roomName,
                    booking.BookingDate,
                    booking.StartTime,
                    booking.EndTime,
                    booking.ParticipantCount);

                await SendEmailsAsync(adminList, adminSubject, adminBody, isHtml: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send 15-minute start reminder alert to admins");
            }
        }
    }

    // =========================================================
    // 3. NOTIFICATION 3: 15-MINUTE END REMINDER
    // =========================================================

    public async Task SendBookingEndReminderAsync(
        Booking booking,
        Employee employee,
        Room room,
        IEnumerable<string> adminEmails)
    {
        var employeeName = employee?.Name ?? "Colleague";
        var employeeEmail = employee?.Email;
        var roomName = !string.IsNullOrWhiteSpace(room?.RoomName) ? room.RoomName : (room?.RoomNumber ?? "Meeting Room");
        var meetingTitle = !string.IsNullOrWhiteSpace(booking.MeetingTitle) ? booking.MeetingTitle : "Room Booking";

        // 1. Send end reminder to Employee
        if (!string.IsNullOrWhiteSpace(employeeEmail))
        {
            try
            {
                var empSubject = "SpaceBook Reminder - Booking Ends in 15 Minutes";
                var empBody = BuildEndReminderEmailHtml(
                    employeeName,
                    meetingTitle,
                    roomName,
                    booking.BookingDate,
                    booking.StartTime,
                    booking.EndTime);

                await SendEmailAsync(employeeEmail, empSubject, empBody, isHtml: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send 15-minute end reminder email to employee {Email}", employeeEmail);
            }
        }

        // 2. Send end reminder to Admins
        var adminList = ResolveAdminEmails(adminEmails);
        if (adminList.Count > 0)
        {
            try
            {
                var adminSubject = $"[Admin Alert] SpaceBook Reminder - Booking Ends in 15 Minutes: {meetingTitle}";
                var adminBody = BuildAdminEndReminderEmailHtml(
                    employeeName,
                    employeeEmail ?? string.Empty,
                    meetingTitle,
                    roomName,
                    booking.BookingDate,
                    booking.StartTime,
                    booking.EndTime);

                await SendEmailsAsync(adminList, adminSubject, adminBody, isHtml: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send 15-minute end reminder alert to admins");
            }
        }
    }

    // =========================================================
    // HELPER: RESOLVE ADMIN EMAILS
    // =========================================================

    private List<string> ResolveAdminEmails(IEnumerable<string>? passedAdminEmails)
    {
        var list = new List<string>();

        if (passedAdminEmails != null)
        {
            list.AddRange(passedAdminEmails.Where(e => !string.IsNullOrWhiteSpace(e)));
        }

        var configAdminEmail = _configuration["EmailSettings:AdminEmail"]
                               ?? _configuration["Smtp:AdminEmail"]
                               ?? _configuration["Resend:AdminEmail"];

        if (!string.IsNullOrWhiteSpace(configAdminEmail))
        {
            list.Add(configAdminEmail.Trim());
        }

        return list.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    // =========================================================
    // HTML TEMPLATES
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
        return $"""
        <!DOCTYPE html>
        <html>
        <head><meta charset="utf-8"><title>SpaceBook Booking Confirmed</title></head>
        <body style="font-family: Arial, sans-serif; background-color: #f4f6f9; padding: 24px; color: #1e293b; margin: 0;">
            <table align="center" width="100%" cellpadding="0" cellspacing="0" style="max-width: 580px; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.06);">
                <tr>
                    <td style="background: linear-gradient(135deg, #10b981 0%, #059669 100%); padding: 28px; text-align: center; color: #ffffff;">
                        <h1 style="margin: 0; font-size: 24px; font-weight: 700;">SpaceBook</h1>
                        <p style="margin: 6px 0 0; font-size: 14px; color: #d1fae5;">Room Booking Confirmed</p>
                    </td>
                </tr>
                <tr>
                    <td style="padding: 28px;">
                        <h2 style="color: #0f172a; margin-top: 0; font-size: 18px;">Hello {employeeName},</h2>
                        <p style="color: #475569; font-size: 15px; line-height: 1.6;">Your SpaceBook room booking has been confirmed successfully.</p>
                        <table width="100%" cellpadding="10" cellspacing="0" style="background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; margin: 20px 0;">
                            <tr><td width="35%" style="color: #64748b; font-weight: 600;">Meeting:</td><td style="color: #0f172a; font-weight: 600;">{meetingTitle}</td></tr>
                            {(!string.IsNullOrWhiteSpace(purpose) ? $"<tr><td style=\"color: #64748b; font-weight: 600;\">Purpose:</td><td style=\"color: #0f172a;\">{purpose}</td></tr>" : "")}
                            <tr><td style="color: #64748b; font-weight: 600;">Room:</td><td style="color: #0f172a;">{roomName}</td></tr>
                            <tr><td style="color: #64748b; font-weight: 600;">Date:</td><td style="color: #0f172a;">{bookingDate:MMMM dd, yyyy}</td></tr>
                            <tr><td style="color: #64748b; font-weight: 600;">Start Time:</td><td style="color: #059669; font-weight: 600;">{startTime:hh\\:mm tt}</td></tr>
                            <tr><td style="color: #64748b; font-weight: 600;">End Time:</td><td style="color: #059669; font-weight: 600;">{endTime:hh\\:mm tt}</td></tr>
                            <tr><td style="color: #64748b; font-weight: 600;">Participants:</td><td style="color: #0f172a;">{participantCount}</td></tr>
                            <tr><td style="color: #64748b; font-weight: 600;">Status:</td><td style="color: #10b981; font-weight: 700;">Approved</td></tr>
                        </table>
                        <p style="color: #475569; font-size: 15px; margin: 16px 0 24px;">Your room has been successfully reserved.</p>
                        <p style="color: #64748b; font-size: 14px; margin: 0;">Regards,<br><strong>SpaceBook</strong></p>
                    </td>
                </tr>
                <tr>
                    <td style="text-align: center; padding: 16px; background: #f1f5f9; color: #64748b; font-size: 12px;">
                        This is an automated notification from SpaceBook.
                    </td>
                </tr>
            </table>
        </body>
        </html>
        """;
    }

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
        return $"""
        <!DOCTYPE html>
        <html>
        <head><meta charset="utf-8"><title>New Booking Confirmed Alert</title></head>
        <body style="font-family: Arial, sans-serif; background-color: #f4f6f9; padding: 24px; color: #1e293b; margin: 0;">
            <div style="max-width: 580px; margin: 0 auto; background: #ffffff; border-radius: 10px; padding: 24px; border-left: 4px solid #10b981; box-shadow: 0 2px 8px rgba(0,0,0,0.05);">
                <h2 style="color: #047857; margin-top: 0;">[Admin Alert] Room Booking Confirmed</h2>
                <p style="color: #475569;">A room booking has been auto-approved in SpaceBook:</p>
                <table width="100%" cellpadding="6" cellspacing="0" style="background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 6px; margin: 16px 0;">
                    <tr><td width="35%"><strong>Booked By:</strong></td><td>{employeeName} ({employeeEmail})</td></tr>
                    {(!string.IsNullOrWhiteSpace(department) ? $"<tr><td><strong>Department:</strong></td><td>{department}</td></tr>" : "")}
                    <tr><td><strong>Meeting:</strong></td><td>{meetingTitle}</td></tr>
                    {(!string.IsNullOrWhiteSpace(purpose) ? $"<tr><td><strong>Purpose:</strong></td><td>{purpose}</td></tr>" : "")}
                    <tr><td><strong>Room:</strong></td><td>{roomName}</td></tr>
                    <tr><td><strong>Date:</strong></td><td>{bookingDate:MMMM dd, yyyy}</td></tr>
                    <tr><td><strong>Time:</strong></td><td>{startTime:hh\\:mm tt} - {endTime:hh\\:mm tt}</td></tr>
                    <tr><td><strong>Participants:</strong></td><td>{participantCount}</td></tr>
                    <tr><td><strong>Status:</strong></td><td style="color: #10b981; font-weight: bold;">Approved</td></tr>
                </table>
                <p style="color: #64748b; font-size: 13px; margin: 0;">Regards,<br><strong>SpaceBook</strong></p>
            </div>
        </body>
        </html>
        """;
    }

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
        return $"""
        <!DOCTYPE html>
        <html>
        <head><meta charset="utf-8"><title>SpaceBook Reminder - Booking Starts in 15 Minutes</title></head>
        <body style="font-family: Arial, sans-serif; background-color: #f4f6f9; padding: 24px; color: #1e293b; margin: 0;">
            <table align="center" width="100%" cellpadding="0" cellspacing="0" style="max-width: 580px; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.06);">
                <tr>
                    <td style="background: linear-gradient(135deg, #2563eb 0%, #1d4ed8 100%); padding: 28px; text-align: center; color: #ffffff;">
                        <h1 style="margin: 0; font-size: 24px; font-weight: 700;">SpaceBook</h1>
                        <p style="margin: 6px 0 0; font-size: 14px; color: #bfdbfe;">Meeting Reminder</p>
                    </td>
                </tr>
                <tr>
                    <td style="padding: 28px;">
                        <h2 style="color: #0f172a; margin-top: 0; font-size: 18px;">Hello {employeeName},</h2>
                        <p style="color: #475569; font-size: 15px; line-height: 1.6;">This is a reminder that your SpaceBook room booking will start in <strong>15 minutes</strong>.</p>
                        <table width="100%" cellpadding="10" cellspacing="0" style="background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; margin: 20px 0;">
                            <tr><td width="35%" style="color: #64748b; font-weight: 600;">Meeting:</td><td style="color: #0f172a; font-weight: 600;">{meetingTitle}</td></tr>
                            {(!string.IsNullOrWhiteSpace(purpose) ? $"<tr><td style=\"color: #64748b; font-weight: 600;\">Purpose:</td><td style=\"color: #0f172a;\">{purpose}</td></tr>" : "")}
                            <tr><td style="color: #64748b; font-weight: 600;">Room:</td><td style="color: #0f172a;">{roomName}</td></tr>
                            <tr><td style="color: #64748b; font-weight: 600;">Date:</td><td style="color: #0f172a;">{bookingDate:MMMM dd, yyyy}</td></tr>
                            <tr><td style="color: #64748b; font-weight: 600;">Start Time:</td><td style="color: #2563eb; font-weight: 600;">{startTime:hh\\:mm tt}</td></tr>
                            <tr><td style="color: #64748b; font-weight: 600;">End Time:</td><td style="color: #2563eb; font-weight: 600;">{endTime:hh\\:mm tt}</td></tr>
                            <tr><td style="color: #64748b; font-weight: 600;">Participants:</td><td style="color: #0f172a;">{participantCount}</td></tr>
                        </table>
                        <p style="color: #475569; font-size: 15px; margin: 16px 0 24px;">Please be ready for your booking.</p>
                        <p style="color: #64748b; font-size: 14px; margin: 0;">Regards,<br><strong>SpaceBook</strong></p>
                    </td>
                </tr>
                <tr>
                    <td style="text-align: center; padding: 16px; background: #f1f5f9; color: #64748b; font-size: 12px;">
                        This is an automated notification from SpaceBook.
                    </td>
                </tr>
            </table>
        </body>
        </html>
        """;
    }

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
        return $"""
        <!DOCTYPE html>
        <html>
        <head><meta charset="utf-8"><title>15-Minute Start Reminder Alert</title></head>
        <body style="font-family: Arial, sans-serif; background-color: #f4f6f9; padding: 24px; color: #1e293b; margin: 0;">
            <div style="max-width: 580px; margin: 0 auto; background: #ffffff; border-radius: 10px; padding: 24px; border-left: 4px solid #2563eb; box-shadow: 0 2px 8px rgba(0,0,0,0.05);">
                <h2 style="color: #1e40af; margin-top: 0;">[Admin Alert] Booking Starts in 15 Minutes</h2>
                <p style="color: #475569;">The following scheduled room booking will start shortly:</p>
                <table width="100%" cellpadding="6" cellspacing="0" style="background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 6px; margin: 16px 0;">
                    <tr><td width="35%"><strong>Booked By:</strong></td><td>{employeeName} ({employeeEmail})</td></tr>
                    {(!string.IsNullOrWhiteSpace(department) ? $"<tr><td><strong>Department:</strong></td><td>{department}</td></tr>" : "")}
                    <tr><td><strong>Meeting:</strong></td><td>{meetingTitle}</td></tr>
                    {(!string.IsNullOrWhiteSpace(purpose) ? $"<tr><td><strong>Purpose:</strong></td><td>{purpose}</td></tr>" : "")}
                    <tr><td><strong>Room:</strong></td><td>{roomName}</td></tr>
                    <tr><td><strong>Date:</strong></td><td>{bookingDate:MMMM dd, yyyy}</td></tr>
                    <tr><td><strong>Start Time:</strong></td><td>{startTime:hh\\:mm tt}</td></tr>
                    <tr><td><strong>End Time:</strong></td><td>{endTime:hh\\:mm tt}</td></tr>
                    <tr><td><strong>Participants:</strong></td><td>{participantCount}</td></tr>
                </table>
                <p style="color: #64748b; font-size: 13px; margin: 0;">Regards,<br><strong>SpaceBook</strong></p>
            </div>
        </body>
        </html>
        """;
    }

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
        <head><meta charset="utf-8"><title>SpaceBook Reminder - Booking Ends in 15 Minutes</title></head>
        <body style="font-family: Arial, sans-serif; background-color: #f4f6f9; padding: 24px; color: #1e293b; margin: 0;">
            <table align="center" width="100%" cellpadding="0" cellspacing="0" style="max-width: 580px; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.06);">
                <tr>
                    <td style="background: linear-gradient(135deg, #d97706 0%, #b45309 100%); padding: 28px; text-align: center; color: #ffffff;">
                        <h1 style="margin: 0; font-size: 24px; font-weight: 700;">SpaceBook</h1>
                        <p style="margin: 6px 0 0; font-size: 14px; color: #fef3c7;">Wrap-up Reminder</p>
                    </td>
                </tr>
                <tr>
                    <td style="padding: 28px;">
                        <h2 style="color: #0f172a; margin-top: 0; font-size: 18px;">Hello {employeeName},</h2>
                        <p style="color: #475569; font-size: 15px; line-height: 1.6;">Your SpaceBook room booking will end in <strong>15 minutes</strong>.</p>
                        <table width="100%" cellpadding="10" cellspacing="0" style="background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; margin: 20px 0;">
                            <tr><td width="35%" style="color: #64748b; font-weight: 600;">Meeting:</td><td style="color: #0f172a; font-weight: 600;">{meetingTitle}</td></tr>
                            <tr><td style="color: #64748b; font-weight: 600;">Room:</td><td style="color: #0f172a;">{roomName}</td></tr>
                            <tr><td style="color: #64748b; font-weight: 600;">Date:</td><td style="color: #0f172a;">{bookingDate:MMMM dd, yyyy}</td></tr>
                            <tr><td style="color: #64748b; font-weight: 600;">Start Time:</td><td style="color: #0f172a;">{startTime:hh\\:mm tt}</td></tr>
                            <tr><td style="color: #64748b; font-weight: 600;">End Time:</td><td style="color: #d97706; font-weight: 700;">{endTime:hh\\:mm tt}</td></tr>
                        </table>
                        <p style="color: #475569; font-size: 15px; margin: 16px 0 24px;">Please complete your meeting and vacate the room on time.</p>
                        <p style="color: #64748b; font-size: 14px; margin: 0;">Regards,<br><strong>SpaceBook</strong></p>
                    </td>
                </tr>
                <tr>
                    <td style="text-align: center; padding: 16px; background: #f1f5f9; color: #64748b; font-size: 12px;">
                        This is an automated notification from SpaceBook.
                    </td>
                </tr>
            </table>
        </body>
        </html>
        """;
    }

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
        <head><meta charset="utf-8"><title>15-Minute End Reminder Alert</title></head>
        <body style="font-family: Arial, sans-serif; background-color: #f4f6f9; padding: 24px; color: #1e293b; margin: 0;">
            <div style="max-width: 580px; margin: 0 auto; background: #ffffff; border-radius: 10px; padding: 24px; border-left: 4px solid #d97706; box-shadow: 0 2px 8px rgba(0,0,0,0.05);">
                <h2 style="color: #92400e; margin-top: 0;">[Admin Alert] Booking Ends in 15 Minutes</h2>
                <p style="color: #475569;">The following scheduled room booking will conclude in 15 minutes:</p>
                <table width="100%" cellpadding="6" cellspacing="0" style="background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 6px; margin: 16px 0;">
                    <tr><td width="35%"><strong>Booked By:</strong></td><td>{employeeName} ({employeeEmail})</td></tr>
                    <tr><td><strong>Meeting:</strong></td><td>{meetingTitle}</td></tr>
                    <tr><td><strong>Room:</strong></td><td>{roomName}</td></tr>
                    <tr><td><strong>Date:</strong></td><td>{bookingDate:MMMM dd, yyyy}</td></tr>
                    <tr><td><strong>Start Time:</strong></td><td>{startTime:hh\\:mm tt}</td></tr>
                    <tr><td><strong>End Time:</strong></td><td>{endTime:hh\\:mm tt}</td></tr>
                </table>
                <p style="color: #64748b; font-size: 13px; margin: 0;">Regards,<br><strong>SpaceBook</strong></p>
            </div>
        </body>
        </html>
        """;
    }
}
