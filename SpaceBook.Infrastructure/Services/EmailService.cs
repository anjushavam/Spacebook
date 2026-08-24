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
            _logger.LogWarning("Email recipient is null or empty. Skipping email dispatch.");
            return;
        }

        var host = _configuration["Smtp:Host"];
        var portStr = _configuration["Smtp:Port"];
        var username = _configuration["Smtp:Username"];
        var password = _configuration["Smtp:Password"];
        var fromEmail = _configuration["Smtp:From"] ?? "no-reply@spacebook.com";
        var fromName = _configuration["Smtp:SenderName"] ?? "SpaceBook";

        // If SMTP is not configured, simulate/log sending so developers and non-SMTP environments work out of the box
        if (string.IsNullOrWhiteSpace(host))
        {
            _logger.LogInformation(
                "[Email Simulated] To: {To}, Subject: '{Subject}'. (Configure 'Smtp:Host' in appsettings.json for live SMTP delivery)",
                toEmail, subject);
            return;
        }

        int port = 587;
        if (!string.IsNullOrWhiteSpace(portStr) && int.TryParse(portStr, out var parsedPort))
        {
            port = parsedPort;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromEmail));
            message.To.Add(new MailboxAddress("", toEmail));
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
            client.Timeout = 10000; // 10 second timeout

            // Accept all SSL certificates if needed for cloud containers
            client.ServerCertificateValidationCallback = (s, c, h, e) => true;

            var secureOption = port == 465 
                ? SecureSocketOptions.SslOnConnect 
                : SecureSocketOptions.StartTlsWhenAvailable;

            await client.ConnectAsync(host, port, secureOption);

            if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
            {
                // Remove any accidental spaces in app password
                var cleanPassword = password.Replace(" ", "").Trim();
                await client.AuthenticateAsync(username.Trim(), cleanPassword);
            }

            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Successfully sent email to {To} with Subject: '{Subject}'", toEmail, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To} via SMTP host {Host}:{Port}", toEmail, host, port);
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

        var host = _configuration["Smtp:Host"];
        var portStr = _configuration["Smtp:Port"];
        var username = _configuration["Smtp:Username"];
        var password = _configuration["Smtp:Password"];
        var fromEmail = _configuration["Smtp:From"] ?? "no-reply@spacebook.com";
        var fromName = _configuration["Smtp:SenderName"] ?? "SpaceBook";

        if (string.IsNullOrWhiteSpace(host))
        {
            _logger.LogInformation(
                "[Email Simulated] Batch To {Count} recipients ({Recipients}), Subject: '{Subject}'.",
                validEmails.Count, string.Join(", ", validEmails), subject);
            return;
        }

        int port = 587;
        if (!string.IsNullOrWhiteSpace(portStr) && int.TryParse(portStr, out var parsedPort))
        {
            port = parsedPort;
        }

        try
        {
            using var client = new SmtpClient();
            client.Timeout = 10000; // 10 second timeout
            client.ServerCertificateValidationCallback = (s, c, h, e) => true;

            var secureOption = port == 465 
                ? SecureSocketOptions.SslOnConnect 
                : SecureSocketOptions.StartTlsWhenAvailable;

            await client.ConnectAsync(host, port, secureOption);

            if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
            {
                var cleanPassword = password.Replace(" ", "").Trim();
                await client.AuthenticateAsync(username.Trim(), cleanPassword);
            }

            var bodyBuilder = new BodyBuilder();
            if (isHtml)
            {
                bodyBuilder.HtmlBody = body;
            }
            else
            {
                bodyBuilder.TextBody = body;
            }
            var messageBody = bodyBuilder.ToMessageBody();

            foreach (var email in validEmails)
            {
                try
                {
                    var message = new MimeMessage();
                    message.From.Add(new MailboxAddress(fromName, fromEmail));
                    message.To.Add(new MailboxAddress("", email));
                    message.Subject = subject;
                    message.Body = messageBody;

                    await client.SendAsync(message);
                    _logger.LogInformation("Successfully sent batch email to {To} with Subject: '{Subject}'", email, subject);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send batch email to {To}", email);
                }
            }

            await client.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect or send batch emails via SMTP host {Host}:{Port}", host, port);
            throw;
        }
    }
}

