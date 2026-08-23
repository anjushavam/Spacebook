using SpaceBook.Domain.Entities;

namespace SpaceBook.Application.Interfaces;

public interface IBookingReminderRepository
{
    Task<List<Booking>> GetTodayBookingsNeedingRemindersAsync(
        DateOnly date,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}
