using SpaceBook.Application.DTOs.Booking;

namespace SpaceBook.Application.Interfaces;

public interface IBookingService
{
    // Dashboard
    Task<BookingDashboardDto> GetDashboardAsync();

    // Get all bookings
    Task<IEnumerable<BookingDto>> GetAllAsync(
        BookingFilterDto filter);

    // Get booking by ID
    Task<BookingDetailsDto?> GetByIdAsync(
        int bookingId);

    // Delete booking
    Task DeleteAsync(
        int bookingId);
}