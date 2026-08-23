using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
        var enableSsl = bool.TryParse(_configuration["Smtp:EnableSsl"], out var ssl) ? ssl : true;

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
            using var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml
            };
            mailMessage.To.Add(toEmail);

            using var smtpClient = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl
            };

            if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
            {
                smtpClient.Credentials = new NetworkCredential(username, password);
            }

            await smtpClient.SendMailAsync(mailMessage);
            _logger.LogInformation("Successfully sent email to {To} with Subject: '{Subject}'", toEmail, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To} via SMTP host {Host}:{Port}", toEmail, host, port);
            throw;
        }
    }
}
