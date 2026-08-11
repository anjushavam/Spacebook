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

    public async Task ApproveAsync(
        int bookingId)
    {
        // Check whether booking exists
        if (!await _bookingRepository.ExistsAsync(bookingId))
        {
            throw new Exception("Booking not found.");
        }

        // Get booking details BEFORE changing the status.
        // We need EmployeeId and booking information
        // to create the employee notification.
        var booking = await _bookingRepository
            .GetByIdAsync(bookingId);

        if (booking == null)
        {
            throw new Exception("Booking not found.");
        }

        // Change booking status to Approved
        await _bookingRepository
            .ApproveAsync(bookingId);

        // =====================================================
        // Create Employee Notification
        // =====================================================

        var purpose =
            booking.Purpose ??
            booking.MeetingTitle ??
            "Workspace";

        var employeeNotification = new Notification
        {
            EmployeeId = booking.EmployeeId,

            // IMPORTANT:
            // Store the booking ID so the notification
            // remains linked to the booking.
            BookingId = bookingId,

            Message =
                $"Your booking for {purpose} has been approved by the admin.",

            // New notification should be unread
            IsRead = false,

            CreatedAt = DateTime.UtcNow
        };

        // Add notification to database
        await _notificationRepository
            .AddAsync(employeeNotification);

        // Save notification
        await _notificationRepository
            .SaveChangesAsync();
    }

    // =========================================================
    // REJECT BOOKING
    // =========================================================

    public async Task RejectAsync(
        int bookingId)
    {
        // Check whether booking exists
        if (!await _bookingRepository.ExistsAsync(bookingId))
        {
            throw new Exception("Booking not found.");
        }

        // Get booking details BEFORE changing the status.
        var booking = await _bookingRepository
            .GetByIdAsync(bookingId);

        if (booking == null)
        {
            throw new Exception("Booking not found.");
        }

        // Change booking status to Rejected
        await _bookingRepository
            .RejectAsync(bookingId);

        // =====================================================
        // Create Employee Notification
        // =====================================================

        var purpose =
            booking.Purpose ??
            booking.MeetingTitle ??
            "Workspace";

        var employeeNotification = new Notification
        {
            EmployeeId = booking.EmployeeId,

            // Link notification to booking
            BookingId = bookingId,

            Message =
                $"Your booking for {purpose} has been rejected by the admin.",

            // New notification should be unread
            IsRead = false,

            CreatedAt = DateTime.UtcNow
        };

        // Add notification to database
        await _notificationRepository
            .AddAsync(employeeNotification);

        // Save notification
        await _notificationRepository
            .SaveChangesAsync();
    }

    // =========================================================
    // DELETE BOOKING
    // =========================================================

    public async Task DeleteAsync(
        int bookingId)
    {
        if (!await _bookingRepository.ExistsAsync(bookingId))
        {
            throw new Exception("Booking not found.");
        }

        await _bookingRepository
            .DeleteAsync(bookingId);
    }
}