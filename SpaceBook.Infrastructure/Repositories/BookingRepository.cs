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
            PendingRequests =
                await _context.Bookings
                    .CountAsync(x =>
                        x.Status == "Pending"),

            Confirmed =
                await _context.Bookings
                    .CountAsync(x =>
                        x.Status == "Approved"),

            Cancelled =
                await _context.Bookings
                    .CountAsync(x =>
                        x.Status == "Cancelled")
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
            .Include(x => x.Room)
                .ThenInclude(r => r!.Module)
            .Include(x => x.Employee)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search =
                filter.Search.Trim();

            query = query.Where(x =>
                x.Purpose.Contains(search) ||

                (x.Room != null &&
                 x.Room.RoomName.Contains(search)) ||

                (x.Room != null &&
                 x.Room.Module != null &&
                 x.Room.Module.ModuleName.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            query = query.Where(x =>
                x.Status == filter.Status);
        }

        return await query
            .OrderByDescending(x => x.BookedOn)
            .Select(x => new BookingDto
            {
                BookingId =
                    x.BookingId,

                Purpose =
                    x.Purpose,

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

                BookingDate =
                    x.BookingDate,

                StartTime =
                    x.StartTime,

                EndTime =
                    x.EndTime,

                Status =
                    x.Status
            })
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

            .Include(x => x.Room)
                .ThenInclude(r => r!.Module)

            .Include(x => x.Employee)

            .Where(x =>
                x.BookingId == bookingId)

            .Select(x => new BookingDetailsDto
            {
                BookingId =
                    x.BookingId,

                EmployeeId =
                    x.EmployeeId,

                MeetingTitle =
                    x.MeetingTitle,

                Purpose =
                    x.Purpose,

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

                BookingDate =
                    x.BookingDate,

                StartTime =
                    x.StartTime,

                EndTime =
                    x.EndTime,

                Status =
                    x.Status,

                BookedOn =
                    x.BookedOn
            })
            .FirstOrDefaultAsync();
    }

    // =========================================================
    // APPROVE BOOKING
    // =========================================================

    public async Task ApproveAsync(
        int bookingId)
    {
        var booking =
            await _context.Bookings
                .FirstOrDefaultAsync(x =>
                    x.BookingId == bookingId);

        if (booking == null)
        {
            throw new KeyNotFoundException(
                "Booking not found.");
        }

        // -----------------------------------------------------
        // ONLY PENDING BOOKINGS CAN BE APPROVED
        // -----------------------------------------------------

        if (!string.Equals(
                booking.Status,
                "Pending",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Booking cannot be approved because its current status is {booking.Status}.");
        }

        booking.Status =
            "Approved";

        await _context.SaveChangesAsync();
    }

    // =========================================================
    // REJECT BOOKING
    // =========================================================

    public async Task RejectAsync(
        int bookingId)
    {
        var booking =
            await _context.Bookings
                .FirstOrDefaultAsync(x =>
                    x.BookingId == bookingId);

        if (booking == null)
        {
            throw new KeyNotFoundException(
                "Booking not found.");
        }

        // -----------------------------------------------------
        // ONLY PENDING BOOKINGS CAN BE REJECTED
        // -----------------------------------------------------

        if (!string.Equals(
                booking.Status,
                "Pending",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Booking cannot be rejected because its current status is {booking.Status}.");
        }

        booking.Status =
            "Rejected";

        await _context.SaveChangesAsync();
    }

    // =========================================================
    // DELETE BOOKING
    // =========================================================

    public async Task DeleteAsync(
        int bookingId)
    {
        var booking =
            await _context.Bookings
                .FirstOrDefaultAsync(x =>
                    x.BookingId == bookingId);

        if (booking == null)
        {
            throw new KeyNotFoundException(
                "Booking not found.");
        }

        _context.Bookings.Remove(
            booking);

        await _context.SaveChangesAsync();
    }

    // =========================================================
    // CHECK BOOKING EXISTS
    // =========================================================

    public async Task<bool> ExistsAsync(
        int bookingId)
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

    public async Task AddAsync(
        Booking booking)
    {
        await _context.Bookings
            .AddAsync(booking);

        await _context.SaveChangesAsync();
    }
}