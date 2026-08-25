using Microsoft.EntityFrameworkCore;
using SpaceBook.Application.Interfaces;
using SpaceBook.Domain.Entities;
using SpaceBook.Infrastructure.Data;

namespace SpaceBook.Infrastructure.Repositories;

public class MissedCheckInRepository 
    : IMissedCheckInRepository
{
    private readonly ApplicationDbContext _context;

    private static readonly TimeZoneInfo IndiaTimeZone = GetIndiaTimeZone();

    private static TimeZoneInfo GetIndiaTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");
        }
        catch (TimeZoneNotFoundException)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            }
            catch
            {
                return TimeZoneInfo.Utc;
            }
        }
        catch (InvalidTimeZoneException)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            }
            catch
            {
                return TimeZoneInfo.Utc;
            }
        }
    }

    private static DateOnly GetIndiaToday()
    {
        var indiaNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, IndiaTimeZone);
        return DateOnly.FromDateTime(indiaNow);
    }

    public MissedCheckInRepository(
        ApplicationDbContext context)
    {
        _context = context;
    }


    public async Task<List<Booking>> GetTodayApprovedBookingsAsync()
    {
        var today = GetIndiaToday();

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