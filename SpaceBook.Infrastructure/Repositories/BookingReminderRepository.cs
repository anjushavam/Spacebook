using Microsoft.EntityFrameworkCore;
using SpaceBook.Application.Interfaces;
using SpaceBook.Domain.Entities;
using SpaceBook.Infrastructure.Data;

namespace SpaceBook.Infrastructure.Repositories;

public class BookingReminderRepository : IBookingReminderRepository
{
    private readonly ApplicationDbContext _context;

    public BookingReminderRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Booking>> GetTodayBookingsNeedingRemindersAsync(
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        return await _context.Bookings
            .Include(b => b.Employee)
            .Include(b => b.Room)
            .Where(b =>
                b.BookingDate == date &&
                b.Status == "Approved" &&
                (!b.StartReminderSent || !b.EndReminderSent))
            .ToListAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
