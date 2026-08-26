using SpaceBook.Application.DTOs.Employee;

namespace SpaceBook.Application.Interfaces;

public interface INotificationService
{
    Task<List<NotificationDto>> GetNotificationsForUserAsync(
        int employeeId);

    Task MarkAllAsReadAsync(
        int employeeId);

    Task ClearNotificationsForUserAsync(
        int employeeId);

    Task DeleteNotificationAsync(
        int notificationId,
        int employeeId);

    Task ClearAdminNotificationsAsync();

    Task DeleteAdminNotificationAsync(
        int notificationId);

    Task CreateNotificationAsync(
        int employeeId,
        int? bookingId,
        string message);
}