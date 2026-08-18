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

    public async Task<IEnumerable<HotseatSeatDto>> GetHotseatsAsync(
        DateOnly? date,
        string? city,
        string? building,
        string? module)
    {
        // =========================================================
        // START FROM SEATS
        // =========================================================

        var query = _context.Seats
            .Include(s => s.Module)
                .ThenInclude(m => m.Office)
                    .ThenInclude(o => o.Location)
            .Include(s => s.HotseatBookings)
            .Where(s => s.IsActive)
            .AsQueryable();

        // =========================================================
        // CITY FILTER
        // =========================================================

        if (!string.IsNullOrWhiteSpace(city))
        {
            var cityValue = city.Trim().ToLower();

            query = query.Where(s =>
                s.Module != null &&
                s.Module.Office != null &&
                s.Module.Office.Location != null &&
                s.Module.Office.Location.LocationName
                    .ToLower()
                    .Contains(cityValue));
        }

        // =========================================================
        // BUILDING / OFFICE FILTER
        // =========================================================

        if (!string.IsNullOrWhiteSpace(building))
        {
            var buildingValue = building.Trim().ToLower();

            query = query.Where(s =>
                s.Module != null &&
                s.Module.Office != null &&
                s.Module.Office.OfficeName
                    .ToLower()
                    .Contains(buildingValue));
        }

        // =========================================================
        // MODULE FILTER
        // =========================================================

        if (!string.IsNullOrWhiteSpace(module))
        {
            var moduleValue = module.Trim().ToLower();

            query = query.Where(s =>
                s.Module != null &&
                s.Module.ModuleName
                    .ToLower()
                    .Contains(moduleValue));
        }

        // =========================================================
        // GET ALL ACTIVE SEATS
        // =========================================================

        var seats = await query
            .OrderBy(s => s.Section)
            .ThenBy(s => s.RowNumber)
            .ThenBy(s => s.ColumnNumber)
            .ToListAsync();

        // =========================================================
        // CONVERT TO DTO
        // =========================================================

        var result = seats.Select(seat =>
        {
            var booking = date.HasValue
                ? seat.HotseatBookings
                    .FirstOrDefault(b =>
                        b.BookingDate == date.Value &&
                        b.BookingStatus != "Cancelled" &&
                        b.ReleasedOn == null)
                : null;

            string status;

            if (booking == null)
            {
                status = "Vacant";
            }
            else if (booking.CheckInTime.HasValue)
            {
                status = "Occupied";
            }
            else
            {
                status = "Reserved";
            }

            return new HotseatSeatDto
            {
                SeatNumber = seat.SeatNumber,
                Section = seat.Section ?? string.Empty,
                Row = seat.RowNumber,
                Status = status
            };
        });

        return result;
    }
}