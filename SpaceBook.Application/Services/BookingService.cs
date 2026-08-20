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
    // DASHBOARD
    // =========================================================

    public async Task<BookingDashboardDto> GetDashboardAsync()
    {
        return await _bookingRepository
            .GetDashboardAsync();
    }

    // =========================================================
    // GET ALL BOOKINGS
    // =========================================================

    public async Task<IEnumerable<BookingDto>> GetAllAsync(
        BookingFilterDto filter)
    {
        return await _bookingRepository
            .GetAllAsync(filter);
    }

    // =========================================================
    // GET BOOKING BY ID
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
        // -----------------------------------------------------
        // Check booking exists
        // -----------------------------------------------------

        if (!await _bookingRepository.ExistsAsync(bookingId))
        {
            throw new KeyNotFoundException(
                "Booking not found.");
        }

        // -----------------------------------------------------
        // Get booking BEFORE changing status.
        //
        // EmployeeId is required for employee notification.
        // -----------------------------------------------------

        var booking =
            await _bookingRepository
                .GetByIdAsync(bookingId);

        if (booking == null)
        {
            throw new KeyNotFoundException(
                "Booking not found.");
        }

        // -----------------------------------------------------
        // Approve booking
        // -----------------------------------------------------

        await _bookingRepository
            .ApproveAsync(bookingId);

        // -----------------------------------------------------
        // Determine notification purpose
        // -----------------------------------------------------

        var purpose =
            !string.IsNullOrWhiteSpace(booking.Purpose)
                ? booking.Purpose
                : !string.IsNullOrWhiteSpace(booking.MeetingTitle)
                    ? booking.MeetingTitle
                    : "Workspace";

        // -----------------------------------------------------
        // Create notification message
        // -----------------------------------------------------

        var message =
            $"Your booking for {purpose} has been approved by the admin.";

        // -----------------------------------------------------
        // IMPORTANT:
        // Existing PostgreSQL column supports 500 characters.
        //
        // Do NOT alter the database.
        // Keep notification safely within 500 characters.
        // -----------------------------------------------------

        if (message.Length > 500)
        {
            message = message[..500];
        }

        // -----------------------------------------------------
        // Create employee notification
        // -----------------------------------------------------

        var employeeNotification = new Notification
        {
            EmployeeId =
                booking.EmployeeId,

            BookingId =
                bookingId,

            Message =
                message,

            IsRead =
                false,

            CreatedAt =
                DateTime.UtcNow
        };

        // -----------------------------------------------------
        // Save notification
        // -----------------------------------------------------

        await _notificationRepository
            .AddAsync(employeeNotification);

        await _notificationRepository
            .SaveChangesAsync();
    }

    // =========================================================
    // REJECT BOOKING
    // =========================================================

    public async Task RejectAsync(
        int bookingId)
    {
        // -----------------------------------------------------
        // Check booking exists
        // -----------------------------------------------------

        if (!await _bookingRepository.ExistsAsync(bookingId))
        {
            throw new KeyNotFoundException(
                "Booking not found.");
        }

        // -----------------------------------------------------
        // Get booking BEFORE changing status.
        // -----------------------------------------------------

        var booking =
            await _bookingRepository
                .GetByIdAsync(bookingId);

        if (booking == null)
        {
            throw new KeyNotFoundException(
                "Booking not found.");
        }

        // -----------------------------------------------------
        // Reject booking
        // -----------------------------------------------------

        await _bookingRepository
            .RejectAsync(bookingId);

        // -----------------------------------------------------
        // Determine notification purpose
        // -----------------------------------------------------

        var purpose =
            !string.IsNullOrWhiteSpace(booking.Purpose)
                ? booking.Purpose
                : !string.IsNullOrWhiteSpace(booking.MeetingTitle)
                    ? booking.MeetingTitle
                    : "Workspace";

        // -----------------------------------------------------
        // Create notification message
        // -----------------------------------------------------

        var message =
            $"Your booking for {purpose} has been rejected by the admin.";

        // -----------------------------------------------------
        // IMPORTANT:
        // Keep notification within existing DB limit.
        // -----------------------------------------------------

        if (message.Length > 500)
        {
            message = message[..500];
        }

        // -----------------------------------------------------
        // Create employee notification
        // -----------------------------------------------------

        var employeeNotification = new Notification
        {
            EmployeeId =
                booking.EmployeeId,

            BookingId =
                bookingId,

            Message =
                message,

            IsRead =
                false,

            CreatedAt =
                DateTime.UtcNow
        };

        // -----------------------------------------------------
        // Save notification
        // -----------------------------------------------------

        await _notificationRepository
            .AddAsync(employeeNotification);

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
            throw new KeyNotFoundException(
                "Booking not found.");
        }

        await _bookingRepository
            .DeleteAsync(bookingId);
    }
}