using SpaceBook.Application.DTOs.Booking;
using SpaceBook.Domain.Entities;

namespace SpaceBook.Application.Interfaces;

public interface IBookingRepository
{
    // =========================================================
    // DASHBOARD
    // =========================================================

    Task<BookingDashboardDto> GetDashboardAsync();

    // =========================================================
    // GET ALL BOOKINGS
    // =========================================================

    Task<IEnumerable<BookingDto>> GetAllAsync(
        BookingFilterDto filter);

    // =========================================================
    // GET BOOKING BY ID
    // =========================================================

    Task<BookingDetailsDto?> GetByIdAsync(
        int bookingId);

    // =========================================================
    // DELETE BOOKING
    // =========================================================

    Task DeleteAsync(
        int bookingId);

    // =========================================================
    // CHECK BOOKING EXISTS
    // =========================================================

    Task<bool> ExistsAsync(
        int bookingId);

    // =========================================================
    // CHECK ROOM AVAILABILITY
    // FSD-E02: Prevent double booking
    // =========================================================

    Task<bool> IsRoomAvailableAsync(
        int roomId,
        DateOnly bookingDate,
        TimeOnly startTime,
        TimeOnly endTime);

    // =========================================================
    // CREATE BOOKING
    // FSD-E02
    // =========================================================

    Task AddAsync(
        Booking booking);
}