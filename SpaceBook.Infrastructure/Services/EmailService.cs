using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
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

        // =====================================================
        // 1. TRY RESEND HTTPS API (PORT 443 - NEVER BLOCKED)
        // =====================================================
        var resendApiKey = _configuration["Resend:ApiKey"] 
                           ?? _configuration["Resend__ApiKey"] 
                           ?? _configuration["RESEND_API_KEY"];

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
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", resendApiKey.Trim());

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
        var host = _configuration["Smtp:Host"];
        var portValue = _configuration["Smtp:Port"];
        var username = _configuration["Smtp:Username"];
        var password = _configuration["Smtp:Password"];
        var fromEmail = _configuration["Smtp:From"];
        var senderName =
            _configuration["Smtp:SenderName"] ?? "SpaceBook";

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning(
                "SMTP is not configured (or Host/Username/Password is missing). Skipping SMTP delivery.");

            return;
        }

        if (string.IsNullOrWhiteSpace(fromEmail))
        {
            fromEmail = username;
        }

        var port = 465;

        if (!string.IsNullOrWhiteSpace(portValue) &&
            int.TryParse(portValue, out var configuredPort))
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
            client.Timeout = 10000;

            var secureSocketOption =
                port == 465
                    ? SecureSocketOptions.SslOnConnect
                    : SecureSocketOptions.StartTls;

            await client.ConnectAsync(
                host,
                port,
                secureSocketOption);

            _logger.LogInformation(
                "Connected successfully to SMTP server on port {Port}.", port);

            await client.AuthenticateAsync(
                username.Trim(),
                cleanPassword);

            _logger.LogInformation(
                "SMTP authentication successful.");

            await client.SendAsync(message);

            _logger.LogInformation(
                "Email successfully sent to {To}. Subject={Subject}",
                toEmail,
                subject);

            await client.DisconnectAsync(true);
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
