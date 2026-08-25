using Microsoft.EntityFrameworkCore;
using SpaceBook.Application.DTOs.Booking;
using SpaceBook.Application.Interfaces;
using SpaceBook.Domain.Entities;
using SpaceBook.Infrastructure.Data;

namespace SpaceBook.Infrastructure.Repositories;

public class BookingRepository : IBookingRepository
{
    private readonly ApplicationDbContext _context;

    public BookingRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    // =========================================================
    // DASHBOARD
    // =========================================================

    public async Task<BookingDashboardDto> GetDashboardAsync()
    {
        return new BookingDashboardDto
        {
            PendingRequests = await _context.Bookings
                .CountAsync(x => x.Status == "Pending"),

            Confirmed = await _context.Bookings
                .CountAsync(x => x.Status == "Approved"),

            Cancelled = await _context.Bookings
                .CountAsync(x => x.Status == "Cancelled")
        };
    }

    // =========================================================
    // GET ALL BOOKINGS
    // =========================================================

    public async Task<IEnumerable<BookingDto>> GetAllAsync(
        BookingFilterDto filter)
    {
        var query = _context.Bookings
            .AsNoTracking()
            .AsQueryable();

        // =====================================================
        // SEARCH
        // =====================================================

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();

            query = query.Where(x =>
                (x.MeetingTitle != null &&
                 x.MeetingTitle.Contains(search)) ||

                (x.Room != null &&
                 x.Room.RoomName.Contains(search)) ||

                (x.Room != null &&
                 x.Room.Module != null &&
                 x.Room.Module.ModuleName.Contains(search)) ||

                (x.Employee != null &&
                 x.Employee.Name.Contains(search)));
        }

        // =====================================================
        // STATUS FILTER
        // =====================================================

        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            query = query.Where(x =>
                x.Status == filter.Status);
        }

        // =====================================================
        // RETURN BOOKINGS
        // =====================================================

        return await query
            .OrderByDescending(x => x.BookedOn)
            .Select(x => new BookingDto
            {
                BookingId = x.BookingId,

                RoomId = x.RoomId,

                RoomName =
                    x.Room != null
                        ? x.Room.RoomName
                        : string.Empty,

                Module =
                    x.Room != null &&
                    x.Room.Module != null
                        ? x.Room.Module.ModuleName
                        : string.Empty,

                EmployeeName =
                    x.Employee != null
                        ? x.Employee.Name
                        : string.Empty,

                MeetingTitle =
                    x.MeetingTitle ?? string.Empty,

                BookingDate = x.BookingDate,

                StartTime = x.StartTime,

                EndTime = x.EndTime,

                Status = x.Status
            })
            .Take(100)
            .ToListAsync();
    }

    // =========================================================
    // GET BOOKING BY ID
    // =========================================================

    public async Task<BookingDetailsDto?> GetByIdAsync(
        int bookingId)
    {
        return await _context.Bookings
            .AsNoTracking()
            .Where(x => x.BookingId == bookingId)
            .Select(x => new BookingDetailsDto
            {
                BookingId = x.BookingId,

                EmployeeId = x.EmployeeId,

                MeetingTitle =
                    x.MeetingTitle ?? string.Empty,

                ParticipantCount =
                    x.ParticipantCount,

                RoomName =
                    x.Room != null
                        ? x.Room.RoomName
                        : string.Empty,

                Module =
                    x.Room != null &&
                    x.Room.Module != null
                        ? x.Room.Module.ModuleName
                        : string.Empty,

                EmployeeName =
                    x.Employee != null
                        ? x.Employee.Name
                        : string.Empty,

                BookingDate = x.BookingDate,

                StartTime = x.StartTime,

                EndTime = x.EndTime,

                Status = x.Status,

                BookedOn = x.BookedOn
            })
            .FirstOrDefaultAsync();
    }

    // =========================================================
    // DELETE BOOKING
    // =========================================================

    public async Task DeleteAsync(int bookingId)
    {
        var booking = await _context.Bookings
            .FirstOrDefaultAsync(x =>
                x.BookingId == bookingId);

        if (booking == null)
        {
            throw new KeyNotFoundException(
                "Booking not found.");
        }

        _context.Bookings.Remove(booking);

        await _context.SaveChangesAsync();
    }

    // =========================================================
    // CHECK BOOKING EXISTS
    // =========================================================

    public async Task<bool> ExistsAsync(int bookingId)
    {
        return await _context.Bookings
            .AnyAsync(x =>
                x.BookingId == bookingId);
    }

    // =========================================================
    // CHECK ROOM AVAILABILITY
    // =========================================================

    public async Task<bool> IsRoomAvailableAsync(
        int roomId,
        DateOnly bookingDate,
        TimeOnly startTime,
        TimeOnly endTime)
    {
        return !await _context.Bookings
            .AnyAsync(b =>
                b.RoomId == roomId &&
                b.BookingDate == bookingDate &&
                b.Status != "Cancelled" &&
                b.Status != "Rejected" &&
                startTime < b.EndTime &&
                endTime > b.StartTime);
    }

    // =========================================================
    // CREATE BOOKING
    // =========================================================

    public async Task AddAsync(Booking booking)
    {
        await _context.Bookings.AddAsync(booking);

        await _context.SaveChangesAsync();
    }
}