using SpaceBook.Application.DTOs.Admin;
using SpaceBook.Application.Interfaces;
using SpaceBook.Domain.Entities;
using SpaceBook.Application.DTOs.Employee;

namespace SpaceBook.Application.Interfaces;

public interface INotificationService
{
    Task<List<NotificationDto>> GetNotificationsForUserAsync(int employeeId);
    Task MarkAllAsReadAsync(int employeeId);
    Task CreateNotificationAsync(int employeeId, int? bookingId, string message);
}