using SpaceBook.Application.DTOs.Admin;
using SpaceBook.Application.DTOs.Employee;
using SpaceBook.Domain.Entities;

namespace SpaceBook.Application.Interfaces;

public interface INotificationRepository
{
    Task<List<NotificationDto>> GetNotificationsForUserAsync(
        int employeeId);

    Task<List<NotificationDto>> GetEmployeeNotificationsAsync(
        int employeeId);

    Task<List<NotificationDto>> GetAdminNotificationsAsync();

    Task<List<NotificationDto>> GetAllAsync();

    Task MarkAllAsReadAsync(
        int employeeId);

    Task AddAsync(
        Notification notification);

    Task SaveChangesAsync();
}