using SpaceBook.Application.DTOs.Admin; // Adjust if NotificationDto is under SpaceBook.Application.DTOs.Employee
using SpaceBook.Application.Interfaces;
using SpaceBook.Domain.Entities;
using SpaceBook.Application.DTOs.Employee;

namespace SpaceBook.Application.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;

    public NotificationService(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task<List<NotificationDto>> GetNotificationsForUserAsync(int employeeId)
    {
        return await _notificationRepository.GetNotificationsForUserAsync(employeeId);
    }

    public async Task MarkAllAsReadAsync(int employeeId)
    {
        await _notificationRepository.MarkAllAsReadAsync(employeeId);
    }

    public async Task CreateNotificationAsync(int employeeId, int? bookingId, string message)
    {
        var notification = new Notification
        {
            EmployeeId = employeeId, // FIXED: Changed EmployeeId to UserId to match DB Entity
            Message = message,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        await _notificationRepository.AddAsync(notification);
        await _notificationRepository.SaveChangesAsync();
    }
}