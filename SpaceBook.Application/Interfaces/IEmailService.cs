using SpaceBook.Domain.Entities;

namespace SpaceBook.Application.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true);

    Task SendEmailsAsync(IEnumerable<string> toEmails, string subject, string body, bool isHtml = true);

    Task SendBookingConfirmationAsync(
        Booking booking,
        Employee employee,
        Room room,
        IEnumerable<string> adminEmails);

    Task SendBookingStartReminderAsync(
        Booking booking,
        Employee employee,
        Room room);

    Task SendBookingEndReminderAsync(
        Booking booking,
        Employee employee,
        Room room);

    Task SendHotseatBookingConfirmationAsync(
        HotseatBooking booking,
        Employee employee,
        Seat seat,
        IEnumerable<string>? adminEmails = null);

    Task SendHotseatBookingRescheduledAsync(
        HotseatBooking booking,
        Employee employee,
        Seat seat,
        IEnumerable<string>? adminEmails = null);

    Task SendHotseatCheckInReminderAsync(
        HotseatBooking booking,
        Employee employee,
        Seat seat);

    Task SendHotseatBookingExpiredAsync(
        HotseatBooking booking,
        Employee employee,
        Seat seat);

    Task SendHotseatBookingCancelledAsync(
        HotseatBooking booking,
        Employee employee,
        Seat seat,
        IEnumerable<string>? adminEmails = null,
        string? cancellationReason = null);
}
