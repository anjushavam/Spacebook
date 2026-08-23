namespace SpaceBook.Application.Interfaces;

public interface IBookingReminderService
{
    Task ProcessBookingRemindersAsync(CancellationToken cancellationToken = default);
}
