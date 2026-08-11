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

    // =========================================================
    // Employee Notifications
    // =========================================================

    public async Task<List<NotificationDto>> GetEmployeeNotificationsAsync(
        int employeeId)
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

    // =========================================================
    // Admin Notifications
    // =========================================================

    public async Task<List<NotificationDto>> GetAdminNotificationsAsync()
    {
        var list = await _context.Notifications
            .AsNoTracking()
            .Where(n =>
                n.Message.ToLower().Contains("request") ||
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

    // =========================================================
    // Generic User Notifications
    // =========================================================

    public async Task<List<NotificationDto>> GetNotificationsForUserAsync(
        int employeeId)
    {
        return await GetEmployeeNotificationsAsync(employeeId);
    }

    // =========================================================
    // Get All Notifications
    // =========================================================

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

    // =========================================================
    // Mark Employee Notifications As Read
    // =========================================================

    public async Task MarkAllAsReadAsync(int employeeId)
    {
        var unreadNotifications = await _context.Notifications
            .Where(n =>
                n.EmployeeId == employeeId &&
                !n.IsRead)
            .ToListAsync();

        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
        }

        await _context.SaveChangesAsync();
    }

    // =========================================================
    // Add Notification
    // =========================================================

    public async Task AddAsync(Notification notification)
    {
        await _context.Notifications.AddAsync(notification);
    }

    // =========================================================
    // Save Changes
    // =========================================================

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    // =========================================================
    // Derive Notification Title
    // =========================================================

    private static string DeriveTitle(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return "Notification";
        }

        // Approval
        if (message.Contains(
                "approve",
                StringComparison.OrdinalIgnoreCase) ||
            message.Contains(
                "confirm",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Booking Approved";
        }

        // Rejection
        if (message.Contains(
                "reject",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Booking Rejected";
        }

        // Cancellation
        if (message.Contains(
                "cancel",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Booking Cancelled";
        }

        // Missed check-in
        if (message.Contains(
                "missed",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Missed Check-in";
        }

        return "Notification";
    }

    // =========================================================
    // Time Ago
    // =========================================================

    private static string FormatTimeAgo(DateTime created)
    {
        var utcCreated = created.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(
                created,
                DateTimeKind.Utc)
            : created.ToUniversalTime();

        var span = DateTime.UtcNow - utcCreated;

        if (span.TotalSeconds < 60)
        {
            return "Just now";
        }

        if (span.TotalMinutes < 60)
        {
            return $"{(int)span.TotalMinutes}m ago";
        }

        if (span.TotalHours < 24)
        {
            return $"{(int)span.TotalHours}h ago";
        }

        if (span.TotalDays < 7)
        {
            return $"{(int)span.TotalDays}d ago";
        }

        return utcCreated.ToString("MMM dd, yyyy");
    }
}