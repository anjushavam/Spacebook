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

    public NotificationRepository(
        ApplicationDbContext context)
    {
        _context = context;
    }

    // =========================================================
    // EMPLOYEE NOTIFICATIONS
    // =========================================================

    public async Task<List<NotificationDto>>
        GetEmployeeNotificationsAsync(
            int employeeId)
    {
        var list = await _context.Notifications
            .AsNoTracking()

            .Include(n => n.Employee)

            .Include(n => n.Booking)
                .ThenInclude(b => b!.Room)

            .Include(n => n.Booking)
                .ThenInclude(b => b!.Employee)

            .Where(n =>
                n.EmployeeId == employeeId)

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

            CreatedAt = n.CreatedAt,

            TimeAgo = FormatTimeAgo(n.CreatedAt),

            EmployeeName =
                n.Employee?.Name
                ?? n.Booking?.Employee?.Name,

            RoomName =
                n.Booking?.Room?.RoomName,

            BookingDate =
                n.Booking?.BookingDate,

            StartTime =
                n.Booking?.StartTime,

            EndTime =
                n.Booking?.EndTime
        })
        .ToList();
    }

    // =========================================================
    // ADMIN NOTIFICATIONS
    // =========================================================

    public async Task<List<NotificationDto>>
        GetAdminNotificationsAsync()
    {
        var list = await _context.Notifications
            .AsNoTracking()

            // Employee who performed the action
            .Include(n => n.Employee)

            // Booking and Room
            .Include(n => n.Booking)
                .ThenInclude(b => b!.Room)

            // Employee associated with booking
            .Include(n => n.Booking)
                .ThenInclude(b => b!.Employee)

            // -------------------------------------------------
            // ADMIN RELEVANT NOTIFICATIONS
            // -------------------------------------------------

            .Where(n =>
                n.Message.ToLower().Contains("request") ||
                n.Message.ToLower().Contains("submitted") ||
                n.Message.ToLower().Contains("pending") ||
                n.Message.ToLower().Contains("rescheduled") ||
                n.Message.ToLower().Contains("requires approval") ||
                n.Message.ToLower().Contains("cancelled"))

            .OrderByDescending(n => n.CreatedAt)

            .Take(50)

            .ToListAsync();

        return list.Select(n =>
        {
            var booking = n.Booking;

            // -------------------------------------------------
            // EMPLOYEE NAME
            // -------------------------------------------------

            var employeeName =
                n.Employee?.Name
                ?? booking?.Employee?.Name
                ?? "Employee";

            // -------------------------------------------------
            // ROOM NAME
            // -------------------------------------------------

            var roomName =
                booking?.Room?.RoomName
                ?? "Meeting Room";

            // -------------------------------------------------
            // NOTIFICATION TYPE
            // -------------------------------------------------

            var isRescheduled =
                n.Message.Contains(
                    "rescheduled",
                    StringComparison.OrdinalIgnoreCase);

            var isCancelled =
                n.Message.Contains(
                    "cancelled",
                    StringComparison.OrdinalIgnoreCase);

            // -------------------------------------------------
            // MESSAGE
            // -------------------------------------------------

            string message;

            if (isRescheduled)
            {
                message =
                    $"{employeeName} rescheduled a booking for " +
                    $"{roomName} and it requires approval.";
            }
            else if (isCancelled)
            {
                message =
                    $"{employeeName} cancelled a booking for " +
                    $"{roomName}.";
            }
            else if (booking != null)
            {
                message =
                    $"{employeeName} submitted a booking request " +
                    $"for {roomName}.";
            }
            else
            {
                message = n.Message;
            }

            // -------------------------------------------------
            // TITLE
            // -------------------------------------------------

            string title;

            if (isRescheduled)
            {
                title = "Booking Rescheduled";
            }
            else if (isCancelled)
            {
                title = "Booking Cancelled";
            }
            else
            {
                title = "Booking Request";
            }

            // -------------------------------------------------
            // RETURN DTO
            // -------------------------------------------------

            return new NotificationDto
            {
                NotificationId =
                    n.NotificationId,

                Title =
                    title,

                Message =
                    message,

                IsRead =
                    n.IsRead,

                CreatedOn =
                    n.CreatedAt,

                CreatedAt =
                    n.CreatedAt,

                TimeAgo =
                    FormatTimeAgo(n.CreatedAt),

                EmployeeName =
                    employeeName,

                RoomName =
                    booking?.Room?.RoomName,

                BookingDate =
                    booking?.BookingDate,

                StartTime =
                    booking?.StartTime,

                EndTime =
                    booking?.EndTime
            };
        })
        .ToList();
    }

    // =========================================================
    // GENERIC USER NOTIFICATIONS
    // =========================================================

    public async Task<List<NotificationDto>>
        GetNotificationsForUserAsync(
            int employeeId)
    {
        return await GetEmployeeNotificationsAsync(
            employeeId);
    }

    // =========================================================
    // GET ALL NOTIFICATIONS
    // =========================================================

    public async Task<List<NotificationDto>>
        GetAllAsync()
    {
        var list = await _context.Notifications
            .AsNoTracking()

            .Include(n => n.Employee)

            .Include(n => n.Booking)
                .ThenInclude(b => b!.Room)

            .Include(n => n.Booking)
                .ThenInclude(b => b!.Employee)

            .OrderByDescending(n => n.CreatedAt)

            .Take(50)

            .ToListAsync();

        return list.Select(n => new NotificationDto
        {
            NotificationId =
                n.NotificationId,

            Title =
                DeriveTitle(n.Message),

            Message =
                n.Message,

            IsRead =
                n.IsRead,

            CreatedOn =
                n.CreatedAt,

            CreatedAt =
                n.CreatedAt,

            TimeAgo =
                FormatTimeAgo(n.CreatedAt),

            EmployeeName =
                n.Employee?.Name
                ?? n.Booking?.Employee?.Name,

            RoomName =
                n.Booking?.Room?.RoomName,

            BookingDate =
                n.Booking?.BookingDate,

            StartTime =
                n.Booking?.StartTime,

            EndTime =
                n.Booking?.EndTime
        })
        .ToList();
    }

    // =========================================================
    // MARK NOTIFICATIONS AS READ
    // =========================================================

    public async Task MarkAllAsReadAsync(
        int employeeId)
    {
        var unreadNotifications =
            employeeId == 0

            // Admin: mark all notifications as read
            ? await _context.Notifications
                .Where(n => !n.IsRead)
                .ToListAsync()

            // Employee: mark only their notifications as read
            : await _context.Notifications
                .Where(n =>
                    n.EmployeeId == employeeId &&
                    !n.IsRead)
                .ToListAsync();

        foreach (var notification
                 in unreadNotifications)
        {
            notification.IsRead = true;
        }

        await _context.SaveChangesAsync();
    }

    // =========================================================
    // ADD NOTIFICATION
    // =========================================================

    public async Task AddAsync(
        Notification notification)
    {
        await _context.Notifications.AddAsync(
            notification);
    }

    // =========================================================
    // SAVE CHANGES
    // =========================================================

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    // =========================================================
    // NOTIFICATION TITLE
    // =========================================================

    private static string DeriveTitle(
        string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return "Notification";
        }

        if (message.Contains(
                "rescheduled",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Booking Rescheduled";
        }

        if (message.Contains(
                "approve",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Booking Approved";
        }

        if (message.Contains(
                "reject",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Booking Rejected";
        }

        if (message.Contains(
                "cancel",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Booking Cancelled";
        }

        if (message.Contains(
                "missed",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Missed Check-in";
        }

        if (
            message.Contains(
                "request",
                StringComparison.OrdinalIgnoreCase)
            ||
            message.Contains(
                "submitted",
                StringComparison.OrdinalIgnoreCase)
            ||
            message.Contains(
                "pending",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Booking Request";
        }

        return "Notification";
    }

    // =========================================================
    // TIME AGO
    // =========================================================

    private static string FormatTimeAgo(
        DateTime created)
    {
        var utcCreated =
            created.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(
                    created,
                    DateTimeKind.Utc)
                : created.ToUniversalTime();

        var span =
            DateTime.UtcNow - utcCreated;

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

        return utcCreated.ToString(
            "MMM dd, yyyy");
    }
}