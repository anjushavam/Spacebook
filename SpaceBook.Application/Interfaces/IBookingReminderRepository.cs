using SpaceBook.Domain.Entities;

namespace SpaceBook.Application.Interfaces;

public interface IBookingReminderRepository
{
    Task<List<Booking>> GetTodayBookingsNeedingRemindersAsync(
        DateOnly date,
        CancellationToken cancellationToken = default);

    Task<bool> HasNotificationBeenSentAsync(
        int bookingId,
        string notificationType,
        CancellationToken cancellationToken = default);

    Task RecordNotificationSentAsync(
        int bookingId,
        string notificationType,
        string status = "Sent",
        CancellationToken cancellationToken = default);

    Task ResetRemindersForBookingAsync(
        int bookingId,
        CancellationToken cancellationToken = default);

    Task<List<string>> GetAdminEmailsAsync(
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}
