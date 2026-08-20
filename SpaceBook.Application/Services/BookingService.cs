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
        return await _bookingRepository.GetDashboardAsync();
    }

    // =========================================================
    // GET ALL BOOKINGS
    // =========================================================

    public async Task<IEnumerable<BookingDto>> GetAllAsync(
        BookingFilterDto filter)
    {
        return await _bookingRepository.GetAllAsync(filter);
    }

    // =========================================================
    // GET BOOKING BY ID
    // =========================================================

    public async Task<BookingDetailsDto?> GetByIdAsync(
        int bookingId)
    {
        return await _bookingRepository.GetByIdAsync(bookingId);
    }

    // =========================================================
    // APPROVE BOOKING
    // =========================================================

    public async Task ApproveAsync(int bookingId)
    {
        // -----------------------------------------------------
        // 1. CHECK WHETHER BOOKING EXISTS
        // -----------------------------------------------------

        var exists =
            await _bookingRepository.ExistsAsync(bookingId);

        if (!exists)
        {
            throw new KeyNotFoundException(
                "Booking not found.");
        }

        // -----------------------------------------------------
        // 2. GET BOOKING DETAILS BEFORE STATUS UPDATE
        // -----------------------------------------------------
        // We need EmployeeId, Purpose and MeetingTitle
        // for creating the employee notification.
        // -----------------------------------------------------

        var booking =
            await _bookingRepository.GetByIdAsync(bookingId);

        if (booking == null)
        {
            throw new KeyNotFoundException(
                "Booking not found.");
        }

        // -----------------------------------------------------
        // 3. APPROVE BOOKING
        // -----------------------------------------------------
        // This is the MAIN operation.
        // If this succeeds, the booking is approved.
        // -----------------------------------------------------

        await _bookingRepository.ApproveAsync(bookingId);

        // -----------------------------------------------------
        // 4. CREATE EMPLOYEE NOTIFICATION
        // -----------------------------------------------------
        // Notification is a SECONDARY operation.
        //
        // If notification insertion fails, the booking should
        // still remain approved.
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

            // -------------------------------------------------
            // Database column:
            //
            // message varchar(500)
            //
            // Therefore make sure the application never sends
            // more than 500 characters.
            // -------------------------------------------------

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
        catch (Exception ex)
        {
            // -------------------------------------------------
            // IMPORTANT
            // -------------------------------------------------
            // Do NOT fail the approval because notification
            // persistence failed.
            //
            // The booking has already been approved.
            //
            // Log the actual exception so that we can diagnose
            // notification problems from Render logs.
            // -------------------------------------------------

            Console.WriteLine(
                $"[BookingService] Notification creation failed " +
                $"for APPROVED booking {bookingId}.");

            Console.WriteLine(
                $"[BookingService] Exception: {ex}");
        }
    }

    // =========================================================
    // REJECT BOOKING
    // =========================================================

    public async Task RejectAsync(int bookingId)
    {
        // -----------------------------------------------------
        // 1. CHECK WHETHER BOOKING EXISTS
        // -----------------------------------------------------

        var exists =
            await _bookingRepository.ExistsAsync(bookingId);

        if (!exists)
        {
            throw new KeyNotFoundException(
                "Booking not found.");
        }

        // -----------------------------------------------------
        // 2. GET BOOKING DETAILS BEFORE STATUS UPDATE
        // -----------------------------------------------------

        var booking =
            await _bookingRepository.GetByIdAsync(bookingId);

        if (booking == null)
        {
            throw new KeyNotFoundException(
                "Booking not found.");
        }

        // -----------------------------------------------------
        // 3. REJECT BOOKING
        // -----------------------------------------------------

        await _bookingRepository.RejectAsync(bookingId);

        // -----------------------------------------------------
        // 4. CREATE EMPLOYEE NOTIFICATION
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

            // -------------------------------------------------
            // Database column:
            //
            // message varchar(500)
            // -------------------------------------------------

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
        catch (Exception ex)
        {
            // -------------------------------------------------
            // IMPORTANT
            // -------------------------------------------------
            // Do NOT fail the rejection because notification
            // persistence failed.
            //
            // The booking has already been rejected.
            // -------------------------------------------------

            Console.WriteLine(
                $"[BookingService] Notification creation failed " +
                $"for REJECTED booking {bookingId}.");

            Console.WriteLine(
                $"[BookingService] Exception: {ex}");
        }
    }

    // =========================================================
    // DELETE BOOKING
    // =========================================================

    public async Task DeleteAsync(int bookingId)
    {
        // -----------------------------------------------------
        // 1. CHECK WHETHER BOOKING EXISTS
        // -----------------------------------------------------

        var exists =
            await _bookingRepository.ExistsAsync(bookingId);

        if (!exists)
        {
            throw new KeyNotFoundException(
                "Booking not found.");
        }

        // -----------------------------------------------------
        // 2. DELETE BOOKING
        // -----------------------------------------------------

        await _bookingRepository.DeleteAsync(bookingId);
    }
}