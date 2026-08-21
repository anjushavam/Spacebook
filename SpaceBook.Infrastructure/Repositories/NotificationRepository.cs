using Microsoft.EntityFrameworkCore;
using SpaceBook.Application.DTOs.Employee;
using SpaceBook.Application.Interfaces;
using SpaceBook.Domain.Entities;
using SpaceBook.Infrastructure.Data;

namespace SpaceBook.Infrastructure.Repositories;

public class NotificationRepository
    : INotificationRepository
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
        var list =
            await _context.Notifications
                .AsNoTracking()

                .Include(n => n.Employee)

                .Include(n => n.Booking)
                    .ThenInclude(b => b!.Room)

                .Include(n => n.Booking)
                    .ThenInclude(b => b!.Employee)

                .Where(n =>
                    n.EmployeeId == employeeId)

                .OrderByDescending(
                    n => n.CreatedAt)

                .Take(50)

                .ToListAsync();

        return list
            .Select(MapNotification)
            .ToList();
    }

    // =========================================================
    // ADMIN NOTIFICATIONS
    // =========================================================

    public async Task<List<NotificationDto>>
        GetAdminNotificationsAsync()
    {
        var list =
            await _context.Notifications
                .AsNoTracking()

                .Include(n => n.Employee)

                .Include(n => n.Booking)
                    .ThenInclude(b => b!.Room)

                .Include(n => n.Booking)
                    .ThenInclude(b => b!.Employee)

                .Where(n =>
                    n.Message.Contains("request") ||
                    n.Message.Contains("submitted") ||
                    n.Message.Contains("pending") ||
                    n.Message.Contains("rescheduled") ||
                    n.Message.Contains("requires approval") ||
                    n.Message.Contains("cancelled") ||
                    n.Message.Contains("canceled"))

                .OrderByDescending(
                    n => n.CreatedAt)

                .Take(100)

                .ToListAsync();

        // -----------------------------------------------------
        // REMOVE DUPLICATE ACTION NOTIFICATIONS
        // -----------------------------------------------------

        var distinctNotifications =
            list
                .GroupBy(n => new
                {
                    BookingId =
                        n.BookingId ?? 0,

                    Action =
                        GetNotificationAction(
                            n.Message)
                })
                .Select(group =>
                    group
                        .OrderByDescending(
                            n => n.CreatedAt)
                        .First())
                .OrderByDescending(
                    n => n.CreatedAt)
                .Take(50)
                .ToList();

        return distinctNotifications
            .Select(MapAdminNotification)
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
        var list =
            await _context.Notifications
                .AsNoTracking()

                .Include(n => n.Employee)

                .Include(n => n.Booking)
                    .ThenInclude(b => b!.Room)

                .Include(n => n.Booking)
                    .ThenInclude(b => b!.Employee)

                .OrderByDescending(
                    n => n.CreatedAt)

                .Take(50)

                .ToListAsync();

        return list
            .Select(MapNotification)
            .ToList();
    }

    // =========================================================
    // MARK ALL AS READ
    // =========================================================

    public async Task MarkAllAsReadAsync(
        int employeeId)
    {
        List<Notification> unreadNotifications;

        if (employeeId == 0)
        {
            // -------------------------------------------------
            // ADMIN
            // -------------------------------------------------

            unreadNotifications =
                await _context.Notifications
                    .Where(n =>
                        !n.IsRead)
                    .ToListAsync();
        }
        else
        {
            // -------------------------------------------------
            // EMPLOYEE
            // -------------------------------------------------

            unreadNotifications =
                await _context.Notifications
                    .Where(n =>
                        n.EmployeeId == employeeId &&
                        !n.IsRead)
                    .ToListAsync();
        }

        foreach (var notification
                 in unreadNotifications)
        {
            notification.IsRead = true;
        }

        await _context.SaveChangesAsync();
    }

    // =========================================================
    // ADD
    // =========================================================

    public async Task AddAsync(
        Notification notification)
    {
        await _context.Notifications
            .AddAsync(notification);
    }

    // =========================================================
    // SAVE
    // =========================================================

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    // =========================================================
    // MAP EMPLOYEE / GENERAL NOTIFICATION
    // =========================================================

    private static NotificationDto
        MapNotification(
            Notification n)
    {
        return new NotificationDto
        {
            NotificationId =
                n.NotificationId,

            Title =
                DeriveTitle(
                    n.Message),

            Message =
                n.Message,

            IsRead =
                n.IsRead,

            CreatedOn =
                n.CreatedAt,

            CreatedAt =
                n.CreatedAt,

            TimeAgo =
                FormatTimeAgo(
                    n.CreatedAt),

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
        };
    }

    // =========================================================
    // MAP ADMIN NOTIFICATION
    // =========================================================

    private static NotificationDto
        MapAdminNotification(
            Notification n)
    {
        var booking =
            n.Booking;

        var employeeName =
            n.Employee?.Name
            ?? booking?.Employee?.Name
            ?? "Employee";

        var roomName =
            booking?.Room?.RoomName
            ?? "Meeting Room";

        var isRescheduled =
            n.Message.Contains(
                "rescheduled",
                StringComparison.OrdinalIgnoreCase);

        var isCancelled =
            n.Message.Contains(
                "cancelled",
                StringComparison.OrdinalIgnoreCase)
            ||
            n.Message.Contains(
                "canceled",
                StringComparison.OrdinalIgnoreCase);

        string title;

        if (isRescheduled)
        {
            title =
                "Booking Rescheduled";
        }
        else if (isCancelled)
        {
            title =
                "Booking Cancelled";
        }
        else
        {
            title =
                "Booking Request";
        }

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
            message =
                n.Message;
        }

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
                FormatTimeAgo(
                    n.CreatedAt),

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
    }

    // =========================================================
    // GET NOTIFICATION ACTION
    // =========================================================

    private static string GetNotificationAction(
        string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return "Notification";
        }

        if (message.Contains(
                "rescheduled",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Rescheduled";
        }

        if (message.Contains(
                "cancelled",
                StringComparison.OrdinalIgnoreCase)
            ||
            message.Contains(
                "canceled",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Cancelled";
        }

        if (message.Contains(
                "rejected",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Rejected";
        }

        if (message.Contains(
                "approved",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Approved";
        }

        if (message.Contains(
                "request",
                StringComparison.OrdinalIgnoreCase)
            ||
            message.Contains(
                "submitted",
                StringComparison.OrdinalIgnoreCase)
            ||
            message.Contains(
                "pending",
                StringComparison.OrdinalIgnoreCase)
            ||
            message.Contains(
                "requires approval",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Request";
        }

        return "Notification";
    }

    // =========================================================
    // NOTIFICATION TITLE
    // =========================================================

    private static string DeriveTitle(
        string? message)
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
                "approved",
                StringComparison.OrdinalIgnoreCase)
            ||
            message.Contains(
                "approve",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Booking Approved";
        }

        if (message.Contains(
                "rejected",
                StringComparison.OrdinalIgnoreCase)
            ||
            message.Contains(
                "reject",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Booking Rejected";
        }

        if (message.Contains(
                "cancelled",
                StringComparison.OrdinalIgnoreCase)
            ||
            message.Contains(
                "canceled",
                StringComparison.OrdinalIgnoreCase)
            ||
            message.Contains(
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

        if (message.Contains(
                "request",
                StringComparison.OrdinalIgnoreCase)
            ||
            message.Contains(
                "submitted",
                StringComparison.OrdinalIgnoreCase)
            ||
            message.Contains(
                "pending",
                StringComparison.OrdinalIgnoreCase)
            ||
            message.Contains(
                "requires approval",
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
            DateTime.UtcNow -
            utcCreated;

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