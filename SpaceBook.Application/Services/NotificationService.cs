using SpaceBook.Application.DTOs.Employee;
using SpaceBook.Application.Interfaces;
using SpaceBook.Domain.Entities;

namespace SpaceBook.Application.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;

    public NotificationService(
        INotificationRepository notificationRepository)
    {
        _notificationRepository =
            notificationRepository;
    }

    // =========================================================
    // GET USER NOTIFICATIONS
    // =========================================================

    public async Task<List<NotificationDto>>
        GetNotificationsForUserAsync(
            int employeeId)
    {
        return await _notificationRepository
            .GetNotificationsForUserAsync(
                employeeId);
    }

    // =========================================================
    // MARK ALL AS READ
    // =========================================================

    public async Task MarkAllAsReadAsync(
        int employeeId)
    {
        await _notificationRepository
            .MarkAllAsReadAsync(
                employeeId);
    }

    // =========================================================
    // CLEAR ALL NOTIFICATIONS FOR USER
    // =========================================================

    public async Task ClearNotificationsForUserAsync(
        int employeeId)
    {
        await _notificationRepository
            .ClearEmployeeNotificationsAsync(
                employeeId);
    }

    // =========================================================
    // DELETE SINGLE NOTIFICATION FOR USER
    // =========================================================

    public async Task DeleteNotificationAsync(
        int notificationId,
        int employeeId)
    {
        await _notificationRepository
            .DeleteEmployeeNotificationAsync(
                notificationId,
                employeeId);
    }

    // =========================================================
    // CLEAR ALL ADMIN NOTIFICATIONS
    // =========================================================

    public async Task ClearAdminNotificationsAsync()
    {
        await _notificationRepository
            .ClearAdminNotificationsAsync();
    }

    // =========================================================
    // DELETE SINGLE ADMIN NOTIFICATION
    // =========================================================

    public async Task DeleteAdminNotificationAsync(
        int notificationId)
    {
        await _notificationRepository
            .DeleteAdminNotificationAsync(
                notificationId);
    }

    // =========================================================
    // CREATE NOTIFICATION
    // =========================================================

    public async Task CreateNotificationAsync(
        int employeeId,
        int? bookingId,
        string message)
    {
        if (employeeId <= 0)
        {
            throw new ArgumentException(
                "Invalid employee ID.");
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException(
                "Notification message is required.");
        }

        var notification =
            new Notification
            {
                EmployeeId =
                    employeeId,

                // IMPORTANT:
                // Preserve booking relationship.
                BookingId =
                    bookingId,

                Message =
                    message.Length > 500
                        ? message[..500]
                        : message,

                IsRead =
                    false,

                CreatedAt =
                    DateTime.UtcNow
            };

        await _notificationRepository
            .AddAsync(notification);

        await _notificationRepository
            .SaveChangesAsync();
    }
}