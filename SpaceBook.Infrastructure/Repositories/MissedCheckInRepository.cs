using Microsoft.EntityFrameworkCore;
using SpaceBook.Application.Interfaces;
using SpaceBook.Domain.Entities;
using SpaceBook.Infrastructure.Data;

namespace SpaceBook.Infrastructure.Repositories;

public class MissedCheckInRepository 
    : IMissedCheckInRepository
{
    private readonly ApplicationDbContext _context;


    public MissedCheckInRepository(
        ApplicationDbContext context)
    {
        _context = context;
    }


    public async Task<List<Booking>> GetTodayApprovedBookingsAsync()
    {
        var today = DateOnly.FromDateTime(
            DateTime.Now);


        return await _context.Bookings
            .Where(b =>
                b.Status == "Approved" &&
                b.BookingDate == today)
            .ToListAsync();
    }


    public async Task<bool> HasCheckInAsync(
        int bookingId)
    {
        return await _context.CheckIns
            .AnyAsync(c =>
                c.BookingId == bookingId);
    }
}