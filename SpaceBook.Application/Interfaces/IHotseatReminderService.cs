namespace SpaceBook.Application.Interfaces;

public interface IHotseatReminderService
{
    Task ProcessHotseatRemindersAndExpirationsAsync(CancellationToken cancellationToken = default);
}
