using SpaceBook.Application.DTOs.Booking;

namespace SpaceBook.Application.Interfaces;

public interface IBookingService
{
    Task<BookingDashboardDto> GetDashboardAsync();

    Task<IEnumerable<BookingDto>> GetAllAsync(BookingFilterDto filter);

    Task<BookingDetailsDto?> GetByIdAsync(int bookingId);

    Task ApproveAsync(int bookingId);

    Task RejectAsync(int bookingId);

    Task DeleteAsync(int bookingId);
}