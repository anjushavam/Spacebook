using Microsoft.EntityFrameworkCore;
using SpaceBook.Application.Interfaces;
using SpaceBook.Domain.Entities;
using SpaceBook.Domain.Enums;
using SpaceBook.Infrastructure.Data;

namespace SpaceBook.Infrastructure.Repositories;

public class BookingReminderRepository : IBookingReminderRepository
{
    private readonly ApplicationDbContext _context;

    public BookingReminderRepository(
        ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Booking>> GetTodayBookingsNeedingRemindersAsync(
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        return await _context.Bookings
            // Employee is required for Name and Email
            .Include(b => b.Employee)
            // Room is required for RoomName / RoomNumber
            .Include(b => b.Room)
            // Email notifications history
            .Include(b => b.EmailNotifications)
            .Where(b =>
                b.BookingDate == date &&
                // Only approved bookings should receive reminders
                b.Status == "Approved")
            .OrderBy(b => b.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasNotificationBeenSentAsync(
        int bookingId,
        string notificationType,
        CancellationToken cancellationToken = default)
    {
        return await _context.BookingEmailNotifications
            .AsNoTracking()
            .AnyAsync(n =>
                n.BookingId == bookingId &&
                n.NotificationType == notificationType &&
                n.Status == "Sent",
                cancellationToken);
    }

    public async Task RecordNotificationSentAsync(
        int bookingId,
        string notificationType,
        string status = "Sent",
        CancellationToken cancellationToken = default)
    {
        var existing = await _context.BookingEmailNotifications
            .FirstOrDefaultAsync(n =>
                n.BookingId == bookingId &&
                n.NotificationType == notificationType,
                cancellationToken);

        if (existing != null)
        {
            existing.SentAt = DateTime.UtcNow;
            existing.Status = status;
        }
        else
        {
            await _context.BookingEmailNotifications.AddAsync(
                new BookingEmailNotification
                {
                    BookingId = bookingId,
                    NotificationType = notificationType,
                    SentAt = DateTime.UtcNow,
                    Status = status
                },
                cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task ResetRemindersForBookingAsync(
        int bookingId,
        CancellationToken cancellationToken = default)
    {
        var reminders = await _context.BookingEmailNotifications
            .Where(n =>
                n.BookingId == bookingId &&
                (n.NotificationType == BookingNotificationType.StartReminder15Minutes ||
                 n.NotificationType == BookingNotificationType.EndReminder15Minutes))
            .ToListAsync(cancellationToken);

        if (reminders.Count > 0)
        {
            _context.BookingEmailNotifications.RemoveRange(reminders);
        }

        var booking = await _context.Bookings
            .FirstOrDefaultAsync(b => b.BookingId == bookingId, cancellationToken);

        if (booking != null)
        {
            booking.StartReminderSent = false;
            booking.EndReminderSent = false;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<string>> GetAdminEmailsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Employees
            .AsNoTracking()
            .Include(e => e.Role)
            .Where(e =>
                e.Role != null &&
                (e.Role.RoleName == "Admin" || e.Role.RoleName == "ADMIN" || e.Role.RoleName == "admin") &&
                !string.IsNullOrWhiteSpace(e.Email))
            .Select(e => e.Email)
            .ToListAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}