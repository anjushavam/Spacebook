using Microsoft.EntityFrameworkCore;
using SpaceBook.Application.DTOs.Employee;
using SpaceBook.Application.Interfaces;
using SpaceBook.Infrastructure.Data;

namespace SpaceBook.Infrastructure.Repositories;

public class EmployeeDashboardRepository : IEmployeeDashboardRepository
{
    private readonly ApplicationDbContext _context;

    public EmployeeDashboardRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<EmployeeDashboardDto> GetDashboardAsync(int employeeId)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var currentTime = TimeOnly.FromDateTime(DateTime.Now);

        // Bookings Today
        var bookingsToday = await _context.Bookings
            .AsNoTracking()
            .CountAsync(x =>
                x.EmployeeId == employeeId &&
                x.BookingDate == today &&
                x.Status != "Cancelled" &&
                x.Status != "Rejected");

        // Upcoming Bookings Count
        var upcomingCount = await _context.Bookings
            .AsNoTracking()
            .CountAsync(x =>
                x.EmployeeId == employeeId &&
                (
                    x.BookingDate > today ||
                    (x.BookingDate == today && x.EndTime > currentTime)
                ) &&
                x.Status != "Cancelled" &&
                x.Status != "Rejected");

        // Recent Reservations
        var recentReservations = await _context.Bookings
            .AsNoTracking()
            .Include(x => x.Room)
            .Where(x =>
                x.EmployeeId == employeeId &&
                x.Status != "Cancelled" &&
                x.Status != "Rejected")
            .OrderByDescending(x => x.BookingDate)
            .ThenByDescending(x => x.StartTime)
            .Take(5)
            .Select(x => new RecentReservationDto
            {
                BookingId = x.BookingId,
                RoomName = x.Room!.RoomName,
                BookingDate = x.BookingDate,
                StartTime = x.StartTime,
                EndTime = x.EndTime,
                Status = x.Status
            })
            .ToListAsync();

        // Today's Meetings
        var todayMeetings = await _context.Bookings
            .AsNoTracking()
            .Include(x => x.Room)
            .Where(x =>
                x.EmployeeId == employeeId &&
                x.BookingDate == today &&
                x.Status != "Cancelled" &&
                x.Status != "Rejected")
            .OrderBy(x => x.StartTime)
            .Select(x => new TodayMeetingDto
            {
                BookingId = x.BookingId,
                Purpose = x.Purpose,
                RoomName = x.Room!.RoomName,
                StartTime = x.StartTime,
                EndTime = x.EndTime,
                Status = x.Status
            })
            .ToListAsync();

        return new EmployeeDashboardDto
        {
            BookingsToday = bookingsToday,
            UpcomingCount = upcomingCount,
            RecentReservations = recentReservations,
            TodayMeetings = todayMeetings
        };
    }

    // ==========================
    // Availability Calendar API
    // ==========================

    public async Task<AvailabilityCalendarDto> GetAvailabilityAsync(DateOnly date)
    {
        var result = new AvailabilityCalendarDto
        {
            Date = date
        };

        var rooms = await _context.Rooms
            .Include(r => r.RoomType)
            .AsNoTracking()
            .ToListAsync();

        foreach (var room in rooms)
        {
            var bookings = await _context.Bookings
                .Where(b =>
                    b.RoomId == room.RoomId &&
                    b.BookingDate == date &&
                    b.Status != "Cancelled" &&
                    b.Status != "Rejected")
                .ToListAsync();

            List<TimeSlotDto> slots = new();

            TimeOnly start = new TimeOnly(8, 0);

            while (start < new TimeOnly(18, 0))
            {
                TimeOnly end = start.AddHours(1);

                bool isBooked = bookings.Any(b =>
                    b.StartTime < end &&
                    b.EndTime > start);

                slots.Add(new TimeSlotDto
                {
                    StartTime = start,
                    EndTime = end,
                    IsBooked = isBooked
                });

                start = end;
            }

            var booking = bookings
                .OrderBy(b => b.StartTime)
                .FirstOrDefault();

            result.Rooms.Add(new RoomAvailabilityDto
            {
                RoomId = room.RoomId,
                RoomName = room.RoomName,
                RoomType = room.RoomType!.TypeName,
                Module = room.Module,
                Capacity = room.Capacity,
                Status = booking == null ? "Available" : "Booked",
                AvailableSlots = slots.Count(x => !x.IsBooked),
                TimeSlots = slots,
                CurrentBooking = booking == null ? null : new BookingPreviewDto
                {
                    Purpose = booking.Purpose,
                    StartTime = booking.StartTime,
                    EndTime = booking.EndTime,
                    Status = booking.Status
                }
            });
        }

        return result;
    }

    // ==========================
    // My Bookings API
    // ==========================

    public async Task<List<MyBookingDto>> GetMyBookingsAsync(int employeeId)
    {
        return await _context.Bookings
            .AsNoTracking()
            .Include(x => x.Room)
            .Where(x => x.EmployeeId == employeeId)
            .OrderByDescending(x => x.BookingDate)
            .ThenByDescending(x => x.StartTime)
            .Select(x => new MyBookingDto
            {
                BookingId = x.BookingId,
                RoomName = x.Room!.RoomName,
                Purpose = x.Purpose,
                BookingDate = x.BookingDate,
                StartTime = x.StartTime,
                EndTime = x.EndTime,
                Status = x.Status
            })
            .ToListAsync();
    }
    public async Task<List<RecentReservationDto>> GetRecentReservationsAsync(int employeeId)
{
    return await _context.Bookings
        .AsNoTracking()
        .Include(x => x.Room)
        .Where(x =>
            x.EmployeeId == employeeId &&
            x.Status != "Cancelled" &&
            x.Status != "Rejected")
        .OrderByDescending(x => x.BookingDate)
        .ThenByDescending(x => x.StartTime)
        .Select(x => new RecentReservationDto
        {
            BookingId = x.BookingId,
            RoomName = x.Room!.RoomName,
            BookingDate = x.BookingDate,
            StartTime = x.StartTime,
            EndTime = x.EndTime,
            Status = x.Status
        })
        .ToListAsync();
}
}