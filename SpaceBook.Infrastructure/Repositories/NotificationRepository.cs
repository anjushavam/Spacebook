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
            .Include(n => n.Employee)
            .Include(n => n.Booking)
                .ThenInclude(b => b!.Room)
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

        }).ToList();
    }


    // =========================================================
    // Admin Notifications
    // =========================================================

    public async Task<List<NotificationDto>> GetAdminNotificationsAsync()
    {
        var list = await _context.Notifications
            .AsNoTracking()

            // Employee who submitted the booking
            .Include(n => n.Employee)

            // Booking and Room
            .Include(n => n.Booking)
                .ThenInclude(b => b!.Room)

            // Employee associated with Booking
            .Include(n => n.Booking)
                .ThenInclude(b => b!.Employee)

            .Where(n =>
                n.Message.ToLower().Contains("request") ||
                n.Message.ToLower().Contains("submitted") ||
                n.Message.ToLower().Contains("pending"))

            .OrderByDescending(n => n.CreatedAt)

            .Take(50)

            .ToListAsync();


        return list.Select(n =>
        {
            var booking = n.Booking;


            // -------------------------------------------------
            // Employee Name
            // -------------------------------------------------

            var employeeName =
                n.Employee?.Name
                ?? booking?.Employee?.Name
                ?? "Employee";


            // -------------------------------------------------
            // Room Name
            // -------------------------------------------------

            var roomName =
                booking?.Room?.RoomName
                ?? "Meeting Room";


            // -------------------------------------------------
            // Notification Message
            // -------------------------------------------------

            var message = booking != null
                ? $"{employeeName} submitted a booking request for {roomName}."
                : n.Message;


            // -------------------------------------------------
            // Return DTO
            // -------------------------------------------------

            return new NotificationDto
            {
                NotificationId =
                    n.NotificationId,

                Title =
                    "Booking Request",

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

        }).ToList();
    }


    // =========================================================
    // Mark Notifications As Read
    // =========================================================

    public async Task MarkAllAsReadAsync(int employeeId)
    {
        var unreadNotifications = employeeId == 0

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
    // Notification Title
    // =========================================================

    private static string DeriveTitle(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return "Notification";
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
                StringComparison.OrdinalIgnoreCase))
        {
            return "Booking Request";
        }


        return "Notification";
    }


    // =========================================================
    // Time Ago
    // =========================================================

    private static string FormatTimeAgo(DateTime created)
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


        return utcCreated.ToString("MMM dd, yyyy");
    }
}