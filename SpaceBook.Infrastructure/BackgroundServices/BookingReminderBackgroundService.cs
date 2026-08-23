using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SpaceBook.Application.Interfaces;

namespace SpaceBook.Infrastructure.BackgroundServices;

public class BookingReminderBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookingReminderBackgroundService> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(60);

    public BookingReminderBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<BookingReminderBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BookingReminderBackgroundService is starting.");

        // Initial brief delay before starting background loop
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var reminderService = scope.ServiceProvider.GetRequiredService<IBookingReminderService>();

                await reminderService.ProcessBookingRemindersAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Normal shutdown requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while executing BookingReminderBackgroundService.");
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation("BookingReminderBackgroundService is stopping.");
    }
}
