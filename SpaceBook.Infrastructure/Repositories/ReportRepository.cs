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


    public async Task<BookingTrendDto> GetBookingTrendAsync(
        ReportFilterDto filter)
    {
        var query = _context.Bookings
            .Include(b => b.Room)
            .ThenInclude(r => r.RoomType)
            .AsQueryable();


        if (!string.IsNullOrEmpty(filter.Module))
        {
            query = query.Where(b =>
                b.Room!.Module == filter.Module);
        }


        if (filter.RoomTypeId.HasValue)
        {
            query = query.Where(b =>
                b.Room!.RoomTypeId == filter.RoomTypeId);
        }


        if (!string.IsNullOrEmpty(filter.Status))
        {
            query = query.Where(b =>
                b.Status == filter.Status);
        }


        var bookings = await query.ToListAsync();


        int totalBookings = bookings.Count;


        int uniqueRooms = bookings
            .Select(b => b.RoomId)
            .Distinct()
            .Count();


        double confirmedRate = totalBookings == 0
            ? 0
            : Math.Round(
                bookings.Count(b => b.Status == "Approved")
                * 100.0 / totalBookings, 2);


        return new BookingTrendDto
        {
            TotalBookings = totalBookings,

            UniqueRooms = uniqueRooms,

            ConfirmedRate = confirmedRate,

            AverageDuration = "0h 0m",

            Chart = bookings
                .GroupBy(b => b.Status)
                .Select(g => new BookingTrendChartDto
                {
                    Label = g.Key,
                    Count = g.Count()
                })
                .ToList()
        };
    }



    public async Task<List<BookingStatusDto>> GetBookingStatusAsync(
        ReportFilterDto filter)
    {
        var query = _context.Bookings
            .Include(b => b.Room)
            .AsQueryable();


        if (!string.IsNullOrEmpty(filter.Module))
        {
            query = query.Where(b =>
                b.Room!.Module == filter.Module);
        }


        if (filter.RoomTypeId.HasValue)
        {
            query = query.Where(b =>
                b.Room!.RoomTypeId == filter.RoomTypeId);
        }


        var result = await query
            .GroupBy(b => b.Status)
            .Select(g => new BookingStatusDto
            {
                Status = g.Key,
                Count = g.Count()
            })
            .ToListAsync();


        return result;
    }



    public async Task<List<RoomUsageDto>> GetRoomUsageAsync(
    ReportFilterDto filter)
{
    var query = _context.Bookings
        .Include(b => b.Room)
        .ThenInclude(r => r.RoomType)
        .AsQueryable();


    if (!string.IsNullOrEmpty(filter.Module))
    {
        query = query.Where(b =>
            b.Room!.Module == filter.Module);
    }


    if (filter.RoomTypeId.HasValue)
    {
        query = query.Where(b =>
            b.Room!.RoomTypeId == filter.RoomTypeId);
    }


    var result = await query
        .GroupBy(b => b.Room!.RoomType!.TypeName)
        .Select(g => new RoomUsageDto
        {
            RoomType = g.Key,
            Count = g.Count()
        })
        .ToListAsync();


    return result;
}
}