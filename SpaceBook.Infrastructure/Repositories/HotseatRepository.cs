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
            .AsNoTracking()
            .Include(s => s.Module)
                .ThenInclude(m => m!.Office)
                    .ThenInclude(o => o!.Location)
            .Where(s => s.IsActive)
            .AsQueryable();

        // =========================================================
        // MODULE FILTER
        // Works for ELCOT, TIDEL OIS, and future modules.
        // =========================================================

        if (!string.IsNullOrWhiteSpace(module))
        {
            var trimmedModule = module.Trim().ToLower();
            query = query.Where(s =>
                s.Module != null &&
                s.Module.ModuleName.ToLower() == trimmedModule);
        }

        // =========================================================
        // BUILDING FILTER
        // =========================================================

        if (!string.IsNullOrWhiteSpace(building))
        {
            var trimmedBuilding = building.Trim().ToLower();
            query = query.Where(s =>
                s.Module != null &&
                s.Module.Office != null &&
                s.Module.Office.OfficeName.ToLower() == trimmedBuilding);
        }

        // =========================================================
        // CITY FILTER
        // =========================================================

        if (!string.IsNullOrWhiteSpace(city))
        {
            var trimmedCity = city.Trim().ToLower();
            query = query.Where(s =>
                s.Module != null &&
                s.Module.Office != null &&
                s.Module.Office.Location != null &&
                s.Module.Office.Location.LocationName.ToLower() == trimmedCity);
        }

        // =========================================================
        // GET SEATS
        // =========================================================

        var seats = await query
            .OrderBy(s => s.ModuleId)
            .ThenBy(s => s.Section)
            .ThenBy(s => s.RowNumber)
            .ThenBy(s => s.ColumnNumber)
            .Select(s => new HotseatSeatDto
            {
                SeatId = s.SeatId,
                SeatNumber = s.SeatNumber,
                Section = s.Section ?? "",
                Row = s.RowNumber,

                Status = date.HasValue
                    ? s.HotseatBookings.Any(h =>
                        h.BookingDate == date.Value &&
                        (
                            h.BookingStatus == "Confirmed" ||
                            h.BookingStatus == "CheckedIn" ||
                            h.BookingStatus == "Checked In" ||
                            h.BookingStatus == "Checked-In"
                        ))
                        ? "Booked"
                        : "Vacant"
                    : "Vacant"
            })
            .ToListAsync();

        return seats;
    }
}