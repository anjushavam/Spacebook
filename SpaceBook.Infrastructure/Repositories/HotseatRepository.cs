using Microsoft.EntityFrameworkCore;
using SpaceBook.Application.DTOs.Hotseat;
using SpaceBook.Application.Interfaces;
using SpaceBook.Infrastructure.Data;

namespace SpaceBook.Infrastructure.Repositories;

public class HotseatRepository : IHotseatRepository
{
    private readonly ApplicationDbContext _context;

    public HotseatRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<HotseatSeatDto>> GetSeatsAsync(
        DateOnly? date,
        string? city,
        string? building,
        string? module)
    {
        var query = _context.Seats
            .Include(s => s.Module)
            .ThenInclude(m => m!.Office)
            .ThenInclude(o => o!.Location)
            .Where(s => s.IsActive)
            .AsQueryable();

        // Filter by module
        if (!string.IsNullOrWhiteSpace(module))
        {
            query = query.Where(s =>
                s.Module != null &&
                s.Module.ModuleName == module);
        }

        // Filter by building
        if (!string.IsNullOrWhiteSpace(building))
        {
            query = query.Where(s =>
                s.Module != null &&
                s.Module.Office != null &&
                s.Module.Office.OfficeName == building);
        }

        // Filter by city
        if (!string.IsNullOrWhiteSpace(city))
        {
            query = query.Where(s =>
                s.Module != null &&
                s.Module.Office != null &&
                s.Module.Office.Location != null &&
                s.Module.Office.Location.LocationName == city);
        }

        var seats = await query
            .OrderBy(s => s.ModuleId)
            .ThenBy(s => s.Section)
            .ThenBy(s => s.RowNumber)
            .ThenBy(s => s.ColumnNumber)
            .Select(s => new HotseatSeatDto
            {
                SeatNumber = s.SeatNumber,
                Section = s.Section ?? "",
                Row = s.RowNumber,

                Status = date.HasValue
                    ? _context.HotseatBookings.Any(h =>
                        h.SeatId == s.SeatId &&
                        h.BookingDate == date.Value &&
                        h.BookingStatus == "Confirmed")
                        ? "Reserved"
                        : "Vacant"
                    : "Vacant"
            })
            .ToListAsync();

        return seats;
    }
}