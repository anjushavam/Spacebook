using SpaceBook.Application.DTOs.Booking;
 
namespace SpaceBook.Application.Interfaces;
 
public interface IEmployeeBookingService
{
    // Create new room booking
    Task<int> CreateBookingAsync(
        int employeeId,
        CreateBookingRequestDto request);
 
 
    // View booking details
    Task<BookingDetailsDto?> GetBookingByIdAsync(
        int bookingId,
        int employeeId);
 
 
    // Cancel employee booking
    Task<bool> CancelBookingAsync(
        int bookingId,
        int employeeId);
 
 
    // Update / Reschedule booking
    Task<bool> UpdateBookingAsync(
        int bookingId,
        int employeeId,
        UpdateBookingRequestDto request);
 
 
    // Search available rooms
    Task<List<AvailableRoomDto>> SearchAvailableRoomsAsync(
        SearchRoomsRequestDto request);
}