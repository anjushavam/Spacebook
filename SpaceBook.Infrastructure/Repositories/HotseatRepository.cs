using Microsoft.EntityFrameworkCore;
using SpaceBook.Application.Interfaces;
using SpaceBook.Domain.Entities;
using SpaceBook.Infrastructure.Data;

namespace SpaceBook.Infrastructure.Repositories;

public class HotseatRepository : IHotseatRepository
{
    private readonly ApplicationDbContext _context;

    public HotseatRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<HotseatBooking>> GetHotseatBookingsAsync(
        DateOnly? date,
        string? city,
        string? building,
        string? module)
    {
        var query = _context.HotseatBookings
            .Include(h => h.Seat)
                .ThenInclude(s => s.Module)
                    .ThenInclude(m => m.Office)
                        .ThenInclude(o => o.Location)
            .AsQueryable();

        // =========================================================
        // DATE FILTER
        // =========================================================

        if (date.HasValue)
        {
            query = query.Where(h =>
                h.BookingDate == date.Value);
        }

        // =========================================================
        // CITY FILTER
        // =========================================================

        if (!string.IsNullOrWhiteSpace(city))
        {
            query = query.Where(h =>
                h.Seat != null &&
                h.Seat.Module != null &&
                h.Seat.Module.Office != null &&
                h.Seat.Module.Office.Location != null &&
                h.Seat.Module.Office.Location.LocationName
                    .ToLower()
                    .Contains(city.ToLower()));
        }

        // =========================================================
        // BUILDING / OFFICE FILTER
        // =========================================================

        if (!string.IsNullOrWhiteSpace(building))
        {
            query = query.Where(h =>
                h.Seat != null &&
                h.Seat.Module != null &&
                h.Seat.Module.Office != null &&
                h.Seat.Module.Office.OfficeName
                    .ToLower()
                    .Contains(building.ToLower()));
        }

        // =========================================================
        // MODULE FILTER
        // =========================================================

        if (!string.IsNullOrWhiteSpace(module))
        {
            query = query.Where(h =>
                h.Seat != null &&
                h.Seat.Module != null &&
                h.Seat.Module.ModuleName
                    .ToLower()
                    .Contains(module.ToLower()));
        }

        // =========================================================
        // RETURN RESULT
        // =========================================================

        return await query
            .OrderByDescending(h => h.BookingDate)
            .ThenBy(h => h.SeatId)
            .ToListAsync();
    }
}