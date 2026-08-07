using Microsoft.EntityFrameworkCore;
using SpaceBook.Application.DTOs.Admin;
using SpaceBook.Application.Interfaces;
using SpaceBook.Domain.Entities;
using SpaceBook.Infrastructure.Data;
using SpaceBook.Application.DTOs.Employee;

namespace SpaceBook.Infrastructure.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly ApplicationDbContext _context;

    public NotificationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    // Get notifications for a specific employee
    public async Task<List<NotificationDto>> GetEmployeeNotificationsAsync(int employeeId)
    {
        var list = await _context.Notifications
            .AsNoTracking()
            .Where(n => n.EmployeeId == employeeId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(15)
            .ToListAsync();

        return list.Select(n => new NotificationDto
        {
            NotificationId = n.NotificationId,
            Title = DeriveTitle(n.Message),
            Message = n.Message,
            IsRead = n.IsRead,
            CreatedOn = n.CreatedAt,
            TimeAgo = FormatTimeAgo(n.CreatedAt)
        }).ToList();
    }

    public async Task<List<NotificationDto>> GetNotificationsForUserAsync(int employeeId)
    {
        return await GetEmployeeNotificationsAsync(employeeId);
    }

    // Get all notifications (Admin)
    public async Task<List<NotificationDto>> GetAllAsync()
    {
        var list = await _context.Notifications
            .AsNoTracking()
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .ToListAsync();

        return list.Select(n => new NotificationDto
        {
            NotificationId = n.NotificationId,
            Title = DeriveTitle(n.Message),
            Message = n.Message,
            IsRead = n.IsRead,
            CreatedOn = n.CreatedAt,
            TimeAgo = FormatTimeAgo(n.CreatedAt)
        }).ToList();
    }

    // Mark all unread notifications as read for an employee
    public async Task MarkAllAsReadAsync(int employeeId)
    {
        var unread = await _context.Notifications
            .Where(n => n.EmployeeId == employeeId && !n.IsRead)
            .ToListAsync();

        foreach (var item in unread)
        {
            item.IsRead = true;
        }

        await _context.SaveChangesAsync();
    }

    // Add a new notification entity
    public async Task AddAsync(Notification notification)
    {
        await _context.Notifications.AddAsync(notification);
    }

    // Save DbContext changes
    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    // Helper: Infer title from message context
    private static string DeriveTitle(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return "Notification";
        if (message.Contains("confirm", StringComparison.OrdinalIgnoreCase) || message.Contains("reserv", StringComparison.OrdinalIgnoreCase)) return "Booking confirmed";
        if (message.Contains("remind", StringComparison.OrdinalIgnoreCase) || message.Contains("start", StringComparison.OrdinalIgnoreCase)) return "Reminder";
        return "Policy update";
    }

    // Helper: Relative time string formatter
    private static string FormatTimeAgo(DateTime created)
    {
        var span = DateTime.UtcNow - created;
        if (span.TotalMinutes < 60) return $"{Math.Max(1, (int)span.TotalMinutes)}M\nAGO";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours}H\nAGO";
        return $"{(int)span.TotalDays}D\nAGO";
    }
}