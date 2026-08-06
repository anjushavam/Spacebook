using SpaceBook.Application.DTOs.Booking;
using SpaceBook.Application.Interfaces;
using SpaceBook.Domain.Entities;
 
namespace SpaceBook.Application.Services;
 
public class EmployeeBookingService : IEmployeeBookingService
{
    private readonly IEmployeeBookingRepository _bookingRepository;
 
 
    public EmployeeBookingService(
        IEmployeeBookingRepository bookingRepository)
    {
        _bookingRepository = bookingRepository;
    }
 
 
 
    // Create Booking
    public async Task<int> CreateBookingAsync(
        int employeeId,
        CreateBookingRequestDto request)
    {
 
        // Validate time
        if(request.StartTime >= request.EndTime)
        {
            throw new Exception(
                "End time must be after start time.");
        }
 
 
        // Check availability
        bool isAvailable =
            await _bookingRepository
            .IsRoomAvailableAsync(
                request.RoomId,
                request.BookingDate,
                request.StartTime,
                request.EndTime);
 
 
 
        if(!isAvailable)
        {
            throw new Exception(
                "Room is already booked for the selected time.");
        }
 
 
 
        var booking = new Booking
        {
            RoomId = request.RoomId,
 
            EmployeeId = employeeId,
 
            MeetingTitle = request.MeetingTitle,
 
            Purpose = request.Purpose,
 
            ParticipantCount =
                request.ParticipantCount,
 
            BookingDate =
                request.BookingDate,
 
            StartTime =
                request.StartTime,
 
            EndTime =
                request.EndTime,
 
            BookedOn =
                DateTime.UtcNow,
 
            Status =
                "Pending"
        };
 
 
        try
        {
            await _bookingRepository
                .CreateBookingAsync(booking);
 
 
            await _bookingRepository
                .SaveChangesAsync();
 
 
            return booking.BookingId;
        }
        catch(Exception ex)
        {
            throw new Exception(
                ex.InnerException?.Message
                ?? ex.Message,
                ex);
        }
    }
 
 
 
    // View Booking
    public async Task<BookingDetailsDto?> GetBookingByIdAsync(
        int bookingId,
        int employeeId)
    {
        return await _bookingRepository
            .GetBookingByIdAsync(
                bookingId,
                employeeId);
    }
 
 
 
    // Cancel Booking
    public async Task<bool> CancelBookingAsync(
        int bookingId,
        int employeeId)
    {
        return await _bookingRepository
            .CancelBookingAsync(
                bookingId,
                employeeId);
    }
 
 
 
    // Update / Reschedule Booking
public async Task<bool> UpdateBookingAsync(
    int bookingId,
    int employeeId,
    UpdateBookingRequestDto request)
{
    // Validate time
    if (request.StartTime >= request.EndTime)
    {
        throw new Exception(
            "End time must be after start time.");
    }
 
 
    // Get existing booking
    var existingBooking =
        await _bookingRepository
        .GetBookingByIdAsync(
            bookingId,
            employeeId);
 
 
    if (existingBooking == null)
    {
        throw new Exception(
            "Booking not found.");
    }
 
 
 
    // SLA check - cannot update within 1 hour
    var bookingStartDateTime =
        existingBooking.BookingDate
        .ToDateTime(existingBooking.StartTime);
 
 
    if (DateTime.Now >=
        bookingStartDateTime.AddHours(-1))
    {
        throw new Exception(
            "Booking cannot be rescheduled within 1 hour before start time.");
    }
 
 
 
    // Check new slot availability
    bool isAvailable =
        await _bookingRepository
        .IsRoomAvailableAsync(
            request.RoomId,
            request.BookingDate,
            request.StartTime,
            request.EndTime,bookingId);
 
 
    if (!isAvailable)
    {
        throw new Exception(
            "Room is already booked for the selected time.");
    }
 
 
 
    return await _bookingRepository
        .UpdateBookingAsync(
            bookingId,
            employeeId,
            request);
}
 
 
    // Search Available Rooms
    public async Task<List<AvailableRoomDto>>
        SearchAvailableRoomsAsync(
        SearchRoomsRequestDto request)
    {
        return await _bookingRepository
            .SearchAvailableRoomsAsync(request);
    }
}
 