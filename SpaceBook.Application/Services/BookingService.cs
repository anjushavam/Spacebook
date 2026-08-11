using SpaceBook.Application.DTOs.Booking;
using SpaceBook.Application.Interfaces;
using SpaceBook.Domain.Entities;

namespace SpaceBook.Application.Services;

public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly INotificationRepository _notificationRepository;

    public BookingService(
        IBookingRepository bookingRepository,
        INotificationRepository notificationRepository)
    {
        _bookingRepository = bookingRepository;
        _notificationRepository = notificationRepository;
    }

    // =========================================================
    // Dashboard
    // =========================================================

    public async Task<BookingDashboardDto> GetDashboardAsync()
    {
        return await _bookingRepository
            .GetDashboardAsync();
    }

    // =========================================================
    // Get All Bookings
    // =========================================================

    public async Task<IEnumerable<BookingDto>> GetAllAsync(
        BookingFilterDto filter)
    {
        return await _bookingRepository
            .GetAllAsync(filter);
    }

    // =========================================================
    // Get Booking By ID
    // =========================================================

    public async Task<BookingDetailsDto?> GetByIdAsync(
        int bookingId)
    {
        return await _bookingRepository
            .GetByIdAsync(bookingId);
    }

    // =========================================================
    // APPROVE BOOKING
    // =========================================================

    public async Task ApproveAsync(int bookingId)
    {
        if (!await _bookingRepository.ExistsAsync(bookingId))
        {
            throw new Exception("Booking not found.");
        }

        // Get booking BEFORE changing status.
        // We need EmployeeId for the notification.
        var booking =
            await _bookingRepository.GetByIdAsync(bookingId);

        if (booking == null)
        {
            throw new Exception("Booking not found.");
        }

        // Approve booking
        await _bookingRepository
            .ApproveAsync(bookingId);

        // =====================================================
        // CREATE EMPLOYEE NOTIFICATION
        // =====================================================

        var purpose =
            !string.IsNullOrWhiteSpace(booking.Purpose)
                ? booking.Purpose
                : !string.IsNullOrWhiteSpace(booking.MeetingTitle)
                    ? booking.MeetingTitle
                    : "Workspace";

        var employeeNotification = new Notification
        {
            EmployeeId = booking.EmployeeId,

            BookingId = bookingId,

            Message =
                $"Your booking for {purpose} has been approved by the admin.",

            IsRead = false,

            CreatedAt = DateTime.UtcNow
        };

        await _notificationRepository
            .AddAsync(employeeNotification);

        await _notificationRepository
            .SaveChangesAsync();
    }

    // =========================================================
    // REJECT BOOKING
    // =========================================================

    public async Task RejectAsync(int bookingId)
    {
        if (!await _bookingRepository.ExistsAsync(bookingId))
        {
            throw new Exception("Booking not found.");
        }

        // Get booking BEFORE changing status.
        var booking =
            await _bookingRepository.GetByIdAsync(bookingId);

        if (booking == null)
        {
            throw new Exception("Booking not found.");
        }

        // Reject booking
        await _bookingRepository
            .RejectAsync(bookingId);

        // =====================================================
        // CREATE EMPLOYEE NOTIFICATION
        // =====================================================

        var purpose =
            !string.IsNullOrWhiteSpace(booking.Purpose)
                ? booking.Purpose
                : !string.IsNullOrWhiteSpace(booking.MeetingTitle)
                    ? booking.MeetingTitle
                    : "Workspace";

        var employeeNotification = new Notification
        {
            EmployeeId = booking.EmployeeId,

            BookingId = bookingId,

            Message =
                $"Your booking for {purpose} has been rejected by the admin.",

            IsRead = false,

            CreatedAt = DateTime.UtcNow
        };

        await _notificationRepository
            .AddAsync(employeeNotification);

        await _notificationRepository
            .SaveChangesAsync();
    }

    // =========================================================
    // DELETE BOOKING
    // =========================================================

    public async Task DeleteAsync(int bookingId)
    {
        if (!await _bookingRepository.ExistsAsync(bookingId))
        {
            throw new Exception("Booking not found.");
        }

        await _bookingRepository
            .DeleteAsync(bookingId);
    }
}