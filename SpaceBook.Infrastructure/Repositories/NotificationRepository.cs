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

                // Room booking
                .Include(n => n.Booking)
                    .ThenInclude(b => b!.Room)

                .Include(n => n.Booking)
                    .ThenInclude(b => b!.Employee)

                // Hotseat booking
                .Include(n => n.HotseatBooking)
                    .ThenInclude(h => h!.Seat)

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

                // Admin notifications are only for
                // normal room bookings.
                .Where(n =>
                    n.BookingId != null &&

                    (
                        EF.Functions.ILike(
                            n.Message,
                            "%request%") ||

                        EF.Functions.ILike(
                            n.Message,
                            "%submitted%") ||

                        EF.Functions.ILike(
                            n.Message,
                            "%pending%") ||

                        EF.Functions.ILike(
                            n.Message,
                            "%rescheduled%") ||

                        EF.Functions.ILike(
                            n.Message,
                            "%requires approval%") ||

                        EF.Functions.ILike(
                            n.Message,
                            "%cancelled%") ||

                        EF.Functions.ILike(
                            n.Message,
                            "%canceled%") ||

                        EF.Functions.ILike(
                            n.Message,
                            "%approved%") ||

                        EF.Functions.ILike(
                            n.Message,
                            "%rejected%") ||

                        EF.Functions.ILike(
                            n.Message,
                            "%booked%") ||

                        EF.Functions.ILike(
                            n.Message,
                            "%booking%")
                    ))

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

                .Include(n => n.HotseatBooking)
                    .ThenInclude(h => h!.Seat)

                .OrderByDescending(
                    n => n.CreatedAt)

                .Take(50)

                .ToListAsync();

        return list
            .Select(MapNotification)
            .ToList();
    }

    // =========================================================
    // MARK SINGLE NOTIFICATION AS READ
    // =========================================================

    public async Task MarkAsReadAsync(
        int notificationId,
        int employeeId)
    {
        var notification =
            await _context.Notifications
                .FirstOrDefaultAsync(n =>
                    n.NotificationId ==
                        notificationId &&

                    n.EmployeeId ==
                        employeeId);

        if (notification == null)
        {
            throw new KeyNotFoundException(
                "Notification not found.");
        }

        if (notification.IsRead)
        {
            return;
        }

        notification.IsRead = true;

        await _context.SaveChangesAsync();
    }

    // =========================================================
    // MARK ALL AS READ
    // =========================================================

    public async Task MarkAllAsReadAsync(
        int employeeId)
    {
        // -----------------------------------------------------
        // IMPORTANT
        //
        // EmployeeId must represent an actual employee.
        //
        // Do not use employeeId = 0 here for admin because that
        // would mark other users' notifications as read.
        // -----------------------------------------------------

        if (employeeId <= 0)
        {
            return;
        }

        var unreadNotifications =
            await _context.Notifications
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
    // ADD
    // =========================================================

    public async Task AddAsync(
        Notification notification)
    {
        // -----------------------------------------------------
        // VALIDATE NOTIFICATION TARGET
        // -----------------------------------------------------

        if (!notification.BookingId.HasValue &&
            !notification.HotseatBookingId.HasValue)
        {
            // General notification is still allowed.
        }

        // A single notification should not point to both types.
        if (notification.BookingId.HasValue &&
            notification.HotseatBookingId.HasValue)
        {
            throw new InvalidOperationException(
                "A notification cannot reference both a room booking and a hotseat booking.");
        }

        if (string.IsNullOrWhiteSpace(
                notification.Message))
        {
            throw new InvalidOperationException(
                "Notification message is required.");
        }

        if (notification.Message.Length > 500)
        {
            notification.Message =
                notification.Message[..500];
        }

        if (notification.CreatedAt == default)
        {
            notification.CreatedAt =
                DateTime.UtcNow;
        }

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
        var isRoomBooking =
            n.BookingId.HasValue;

        var isHotseat =
            n.HotseatBookingId.HasValue;

        return new NotificationDto
        {
            NotificationId =
                n.NotificationId,

            Title =
                DeriveTitle(
                    n.Message,
                    isHotseat),

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

            // Only room bookings have RoomName
            RoomName =
                isRoomBooking
                    ? n.Booking?.Room?.RoomName
                    : null,

            // Booking date can come from either type
            BookingDate =
                isRoomBooking
                    ? n.Booking?.BookingDate
                    : isHotseat
                        ? n.HotseatBooking?.BookingDate
                        : null,

            // Hotseat bookings do not have room times
            StartTime =
                isRoomBooking
                    ? n.Booking?.StartTime
                    : null,

            EndTime =
                isRoomBooking
                    ? n.Booking?.EndTime
                    : null
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

        var isRejected =
            n.Message.Contains(
                "rejected",
                StringComparison.OrdinalIgnoreCase);

        var isApproved =
            n.Message.Contains(
                "approved",
                StringComparison.OrdinalIgnoreCase);

        var isBooked =
            n.Message.Contains(
                "booked",
                StringComparison.OrdinalIgnoreCase);

        string title;

        // -----------------------------------------------------
        // PRIORITY
        // -----------------------------------------------------

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
        else if (isRejected)
        {
            title =
                "Booking Rejected";
        }
        else if (isApproved)
        {
            title =
                "Booking Approved";
        }
        else if (isBooked)
        {
            // AUTO-APPROVAL FLOW
            title =
                "New Booking";
        }
        else
        {
            // Legacy notifications
            title =
                "Booking Request";
        }

        string message;

        // -----------------------------------------------------
        // RESCHEDULED
        // -----------------------------------------------------

        if (isRescheduled)
        {
            message =
                $"{employeeName} rescheduled a booking for " +
                $"{roomName}.";
        }

        // -----------------------------------------------------
        // CANCELLED
        // -----------------------------------------------------

        else if (isCancelled)
        {
            message =
                $"{employeeName} cancelled a booking for " +
                $"{roomName}.";
        }

        // -----------------------------------------------------
        // REJECTED
        // -----------------------------------------------------

        else if (isRejected)
        {
            message =
                $"{employeeName}'s booking for " +
                $"{roomName} was rejected.";
        }

        // -----------------------------------------------------
        // APPROVED
        // -----------------------------------------------------

        else if (isApproved)
        {
            message =
                $"{employeeName}'s booking for " +
                $"{roomName} was approved.";
        }

        // -----------------------------------------------------
        // NEW BOOKING
        // AUTO-APPROVAL FLOW
        // -----------------------------------------------------

        else if (isBooked)
        {
            var meetingTitle =
                !string.IsNullOrWhiteSpace(
                    booking?.MeetingTitle)
                    ? $" for '{booking.MeetingTitle}'"
                    : "";

            message =
                $"{employeeName} booked " +
                $"{roomName}{meetingTitle}.";
        }

        // -----------------------------------------------------
        // LEGACY REQUEST
        // -----------------------------------------------------

        else if (booking != null)
        {
            message =
                $"{employeeName} submitted a booking request " +
                $"for {roomName}.";
        }

        // -----------------------------------------------------
        // GENERAL NOTIFICATION
        // -----------------------------------------------------

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
                "booked",
                StringComparison.OrdinalIgnoreCase))
        {
            // AUTO-APPROVAL FLOW
            return "Booked";
        }

        if (message.Contains(
                "confirmed",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Confirmed";
        }

        if (message.Contains(
                "checked in",
                StringComparison.OrdinalIgnoreCase))
        {
            return "CheckedIn";
        }

        if (message.Contains(
                "expired",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Expired";
        }

        // -----------------------------------------------------
        // LEGACY REQUEST NOTIFICATIONS
        // -----------------------------------------------------

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
        string? message,
        bool isHotseat)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return isHotseat
                ? "Hotseat Notification"
                : "Notification";
        }

        // -----------------------------------------------------
        // HOTSEAT CHECK-IN
        // -----------------------------------------------------

        if (message.Contains(
                "checked in",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Hotseat Check-in";
        }

        // -----------------------------------------------------
        // HOTSEAT EXPIRED
        // -----------------------------------------------------

        if (message.Contains(
                "expired",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Hotseat Booking Expired";
        }

        // -----------------------------------------------------
        // BOOKED
        // -----------------------------------------------------

        if (message.Contains(
                "booked",
                StringComparison.OrdinalIgnoreCase))
        {
            return isHotseat
                ? "Hotseat Booking Confirmed"
                : "Booking Confirmed";
        }

        // -----------------------------------------------------
        // CONFIRMED
        // -----------------------------------------------------

        if (message.Contains(
                "confirmed",
                StringComparison.OrdinalIgnoreCase))
        {
            return isHotseat
                ? "Hotseat Booking Confirmed"
                : "Booking Confirmed";
        }

        // -----------------------------------------------------
        // RESCHEDULED
        // -----------------------------------------------------

        if (message.Contains(
                "rescheduled",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Booking Rescheduled";
        }

        // -----------------------------------------------------
        // APPROVED
        // -----------------------------------------------------

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

        // -----------------------------------------------------
        // REJECTED
        // -----------------------------------------------------

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

        // -----------------------------------------------------
        // CANCELLED
        // -----------------------------------------------------

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
            return isHotseat
                ? "Hotseat Booking Cancelled"
                : "Booking Cancelled";
        }

        // -----------------------------------------------------
        // MISSED CHECK-IN
        // -----------------------------------------------------

        if (message.Contains(
                "missed",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Missed Check-in";
        }

        // -----------------------------------------------------
        // LEGACY REQUEST
        // -----------------------------------------------------

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

        // -----------------------------------------------------
        // REMINDER
        // -----------------------------------------------------

        if (message.Contains(
                "reminder",
                StringComparison.OrdinalIgnoreCase)
            ||
            message.Contains(
                "starts in 15 minutes",
                StringComparison.OrdinalIgnoreCase)
            ||
            message.Contains(
                "ends in 15 minutes",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Booking Reminder";
        }

        return isHotseat
            ? "Hotseat Notification"
            : "Notification";
    }

    // =========================================================
    // TIME AGO
    // =========================================================

    private static string FormatTimeAgo(
        DateTime created)
    {
        var utcCreated =
            created.Kind == DateTimeKind.Utc
                ? created
                : created.ToUniversalTime();

        var span =
            DateTime.UtcNow -
            utcCreated;

        if (span.TotalSeconds < 0 ||
            span.TotalSeconds < 60)
        {
            return "Just now";
        }

        if (span.TotalMinutes < 60)
        {
            return
                $"{(int)span.TotalMinutes}m ago";
        }

        if (span.TotalHours < 24)
        {
            return
                $"{(int)span.TotalHours}h ago";
        }

        if (span.TotalDays < 7)
        {
            return
                $"{(int)span.TotalDays}d ago";
        }

        return utcCreated.ToString(
            "MMM dd, yyyy");
    }
}