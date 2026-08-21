using SpaceBook.Application.DTOs.Employee;
using SpaceBook.Domain.Entities;

namespace SpaceBook.Application.Interfaces;

public interface INotificationRepository
{
    // =========================================================
    // GET EMPLOYEE NOTIFICATIONS
    // =========================================================

    Task<List<NotificationDto>> GetEmployeeNotificationsAsync(
        int employeeId);


    // =========================================================
    // GET ADMIN NOTIFICATIONS
    // =========================================================

    Task<List<NotificationDto>> GetAdminNotificationsAsync();


    // =========================================================
    // GET USER NOTIFICATIONS
    // =========================================================

    Task<List<NotificationDto>> GetNotificationsForUserAsync(
        int employeeId);


    // =========================================================
    // GET ALL NOTIFICATIONS
    // =========================================================

    Task<List<NotificationDto>> GetAllAsync();


    // =========================================================
    // MARK SINGLE NOTIFICATION AS READ
    // =========================================================

    Task MarkAsReadAsync(
        int notificationId,
        int employeeId);


    // =========================================================
    // MARK ALL NOTIFICATIONS AS READ
    // =========================================================

    Task MarkAllAsReadAsync(
        int employeeId);


    // =========================================================
    // ADD NOTIFICATION
    // =========================================================

    Task AddAsync(
        Notification notification);


    // =========================================================
    // SAVE CHANGES
    // =========================================================

    Task SaveChangesAsync();
}