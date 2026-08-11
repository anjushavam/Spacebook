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
    // Dashboard
    // =========================================================

    public async Task<BookingDashboardDto> GetDashboardAsync()
    {
        return new BookingDashboardDto
        {
            PendingRequests =
                await _context.Bookings
                    .CountAsync(x => x.Status == "Pending"),

            Confirmed =
                await _context.Bookings
                    .CountAsync(x => x.Status == "Approved"),

            Cancelled =
                await _context.Bookings
                    .CountAsync(x => x.Status == "Cancelled")
        };
    }

    // =========================================================
    // Get All Bookings
    // =========================================================

    public async Task<IEnumerable<BookingDto>> GetAllAsync(
        BookingFilterDto filter)
    {
        var query = _context.Bookings
            .Include(x => x.Room)
            .Include(x => x.Employee)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            query = query.Where(x =>
                x.Purpose.Contains(filter.Search) ||
                x.Room!.RoomName.Contains(filter.Search));
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
                BookingId = x.BookingId,
                Purpose = x.Purpose,
                RoomName = x.Room!.RoomName,
                EmployeeName = x.Employee!.Name,
                BookingDate = x.BookingDate,
                StartTime = x.StartTime,
                EndTime = x.EndTime,
                Status = x.Status
            })
            .ToListAsync();
    }

    // =========================================================
    // Get Booking By Id
    // =========================================================

    public async Task<BookingDetailsDto?> GetByIdAsync(
        int bookingId)
    {
        return await _context.Bookings
            .Include(x => x.Room)
            .Include(x => x.Employee)
            .Where(x => x.BookingId == bookingId)
            .Select(x => new BookingDetailsDto
            {
                BookingId = x.BookingId,

                // IMPORTANT
                // This is required for employee notifications.
                EmployeeId = x.EmployeeId,

                MeetingTitle = x.MeetingTitle,

                Purpose = x.Purpose,

                ParticipantCount = x.ParticipantCount,

                RoomName = x.Room!.RoomName,

                EmployeeName = x.Employee!.Name,

                BookingDate = x.BookingDate,

                StartTime = x.StartTime,

                EndTime = x.EndTime,

                Status = x.Status,

                BookedOn = x.BookedOn
            })
            .FirstOrDefaultAsync();
    }

    // =========================================================
    // Approve Booking
    // =========================================================

    public async Task ApproveAsync(int bookingId)
    {
        var booking =
            await _context.Bookings.FindAsync(bookingId);

        if (booking != null)
        {
            booking.Status = "Approved";

            await _context.SaveChangesAsync();
        }
    }

    // =========================================================
    // Reject Booking
    // =========================================================

    public async Task RejectAsync(int bookingId)
    {
        var booking =
            await _context.Bookings.FindAsync(bookingId);

        if (booking != null)
        {
            booking.Status = "Rejected";

            await _context.SaveChangesAsync();
        }
    }

    // =========================================================
    // Delete Booking
    // =========================================================

    public async Task DeleteAsync(int bookingId)
    {
        var booking =
            await _context.Bookings.FindAsync(bookingId);

        if (booking != null)
        {
            _context.Bookings.Remove(booking);

            await _context.SaveChangesAsync();
        }
    }

    // =========================================================
    // Check Booking Exists
    // =========================================================

    public async Task<bool> ExistsAsync(int bookingId)
    {
        return await _context.Bookings
            .AnyAsync(x => x.BookingId == bookingId);
    }

    // =========================================================
    // Check Room Availability
    // =========================================================

    public async Task<bool> IsRoomAvailableAsync(
        int roomId,
        DateOnly bookingDate,
        TimeOnly startTime,
        TimeOnly endTime)
    {
        return !await _context.Bookings.AnyAsync(b =>
            b.RoomId == roomId &&
            b.BookingDate == bookingDate &&
            b.Status != "Cancelled" &&
            startTime < b.EndTime &&
            endTime > b.StartTime
        );
    }

    // =========================================================
    // Create Booking
    // =========================================================

    public async Task AddAsync(Booking booking)
    {
        await _context.Bookings.AddAsync(booking);

        await _context.SaveChangesAsync();
    }
}