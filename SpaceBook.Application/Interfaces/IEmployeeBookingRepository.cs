using SpaceBook.Application.DTOs.Booking;
using SpaceBook.Domain.Entities;

namespace SpaceBook.Application.Interfaces;

public interface IEmployeeBookingRepository
{
    Task CreateBookingAsync(Booking booking);

    Task<bool> IsRoomAvailableAsync(
        int roomId,
        DateOnly bookingDate,
        TimeOnly startTime,
        TimeOnly endTime);

    Task<BookingDetailsDto?> GetBookingByIdAsync(
        int bookingId,
        int employeeId);

    Task<bool> CancelBookingAsync(
        int bookingId,
        int employeeId);

    Task<bool> UpdateBookingAsync(
        int bookingId,
        int employeeId,
        UpdateBookingRequestDto request);

    Task<List<AvailableRoomDto>> SearchAvailableRoomsAsync(
    SearchRoomsRequestDto request);    

    Task SaveChangesAsync();

    Task<bool> IsRoomAvailableAsync(
    int roomId,
    DateOnly bookingDate,
    TimeOnly startTime,
    TimeOnly endTime,
    int excludeBookingId);
}