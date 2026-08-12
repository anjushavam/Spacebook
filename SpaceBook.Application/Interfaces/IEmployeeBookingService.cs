using SpaceBook.Application.DTOs.Booking;

namespace SpaceBook.Application.Interfaces;

public interface IEmployeeBookingService
{
    // =========================================================
    // Create new room booking
    // =========================================================

    Task<int> CreateBookingAsync(
        int employeeId,
        CreateBookingRequestDto request);


    // =========================================================
    // View booking details
    // =========================================================

    Task<BookingDetailsDto?> GetBookingByIdAsync(
        int bookingId,
        int employeeId);


    // =========================================================
    // Cancel employee booking
    // =========================================================

    Task<bool> CancelBookingAsync(
        int bookingId,
        int employeeId);


    // =========================================================
    // Update / Reschedule booking
    // =========================================================

    Task<bool> UpdateBookingAsync(
        int bookingId,
        int employeeId,
        UpdateBookingRequestDto request);


    // =========================================================
    // Search available rooms
    //
    // Supports:
    // - Module
    // - Room Type
    // - Participant Count
    // - Facilities
    // - Booking Date
    // - Start Time
    // - End Time
    //
    // Any one criterion can be supplied.
    // =========================================================

    Task<List<AvailableRoomDto>> SearchAvailableRoomsAsync(
        SearchRoomsRequestDto request);


    // =========================================================
    // Get rooms belonging to a specific module
    //
    // Example:
    // "Module 2"
    //
    // Returns all available/non-blocked rooms belonging
    // to the selected module.
    // =========================================================

    Task<List<AvailableRoomDto>> GetRoomsByModuleAsync(
        string module);
}