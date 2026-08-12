using SpaceBook.Application.DTOs.Booking;
using SpaceBook.Domain.Entities;

namespace SpaceBook.Application.Interfaces;

public interface IEmployeeBookingRepository
{
    // =========================================================
    // Create Booking
    // =========================================================

    Task CreateBookingAsync(Booking booking);


    // =========================================================
    // Check Room Availability
    // =========================================================

    Task<bool> IsRoomAvailableAsync(
        int roomId,
        DateOnly bookingDate,
        TimeOnly startTime,
        TimeOnly endTime);


    // =========================================================
    // Check Room Availability
    // Exclude Existing Booking
    // =========================================================

    Task<bool> IsRoomAvailableAsync(
        int roomId,
        DateOnly bookingDate,
        TimeOnly startTime,
        TimeOnly endTime,
        int excludeBookingId);


    // =========================================================
    // Get Booking Details
    // =========================================================

    Task<BookingDetailsDto?> GetBookingByIdAsync(
        int bookingId,
        int employeeId);


    // =========================================================
    // Cancel Booking
    // =========================================================

    Task<bool> CancelBookingAsync(
        int bookingId,
        int employeeId);


    // =========================================================
    // Update / Reschedule Booking
    // =========================================================

    Task<bool> UpdateBookingAsync(
        int bookingId,
        int employeeId,
        UpdateBookingRequestDto request);


    // =========================================================
    // Search Available Rooms
    // =========================================================

    Task<List<AvailableRoomDto>> SearchAvailableRoomsAsync(
        SearchRoomsRequestDto request);


    // =========================================================
    // Get Rooms By Module
    // =========================================================

    Task<List<AvailableRoomDto>> GetRoomsByModuleAsync(
        string module);


    // =========================================================
    // Save Changes
    // =========================================================

    Task SaveChangesAsync();
}