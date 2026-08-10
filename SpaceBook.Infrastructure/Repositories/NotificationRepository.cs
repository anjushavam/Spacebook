using Microsoft.EntityFrameworkCore;
using SpaceBook.Application.DTOs.Admin;
using SpaceBook.Application.DTOs.Employee;
using SpaceBook.Application.Interfaces;
using SpaceBook.Domain.Entities;
using SpaceBook.Infrastructure.Data;

namespace SpaceBook.Infrastructure.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly ApplicationDbContext _context;

    public NotificationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    // Employees see only their own notifications linked by EmployeeId
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

    // Admins see action items (requests, submissions)
    public async Task<List<NotificationDto>> GetAdminNotificationsAsync()
    {
        var list = await _context.Notifications
            .AsNoTracking()
            .Where(n => n.Message.ToLower().Contains("request") || 
                        n.Message.ToLower().Contains("submitted") ||
                        n.Message.ToLower().Contains("pending"))
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .ToListAsync();

        return list.Select(n => new NotificationDto
        {
            NotificationId = n.NotificationId,
            Title = "Booking Request",
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

    public async Task AddAsync(Notification notification)
    {
        await _context.Notifications.AddAsync(notification);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    private static string DeriveTitle(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return "Notification";
        if (message.Contains("approve", StringComparison.OrdinalIgnoreCase) || message.Contains("confirm", StringComparison.OrdinalIgnoreCase)) return "Booking Approved";
        if (message.Contains("reject", StringComparison.OrdinalIgnoreCase) || message.Contains("cancel", StringComparison.OrdinalIgnoreCase)) return "Booking Cancelled";
        if (message.Contains("missed", StringComparison.OrdinalIgnoreCase)) return "Missed Check-in";
        return "Notification";
    }

    private static string FormatTimeAgo(DateTime created)
    {
        var span = DateTime.UtcNow - created;
        if (span.TotalMinutes < 60) return $"{Math.Max(1, (int)span.TotalMinutes)}M\nAGO";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours}H\nAGO";
        return $"{(int)span.TotalDays}D\nAGO";
    }
}