using SpaceBook.Application.DTOs.Booking;

namespace SpaceBook.Application.Interfaces;

public interface IEmployeeBookingService
{
    // =========================================================
    // CREATE BOOKING
    // =========================================================

    Task<int> CreateBookingAsync(
        int employeeId,
        CreateBookingRequestDto request);


    // =========================================================
    // GET BOOKING DETAILS
    // =========================================================

    Task<BookingDetailsDto?> GetBookingByIdAsync(
        int bookingId,
        int employeeId);


    // =========================================================
    // CANCEL BOOKING
    // =========================================================

    Task<bool> CancelBookingAsync(
        int bookingId,
        int employeeId);


    // =========================================================
    // UPDATE / RESCHEDULE BOOKING
    // =========================================================

    Task<bool> UpdateBookingAsync(
        int bookingId,
        int employeeId,
        UpdateBookingRequestDto request);


    // =========================================================
    // SEARCH AVAILABLE ROOMS
    //
    // Supported criteria:
    // - Module
    // - Room Type
    // - Participant Count
    // - Facilities
    // - Booking Date
    // - Start Time
    // - End Time
    //
    // Criteria are optional and can be combined.
    // =========================================================

    Task<List<AvailableRoomDto>> SearchAvailableRoomsAsync(
        SearchRoomsRequestDto request);


    // =========================================================
    // GET ROOMS BY MODULE
    //
    // Returns available/non-blocked rooms belonging
    // to the specified module.
    // =========================================================

    Task<List<AvailableRoomDto>> GetRoomsByModuleAsync(
        string module);
}
