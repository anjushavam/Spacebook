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
        int employeeId,
        string cancellationReason);

    // =========================================================
    // UPDATE / RESCHEDULE BOOKING
    // =========================================================

    Task<bool> UpdateBookingAsync(
        int bookingId,
        int employeeId,
        UpdateBookingRequestDto request);

    // =========================================================
    // SEARCH AVAILABLE ROOMS
    // =========================================================

    Task<List<AvailableRoomDto>> SearchAvailableRoomsAsync(
        SearchRoomsRequestDto request);

    // =========================================================
    // GET ROOMS BY MODULE
    // =========================================================

    Task<List<AvailableRoomDto>> GetRoomsByModuleAsync(
        string module);

    // =========================================================
    // GET ALL MODULES
    // =========================================================

    Task<List<ModuleDropdownDto>> GetModulesAsync();
}