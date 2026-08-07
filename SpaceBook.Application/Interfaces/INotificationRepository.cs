using SpaceBook.Application.DTOs.Admin;
using SpaceBook.Domain.Entities;
using SpaceBook.Application.DTOs.Employee;

namespace SpaceBook.Application.Interfaces;

public interface INotificationRepository
{
    // Existing user-facing methods
    Task<List<NotificationDto>> GetNotificationsForUserAsync(int employeeId);
    Task<List<NotificationDto>> GetEmployeeNotificationsAsync(int employeeId);
    
    // Admin-facing method
    Task<List<NotificationDto>> GetAllAsync();
    
    // Read operations
    Task MarkAllAsReadAsync(int employeeId);
    
    // Persistence operations
    Task AddAsync(Notification notification);
    Task SaveChangesAsync();
}