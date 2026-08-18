using Microsoft.EntityFrameworkCore;
using SpaceBook.Application.DTOs.Reports;
using SpaceBook.Application.Interfaces;
using SpaceBook.Infrastructure.Data;

namespace SpaceBook.Infrastructure.Repositories;

public class ReportRepository : IReportRepository
{
    private readonly ApplicationDbContext _context;

    public ReportRepository(ApplicationDbContext context)
    {
        _context = context;
    }


    // =========================================================
    // BOOKING TREND
    // =========================================================

    public async Task<BookingTrendDto> GetBookingTrendAsync(
        ReportFilterDto filter)
    {
        var query = _context.Bookings
            .Include(b => b.Room)
                .ThenInclude(r => r!.RoomType)
            .Include(b => b.Room)
                .ThenInclude(r => r!.Module)
            .AsQueryable();


        // -----------------------------------------------------
        // MODULE FILTER
        // -----------------------------------------------------

        if (!string.IsNullOrWhiteSpace(filter.Module))
        {
            var moduleName = filter.Module.Trim();

            query = query.Where(b =>
                b.Room != null &&
                b.Room.Module != null &&
                b.Room.Module.ModuleName == moduleName);
        }


        // -----------------------------------------------------
        // ROOM TYPE FILTER
        // -----------------------------------------------------

        if (filter.RoomTypeId.HasValue)
        {
            query = query.Where(b =>
                b.Room != null &&
                b.Room.RoomTypeId == filter.RoomTypeId.Value);
        }


        // -----------------------------------------------------
        // STATUS FILTER
        // -----------------------------------------------------

        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            query = query.Where(b =>
                b.Status == filter.Status);
        }


        // -----------------------------------------------------
        // GET BOOKINGS
        // -----------------------------------------------------

        var bookings =
            await query.ToListAsync();


        // -----------------------------------------------------
        // TOTAL BOOKINGS
        // -----------------------------------------------------

        int totalBookings =
            bookings.Count;


        // -----------------------------------------------------
        // UNIQUE ROOMS
        // -----------------------------------------------------

        int uniqueRooms =
            bookings
                .Select(b => b.RoomId)
                .Distinct()
                .Count();


        // -----------------------------------------------------
        // CONFIRMED RATE
        // -----------------------------------------------------

        double confirmedRate =
            totalBookings == 0
                ? 0
                : Math.Round(
                    bookings.Count(
                        b => b.Status == "Approved")
                    * 100.0 /
                    totalBookings,
                    2);


        // -----------------------------------------------------
        // RETURN RESULT
        // -----------------------------------------------------

        return new BookingTrendDto
        {
            TotalBookings =
                totalBookings,

            UniqueRooms =
                uniqueRooms,

            ConfirmedRate =
                confirmedRate,

            AverageDuration =
                "0h 0m",

            Chart =
                bookings
                    .GroupBy(b => b.Status)
                    .Select(g =>
                        new BookingTrendChartDto
                        {
                            Label = g.Key,
                            Count = g.Count()
                        })
                    .ToList()
        };
    }


    // =========================================================
    // BOOKING STATUS
    // =========================================================

    public async Task<List<BookingStatusDto>>
        GetBookingStatusAsync(
            ReportFilterDto filter)
    {
        var query = _context.Bookings
            .Include(b => b.Room)
                .ThenInclude(r => r!.Module)
            .AsQueryable();


        // -----------------------------------------------------
        // MODULE FILTER
        // -----------------------------------------------------

        if (!string.IsNullOrWhiteSpace(filter.Module))
        {
            var moduleName = filter.Module.Trim();

            query = query.Where(b =>
                b.Room != null &&
                b.Room.Module != null &&
                b.Room.Module.ModuleName == moduleName);
        }


        // -----------------------------------------------------
        // ROOM TYPE FILTER
        // -----------------------------------------------------

        if (filter.RoomTypeId.HasValue)
        {
            query = query.Where(b =>
                b.Room != null &&
                b.Room.RoomTypeId ==
                    filter.RoomTypeId.Value);
        }


        // -----------------------------------------------------
        // GROUP BY STATUS
        // -----------------------------------------------------

        var result =
            await query
                .GroupBy(b => b.Status)
                .Select(g =>
                    new BookingStatusDto
                    {
                        Status = g.Key,
                        Count = g.Count()
                    })
                .ToListAsync();


        return result;
    }


    // =========================================================
    // ROOM USAGE
    // =========================================================

    public async Task<List<RoomUsageDto>>
        GetRoomUsageAsync(
            ReportFilterDto filter)
    {
        var query = _context.Bookings
            .Include(b => b.Room)
                .ThenInclude(r => r!.RoomType)
            .Include(b => b.Room)
                .ThenInclude(r => r!.Module)
            .AsQueryable();


        // -----------------------------------------------------
        // MODULE FILTER
        // -----------------------------------------------------

        if (!string.IsNullOrWhiteSpace(filter.Module))
        {
            var moduleName = filter.Module.Trim();

            query = query.Where(b =>
                b.Room != null &&
                b.Room.Module != null &&
                b.Room.Module.ModuleName == moduleName);
        }


        // -----------------------------------------------------
        // ROOM TYPE FILTER
        // -----------------------------------------------------

        if (filter.RoomTypeId.HasValue)
        {
            query = query.Where(b =>
                b.Room != null &&
                b.Room.RoomTypeId ==
                    filter.RoomTypeId.Value);
        }


        // -----------------------------------------------------
        // GROUP BY ROOM TYPE
        // -----------------------------------------------------

        var result =
            await query
                .Where(b =>
                    b.Room != null &&
                    b.Room.RoomType != null)
                .GroupBy(b =>
                    b.Room!.RoomType!.TypeName)
                .Select(g =>
                    new RoomUsageDto
                    {
                        RoomType = g.Key,
                        Count = g.Count()
                    })
                .ToListAsync();


        return result;
    }
}