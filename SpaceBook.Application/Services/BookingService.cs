using SpaceBook.Application.DTOs.Booking;
using SpaceBook.Application.Interfaces;

namespace SpaceBook.Application.Services;

public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepository;

    public BookingService(
        IBookingRepository bookingRepository)
    {
        _bookingRepository = bookingRepository;
    }

    // =========================================================
    // DASHBOARD
    // =========================================================

    public async Task<BookingDashboardDto> GetDashboardAsync()
    {
        return await _bookingRepository.GetDashboardAsync();
    }

    // =========================================================
    // GET ALL BOOKINGS
    // =========================================================

    public async Task<IEnumerable<BookingDto>> GetAllAsync(
        BookingFilterDto filter)
    {
        return await _bookingRepository.GetAllAsync(filter);
    }

    // =========================================================
    // GET BOOKING BY ID
    // =========================================================

    public async Task<BookingDetailsDto?> GetByIdAsync(
        int bookingId)
    {
        return await _bookingRepository.GetByIdAsync(
            bookingId);
    }

    // =========================================================
    // DELETE BOOKING
    // =========================================================

    public async Task DeleteAsync(
        int bookingId)
    {
        // -----------------------------------------------------
        // 1. CHECK WHETHER BOOKING EXISTS
        // -----------------------------------------------------

        var exists =
            await _bookingRepository.ExistsAsync(
                bookingId);

        if (!exists)
        {
            throw new KeyNotFoundException(
                "Booking not found.");
        }

        // -----------------------------------------------------
        // 2. DELETE BOOKING
        // -----------------------------------------------------

        await _bookingRepository.DeleteAsync(
            bookingId);
    }
}