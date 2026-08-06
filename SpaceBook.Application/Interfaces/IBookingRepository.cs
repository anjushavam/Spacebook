using SpaceBook.Application.DTOs.Booking;
using SpaceBook.Domain.Entities;
 
namespace SpaceBook.Application.Interfaces;
 
public interface IBookingRepository
{
    Task<BookingDashboardDto> GetDashboardAsync();
 
 
    Task<IEnumerable<BookingDto>> GetAllAsync(
        BookingFilterDto filter);
 
 
    Task<BookingDetailsDto?> GetByIdAsync(
        int bookingId);
 
 
    Task ApproveAsync(
        int bookingId);
 
 
    Task RejectAsync(
        int bookingId);
 
 
    Task DeleteAsync(
        int bookingId);
 
 
    Task<bool> ExistsAsync(
        int bookingId);
 
 
    // FSD-E02: Prevent double booking
    Task<bool> IsRoomAvailableAsync(
        int roomId,
        DateOnly bookingDate,
        TimeOnly startTime,
        TimeOnly endTime);
 
 
    // FSD-E02: Create booking
    Task AddAsync(
        Booking booking);
}