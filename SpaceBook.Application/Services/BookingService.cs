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

    public async Task ApproveAsync(int bookingId)
    {
        // -----------------------------------------------------
        // 1. Check whether booking exists
        // -----------------------------------------------------

        var exists =
            await _bookingRepository.ExistsAsync(bookingId);

        if (!exists)
        {
            throw new Exception("Booking not found.");
        }

        // -----------------------------------------------------
        // 2. Get booking details BEFORE updating status
        // -----------------------------------------------------
        // We need EmployeeId, Purpose and MeetingTitle
        // for the employee notification.
        // -----------------------------------------------------

        var booking =
            await _bookingRepository.GetByIdAsync(bookingId);

        if (booking == null)
        {
            throw new Exception("Booking not found.");
        }

        // -----------------------------------------------------
        // 3. APPROVE THE BOOKING
        // -----------------------------------------------------
        // This is the primary operation.
        // If this succeeds, the booking is approved.
        // -----------------------------------------------------

        await _bookingRepository
            .ApproveAsync(bookingId);

        // -----------------------------------------------------
        // 4. CREATE EMPLOYEE NOTIFICATION
        // -----------------------------------------------------
        // Notification is a secondary operation.
        //
        // We do NOT allow a notification database problem
        // to turn a successful booking approval into HTTP 500.
        // -----------------------------------------------------

        try
        {
            var purpose =
                !string.IsNullOrWhiteSpace(booking.Purpose)
                    ? booking.Purpose
                    : !string.IsNullOrWhiteSpace(booking.MeetingTitle)
                        ? booking.MeetingTitle
                        : "Workspace";

            var message =
                $"Your booking for {purpose} has been approved by the admin.";

            // Keep the message safely bounded in application code.
            // This does NOT alter the database.
            if (message.Length > 500)
            {
                message = message[..500];
            }

            var employeeNotification = new Notification
            {
                EmployeeId = booking.EmployeeId,

                BookingId = bookingId,

                Message = message,

                IsRead = false,

                CreatedAt = DateTime.UtcNow
            };

            await _notificationRepository
                .AddAsync(employeeNotification);

            await _notificationRepository
                .SaveChangesAsync();
        }
        catch
        {
            // -------------------------------------------------
            // IMPORTANT
            // Do not throw the notification error here.
            //
            // The booking has already been successfully
            // approved. A notification persistence problem
            // must not cause PATCH /approve to return 500.
            // -------------------------------------------------
        }
    }

    // =========================================================
    // REJECT BOOKING
    // =========================================================

    public async Task RejectAsync(int bookingId)
    {
        var exists =
            await _bookingRepository.ExistsAsync(bookingId);

        if (!exists)
        {
            throw new Exception("Booking not found.");
        }

        var booking =
            await _bookingRepository.GetByIdAsync(bookingId);

        if (booking == null)
        {
            throw new Exception("Booking not found.");
        }

        // -----------------------------------------------------
        // REJECT BOOKING
        // -----------------------------------------------------

        await _bookingRepository
            .RejectAsync(bookingId);

        // -----------------------------------------------------
        // CREATE EMPLOYEE NOTIFICATION
        // -----------------------------------------------------

        try
        {
            var purpose =
                !string.IsNullOrWhiteSpace(booking.Purpose)
                    ? booking.Purpose
                    : !string.IsNullOrWhiteSpace(booking.MeetingTitle)
                        ? booking.MeetingTitle
                        : "Workspace";

            var message =
                $"Your booking for {purpose} has been rejected by the admin.";

            if (message.Length > 500)
            {
                message = message[..500];
            }

            var employeeNotification = new Notification
            {
                EmployeeId = booking.EmployeeId,

                BookingId = bookingId,

                Message = message,

                IsRead = false,

                CreatedAt = DateTime.UtcNow
            };

            await _notificationRepository
                .AddAsync(employeeNotification);

            await _notificationRepository
                .SaveChangesAsync();
        }
        catch
        {
            // Notification failure should not make rejection fail.
        }
    }

    // =========================================================
    // DELETE BOOKING
    // =========================================================

    public async Task DeleteAsync(int bookingId)
    {
        var exists =
            await _bookingRepository.ExistsAsync(bookingId);

        if (!exists)
        {
            throw new Exception("Booking not found.");
        }

        await _bookingRepository
            .DeleteAsync(bookingId);
    }
}