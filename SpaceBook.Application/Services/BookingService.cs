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
    // This method is retained for:
    // - Existing Pending bookings
    // - Manual admin approval if required
    //
    // New employee bookings should now be created as
    // "Approved" directly from EmployeeBookingService.
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

        var booking =
            await _bookingRepository.GetByIdAsync(bookingId);

        if (booking == null)
        {
            throw new KeyNotFoundException(
                "Booking not found.");
        }

        // -----------------------------------------------------
        // 3. VALIDATE CURRENT STATUS
        // -----------------------------------------------------
        // Only Pending bookings are eligible for manual approval.

        if (!string.Equals(
                booking.Status,
                "Pending",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Booking cannot be approved because its current status is {booking.Status}.");
        }

        // -----------------------------------------------------
        // 4. APPROVE BOOKING
        // -----------------------------------------------------

        await _bookingRepository.ApproveAsync(bookingId);

        // -----------------------------------------------------
        // 5. CREATE EMPLOYEE NOTIFICATION
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

            // Database column:
            // message varchar(500)

            if (message.Length > 500)
            {
                message = message[..500];
            }

            // -------------------------------------------------
            // PostgreSQL column:
            // timestamp without time zone
            //
            // Therefore DateTime must be Kind = Unspecified.
            // -------------------------------------------------

            var createdAt = DateTime.SpecifyKind(
                DateTime.UtcNow,
                DateTimeKind.Unspecified
            );

            var employeeNotification = new Notification
            {
                EmployeeId = booking.EmployeeId,

                BookingId = bookingId,

                Message = message,

                IsRead = false,

                CreatedAt = createdAt
            };

            await _notificationRepository
                .AddAsync(employeeNotification);

            await _notificationRepository
                .SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // -------------------------------------------------
            // Notification failure must not fail approval
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
    // This method is retained for existing Pending bookings.
    //
    // New employee bookings are auto-approved, so normally
    // new bookings will not reach this method.
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
        // 3. VALIDATE CURRENT STATUS
        // -----------------------------------------------------
        // Only Pending bookings are eligible for rejection.

        if (!string.Equals(
                booking.Status,
                "Pending",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Booking cannot be rejected because its current status is {booking.Status}.");
        }

        // -----------------------------------------------------
        // 4. REJECT BOOKING
        // -----------------------------------------------------

        await _bookingRepository.RejectAsync(bookingId);

        // -----------------------------------------------------
        // 5. CREATE EMPLOYEE NOTIFICATION
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

            // Database column:
            // message varchar(500)

            if (message.Length > 500)
            {
                message = message[..500];
            }

            // -------------------------------------------------
            // PostgreSQL column:
            // timestamp without time zone
            //
            // Therefore DateTime must be Kind = Unspecified.
            // -------------------------------------------------

            var createdAt = DateTime.SpecifyKind(
                DateTime.UtcNow,
                DateTimeKind.Unspecified
            );

            var employeeNotification = new Notification
            {
                EmployeeId = booking.EmployeeId,

                BookingId = bookingId,

                Message = message,

                IsRead = false,

                CreatedAt = createdAt
            };

            await _notificationRepository
                .AddAsync(employeeNotification);

            await _notificationRepository
                .SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // -------------------------------------------------
            // Notification failure must not fail rejection
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