using Microsoft.EntityFrameworkCore;
using SpaceBook.Application.DTOs.Booking;
using SpaceBook.Application.Interfaces;
using SpaceBook.Domain.Entities;
using SpaceBook.Infrastructure.Data;
 
namespace SpaceBook.Infrastructure.Repositories;
 
public class EmployeeBookingRepository : IEmployeeBookingRepository
{
    private readonly ApplicationDbContext _context;
 
    public EmployeeBookingRepository(ApplicationDbContext context)
    {
        _context = context;
    }
 
    // Create Booking
    public async Task CreateBookingAsync(Booking booking)
    {
        await _context.Bookings.AddAsync(booking);
    }
 
    // Check Room Availability
    public async Task<bool> IsRoomAvailableAsync(
        int roomId,
        DateOnly bookingDate,
        TimeOnly startTime,
        TimeOnly endTime)
    {
        return !await _context.Bookings.AnyAsync(b =>
            b.RoomId == roomId &&
            b.BookingDate == bookingDate &&
            b.Status != "Rejected" &&
            b.Status != "Cancelled" &&
            startTime < b.EndTime &&
            endTime > b.StartTime);
    }
 
    // View Booking
    public async Task<BookingDetailsDto?> GetBookingByIdAsync(
        int bookingId,
        int employeeId)
    {
        return await _context.Bookings
            .Where(b => b.BookingId == bookingId &&
                        b.EmployeeId == employeeId)
            .Select(b => new BookingDetailsDto
            {
                BookingId = b.BookingId,
                MeetingTitle = b.MeetingTitle,
                Purpose = b.Purpose,
                ParticipantCount = b.ParticipantCount,
                RoomName = b.Room!.RoomName,
                EmployeeName = b.Employee!.Name,
                BookingDate = b.BookingDate,
                StartTime = b.StartTime,
                EndTime = b.EndTime,
                Status = b.Status,
                BookedOn = b.BookedOn
            })
            .FirstOrDefaultAsync();
    }
 
    // Cancel Booking
    public async Task<bool> CancelBookingAsync(
        int bookingId,
        int employeeId)
    {
        var booking = await _context.Bookings.FirstOrDefaultAsync(b =>
            b.BookingId == bookingId &&
            b.EmployeeId == employeeId);
 
        if (booking == null)
        {
            return false;
        }
 
        booking.Status = "Cancelled";
 
        await _context.SaveChangesAsync();
 
        return true;
    }
 
    // Update Booking
    public async Task<bool> UpdateBookingAsync(
        int bookingId,
        int employeeId,
        UpdateBookingRequestDto request)
    {
        var booking = await _context.Bookings.FirstOrDefaultAsync(b =>
            b.BookingId == bookingId &&
            b.EmployeeId == employeeId);
 
        if (booking == null)
        {
            return false;
        }
 
        booking.MeetingTitle = request.MeetingTitle;
        booking.Purpose = request.Purpose;
        booking.ParticipantCount = request.ParticipantCount;
        booking.RoomId = request.RoomId;
        booking.BookingDate = request.BookingDate;
        booking.StartTime = request.StartTime;
        booking.EndTime = request.EndTime;
 
        await _context.SaveChangesAsync();
 
        return true;
    }
 
    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<List<AvailableRoomDto>> SearchAvailableRoomsAsync(
    SearchRoomsRequestDto request)
{
    return await _context.Rooms
        .Include(r => r.RoomType)
        .Where(r =>
            r.Module == request.Module &&
            r.RoomTypeId == request.RoomTypeId &&
            r.Capacity >= request.ParticipantCount)
        .Where(r => !_context.Bookings.Any(b =>
            b.RoomId == r.RoomId &&
            b.BookingDate == request.BookingDate &&
            b.Status != "Cancelled" &&
            b.Status != "Rejected" &&
            request.StartTime < b.EndTime &&
            request.EndTime > b.StartTime))
        .Select(r => new AvailableRoomDto
        {
            RoomId = r.RoomId,
            RoomName = r.RoomName,
            Module = r.Module,
            RoomType = r.RoomType!.TypeName,
            Capacity = r.Capacity
        })
        .ToListAsync();
}
// Check Room Availability during Reschedule
public async Task<bool> IsRoomAvailableAsync(
    int roomId,
    DateOnly bookingDate,
    TimeOnly startTime,
    TimeOnly endTime,
    int excludeBookingId)
{
    return !await _context.Bookings.AnyAsync(b =>
        b.RoomId == roomId &&
        b.BookingDate == bookingDate &&
        b.BookingId != excludeBookingId &&
        b.Status != "Rejected" &&
        b.Status != "Cancelled" &&
        startTime < b.EndTime &&
        endTime > b.StartTime);
}

}