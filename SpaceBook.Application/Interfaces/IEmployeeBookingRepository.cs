using SpaceBook.Application.DTOs.Booking;
using SpaceBook.Domain.Entities;

namespace SpaceBook.Application.Interfaces;

public interface IEmployeeBookingRepository
{
    // =========================================================
    // Create Booking
    // =========================================================

    Task CreateBookingAsync(
        Booking booking);


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
    // Used while editing/rescheduling
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
    //
    // Supports partial criteria:
    // Module
    // Room Type
    // Capacity
    // Facilities
    // Date
    // Start Time
    // End Time
    // =========================================================

    Task<List<AvailableRoomDto>> SearchAvailableRoomsAsync(
        SearchRoomsRequestDto request);


    // =========================================================
    // Get Rooms By Module
    //
    // Example:
    // "Module 2"
    //
    // Returns rooms belonging to the selected module.
    // =========================================================

    Task<List<AvailableRoomDto>> GetRoomsByModuleAsync(
        string module);


    // =========================================================
    // Save Changes
    // =========================================================

    Task SaveChangesAsync();
}