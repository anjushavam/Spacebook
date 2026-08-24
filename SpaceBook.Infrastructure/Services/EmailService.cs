using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using SpaceBook.Application.Interfaces;

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

        var host = _configuration["Smtp:Host"];
        var portValue = _configuration["Smtp:Port"];
        var username = _configuration["Smtp:Username"];
        var password = _configuration["Smtp:Password"];
        var fromEmail = _configuration["Smtp:From"];
        var senderName =
            _configuration["Smtp:SenderName"] ?? "SpaceBook";

        // =====================================================
        // VALIDATE SMTP CONFIGURATION
        // =====================================================

        if (string.IsNullOrWhiteSpace(host))
        {
            _logger.LogError(
                "SMTP Host is not configured.");

            return;
        }

        if (string.IsNullOrWhiteSpace(username))
        {
            _logger.LogError(
                "SMTP Username is not configured.");

            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            _logger.LogError(
                "SMTP Password is not configured.");

            return;
        }

        if (string.IsNullOrWhiteSpace(fromEmail))
        {
            fromEmail = username;
        }

        var port = 587;

        if (!string.IsNullOrWhiteSpace(portValue) &&
            int.TryParse(portValue, out var configuredPort))
        {
            port = configuredPort;
        }

        try
        {
            _logger.LogInformation(
                "Attempting to send email. To={To}, Host={Host}, Port={Port}",
                toEmail,
                host,
                port);

            // Remove spaces from Gmail App Password
            var cleanPassword =
                password.Replace(" ", "").Trim();

            var message = new MimeMessage();

            message.From.Add(
                new MailboxAddress(
                    senderName,
                    fromEmail));

            message.To.Add(
                MailboxAddress.Parse(toEmail));

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

            using var client = new SmtpClient();
            client.Timeout = 10000; // 10 second timeout

            // =================================================
            // CONNECT TO SMTP (Try Port 465 SSL / 587 StartTLS)
            // =================================================

            try
            {
                var secureSocketOption =
                    port == 465
                        ? SecureSocketOptions.SslOnConnect
                        : SecureSocketOptions.StartTls;

                await client.ConnectAsync(
                    host,
                    port,
                    secureSocketOption);
            }
            catch (Exception ex) when (port == 587)
            {
                _logger.LogWarning(ex, "Port 587 timed out. Attempting fallback to Port 465 SSL.");

                await client.ConnectAsync(
                    host,
                    465,
                    SecureSocketOptions.SslOnConnect);
            }

            _logger.LogInformation(
                "Connected successfully to SMTP server.");

            // =================================================
            // AUTHENTICATE
            // =================================================

            await client.AuthenticateAsync(
                username.Trim(),
                cleanPassword);

            _logger.LogInformation(
                "SMTP authentication successful.");

            // =================================================
            // SEND
            // =================================================

            await client.SendAsync(message);

            _logger.LogInformation(
                "Email successfully sent to {To}. Subject={Subject}",
                toEmail,
                subject);

            // =================================================
            // DISCONNECT
            // =================================================

            await client.DisconnectAsync(true);
        }
        catch (AuthenticationException ex)
        {
            _logger.LogError(
                ex,
                "SMTP authentication failed. Check Gmail username and App Password.");

            throw;
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
}
