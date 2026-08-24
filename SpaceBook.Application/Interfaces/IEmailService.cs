namespace SpaceBook.Application.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true);
    Task SendEmailsAsync(IEnumerable<string> toEmails, string subject, string body, bool isHtml = true);
}
