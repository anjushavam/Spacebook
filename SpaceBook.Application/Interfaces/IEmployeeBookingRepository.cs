using SpaceBook.Application.DTOs.Booking;
using SpaceBook.Domain.Entities;

namespace SpaceBook.Application.Interfaces;

public interface IEmployeeBookingRepository
{
    // =========================================================
    // CREATE BOOKING
    // =========================================================

    Task CreateBookingAsync(Booking booking);

    // =========================================================
    // SAVE CHANGES
    // =========================================================

    Task SaveChangesAsync();

    // =========================================================
    // CHECK ROOM AVAILABILITY
    // =========================================================

    Task<bool> IsRoomAvailableAsync(
        int roomId,
        DateOnly bookingDate,
        TimeOnly startTime,
        TimeOnly endTime);

    // =========================================================
    // CHECK ROOM AVAILABILITY
    // EXCLUDE EXISTING BOOKING
    // =========================================================

    Task<bool> IsRoomAvailableAsync(
        int roomId,
        DateOnly bookingDate,
        TimeOnly startTime,
        TimeOnly endTime,
        int excludeBookingId);

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
        string reason);

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
    // CHECK ROOM CAPACITY
    // =========================================================

    Task<bool> HasRoomWithRequiredCapacityAsync(
        SearchRoomsRequestDto request);

    // =========================================================
    // GET ROOMS BY MODULE
    // =========================================================

    Task<List<AvailableRoomDto>> GetRoomsByModuleAsync(
        string module);

    // =========================================================
    // GET ROOM CAPACITY
    // =========================================================

    Task<int?> GetRoomCapacityAsync(
        int roomId);

    // =========================================================
    // GET EMPLOYEE NAME
    // =========================================================

    Task<string?> GetEmployeeNameAsync(
        int employeeId);

    // =========================================================
    // GET EMPLOYEE & ROOM & ADMIN EMAILS
    // =========================================================

    Task<Employee?> GetEmployeeByIdAsync(
        int employeeId);

    Task<Room?> GetRoomByIdAsync(
        int roomId);

    Task<List<string>> GetAdminEmailsAsync();
}