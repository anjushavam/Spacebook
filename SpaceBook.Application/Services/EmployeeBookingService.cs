using Microsoft.Extensions.Logging;
using SpaceBook.Application.DTOs.Booking;
using SpaceBook.Application.Interfaces;
using SpaceBook.Domain.Entities;

namespace SpaceBook.Application.Services;

public class EmployeeBookingService : IEmployeeBookingService
{
    private readonly IEmployeeBookingRepository _bookingRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IEmailService _emailService;
    private readonly ILogger<EmployeeBookingService> _logger;

    // =========================================================
    // OFFICE HOURS
    // =========================================================
    // Booking/Search Hours:
    // 10:00 AM to 10:00 PM
    // =========================================================

    private static readonly TimeOnly OfficeStartTime =
        new TimeOnly(10, 0);

    private static readonly TimeOnly OfficeEndTime =
        new TimeOnly(22, 0);

    public EmployeeBookingService(
        IEmployeeBookingRepository bookingRepository,
        INotificationRepository notificationRepository,
        IEmailService emailService,
        ILogger<EmployeeBookingService> logger)
    {
        _bookingRepository = bookingRepository;
        _notificationRepository = notificationRepository;
        _emailService = emailService;
        _logger = logger;
    }

    // =========================================================
    // DATABASE DATETIME
    // =========================================================

    private static DateTime GetDatabaseDateTime()
    {
        return DateTime.UtcNow;
    }

    // =========================================================
    // CHECK WEEKEND
    // =========================================================

    private static bool IsWeekend(DateOnly date)
    {
        return date.DayOfWeek == DayOfWeek.Saturday ||
               date.DayOfWeek == DayOfWeek.Sunday;
    }

    // =========================================================
    // VALIDATE WEEKDAY
    // =========================================================

    private static void ValidateWeekday(DateOnly date)
    {
        if (IsWeekend(date))
        {
            throw new Exception(
                "Bookings and room availability are not allowed on Saturdays and Sundays.");
        }
    }

    // =========================================================
    // CREATE BOOKING
    // =========================================================

    public async Task<int> CreateBookingAsync(
        int employeeId,
        CreateBookingRequestDto request)
    {
        // -----------------------------------------------------
        // VALIDATE EMPLOYEE
        // -----------------------------------------------------

        if (employeeId <= 0)
        {
            throw new Exception(
                "Invalid employee.");
        }

        // -----------------------------------------------------
        // VALIDATE REQUEST
        // -----------------------------------------------------

        if (request == null)
        {
            throw new Exception(
                "Booking request is required.");
        }

        // -----------------------------------------------------
        // VALIDATE DATE
        // -----------------------------------------------------

        ValidateWeekday(request.BookingDate);

        var now = DateTime.Now;

        var today =
            DateOnly.FromDateTime(now);

        var currentTime =
            TimeOnly.FromDateTime(now);

        if (request.BookingDate < today)
        {
            throw new Exception(
                "Bookings cannot be created for a past date.");
        }

        if (request.BookingDate == today &&
            request.StartTime <= currentTime)
        {
            throw new Exception(
                "Bookings cannot start at or before the current time.");
        }

        // -----------------------------------------------------
        // VALIDATE TIME ORDER
        // -----------------------------------------------------

        if (request.StartTime >= request.EndTime)
        {
            throw new Exception(
                "End time must be after start time.");
        }

        // -----------------------------------------------------
        // VALIDATE OFFICE HOURS
        // -----------------------------------------------------

        if (request.StartTime < OfficeStartTime)
        {
            throw new Exception(
                "Bookings can only start from 10:00 AM.");
        }

        if (request.EndTime > OfficeEndTime)
        {
            throw new Exception(
                "Bookings must end by 10:00 PM.");
        }

        // -----------------------------------------------------
        // VALIDATE PARTICIPANT COUNT
        // -----------------------------------------------------

        if (request.ParticipantCount <= 0)
        {
            throw new Exception(
                "Participant count must be at least 1.");
        }

        // -----------------------------------------------------
        // VALIDATE ROOM ID
        // -----------------------------------------------------

        if (request.RoomId <= 0)
        {
            throw new Exception(
                "Room ID is required.");
        }

        // -----------------------------------------------------
        // VALIDATE ROOM CAPACITY
        // -----------------------------------------------------

        var roomCapacity =
            await _bookingRepository.GetRoomCapacityAsync(
                request.RoomId);

        if (roomCapacity == null)
        {
            throw new Exception(
                "Selected room is not available.");
        }

        if (request.ParticipantCount >
            roomCapacity.Value)
        {
            throw new Exception(
                $"The selected room can accommodate a maximum of {roomCapacity.Value} participants.");
        }

        // -----------------------------------------------------
        // CHECK ROOM AVAILABILITY
        // -----------------------------------------------------

        var isAvailable =
            await _bookingRepository.IsRoomAvailableAsync(
                request.RoomId,
                request.BookingDate,
                request.StartTime,
                request.EndTime);

        if (!isAvailable)
        {
            throw new Exception(
                "Room is already booked for the selected time.");
        }

        // -----------------------------------------------------
        // RESOLVE MEETING TITLE
        // -----------------------------------------------------

        var resolvedTitle =
            !string.IsNullOrWhiteSpace(request.MeetingTitle)
                ? request.MeetingTitle.Trim()
                : "Reserved Workspace";

        // -----------------------------------------------------
        // RESOLVE PURPOSE
        // -----------------------------------------------------

        var resolvedPurpose =
            !string.IsNullOrWhiteSpace(request.Purpose)
                ? request.Purpose.Trim()
                : resolvedTitle;

        // =====================================================
        // CREATE BOOKING ENTITY
        // =====================================================
        //
        // IMPORTANT:
        // New employee bookings are now AUTO-APPROVED.
        //
        // Previously:
        //
        // Status = "Pending"
        //
        // Now:
        //
        // Status = "Approved"
        //
        // =====================================================

        var booking = new Booking
        {
            RoomId =
                request.RoomId,

            EmployeeId =
                employeeId,

            MeetingTitle =
                resolvedTitle,

            Purpose =
                resolvedPurpose,

            ParticipantCount =
                request.ParticipantCount,

            BookingDate =
                request.BookingDate,

            StartTime =
                request.StartTime,

            EndTime =
                request.EndTime,

            BookedOn =
                GetDatabaseDateTime(),

            Status =
                "Approved"
        };

        try
        {
            // -------------------------------------------------
            // SAVE BOOKING
            // -------------------------------------------------

            await _bookingRepository.CreateBookingAsync(
                booking);

            await _bookingRepository.SaveChangesAsync();

            // -------------------------------------------------
            // CREATE EMPLOYEE NOTIFICATION
            // -------------------------------------------------
            //
            // Since the booking is automatically approved,
            // tell the employee that it is approved.
            // -------------------------------------------------

            var notification = new Notification
            {
                EmployeeId =
                    employeeId,

                BookingId =
                    booking.BookingId,

                Message =
                    $"Your booking for {resolvedTitle} has been automatically approved.",

                IsRead =
                    false,

                CreatedAt =
                    GetDatabaseDateTime()
            };

            await _notificationRepository.AddAsync(
                notification);

            await _notificationRepository.SaveChangesAsync();

            // -------------------------------------------------
            // SEND CONFIRMATION & ADMIN ALERT EMAILS
            // -------------------------------------------------
            _ = Task.Run(async () =>
            {
                try
                {
                    await SendBookingConfirmationAndAdminAlertEmailsAsync(booking, employeeId, resolvedTitle, resolvedPurpose);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Background email dispatch failed for booking ID {BookingId}", booking.BookingId);
                }
            });

            return booking.BookingId;
        }
        catch (Exception ex)
        {
            throw new Exception(
                ex.InnerException?.Message ?? ex.Message,
                ex);
        }
    }

    // =========================================================
    // GET BOOKING DETAILS
    // =========================================================

    public async Task<BookingDetailsDto?> GetBookingByIdAsync(
        int bookingId,
        int employeeId)
    {
        if (bookingId <= 0)
        {
            throw new Exception(
                "Invalid booking ID.");
        }

        if (employeeId <= 0)
        {
            throw new Exception(
                "Invalid employee.");
        }

        return await _bookingRepository.GetBookingByIdAsync(
            bookingId,
            employeeId);
    }

    // =========================================================
    // CANCEL BOOKING
    // =========================================================

    public async Task<bool> CancelBookingAsync(
        int bookingId,
        int employeeId,
        string reason)
    {
        // -----------------------------------------------------
        // VALIDATE BOOKING ID
        // -----------------------------------------------------

        if (bookingId <= 0)
        {
            throw new Exception(
                "Invalid booking ID.");
        }

        // -----------------------------------------------------
        // VALIDATE EMPLOYEE
        // -----------------------------------------------------

        if (employeeId <= 0)
        {
            throw new Exception(
                "Invalid employee.");
        }

        // -----------------------------------------------------
        // VALIDATE CANCELLATION REASON
        // -----------------------------------------------------

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new Exception(
                "Cancellation reason is required.");
        }

        reason = reason.Trim();

        // -----------------------------------------------------
        // GET EMPLOYEE NAME
        // -----------------------------------------------------

        var employeeName =
            await _bookingRepository.GetEmployeeNameAsync(
                employeeId);

        if (string.IsNullOrWhiteSpace(employeeName))
        {
            employeeName = "Unknown user";
        }

        // -----------------------------------------------------
        // CANCEL BOOKING
        // -----------------------------------------------------

        var result =
            await _bookingRepository.CancelBookingAsync(
                bookingId,
                employeeId,
                reason);

        if (!result)
        {
            return false;
        }

        // -----------------------------------------------------
        // CREATE ADMIN NOTIFICATION
        // -----------------------------------------------------

        var notification = new Notification
        {
            EmployeeId =
                employeeId,

            BookingId =
                bookingId,

            Message =
                $"Booking was cancelled by {employeeName}. Reason: {reason}",

            IsRead =
                false,

            CreatedAt =
                GetDatabaseDateTime()
        };

        await _notificationRepository.AddAsync(
            notification);

        await _notificationRepository.SaveChangesAsync();

        // -----------------------------------------------------
        // SEND CANCELLATION EMAILS
        // -----------------------------------------------------
        _ = Task.Run(async () =>
        {
            try
            {
                await SendBookingCancellationEmailsAsync(bookingId, employeeId, employeeName, reason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background cancellation email dispatch failed for booking ID {BookingId}", bookingId);
            }
        });

        return true;
    }

    // =========================================================
    // UPDATE / RESCHEDULE BOOKING
    // =========================================================

    public async Task<bool> UpdateBookingAsync(
        int bookingId,
        int employeeId,
        UpdateBookingRequestDto request)
    {
        // -----------------------------------------------------
        // VALIDATE IDs
        // -----------------------------------------------------

        if (bookingId <= 0)
        {
            throw new Exception(
                "Invalid booking ID.");
        }

        if (employeeId <= 0)
        {
            throw new Exception(
                "Invalid employee.");
        }

        // -----------------------------------------------------
        // VALIDATE REQUEST
        // -----------------------------------------------------

        if (request == null)
        {
            throw new Exception(
                "Update booking request is required.");
        }

        // -----------------------------------------------------
        // VALIDATE DATE
        // -----------------------------------------------------

        ValidateWeekday(request.BookingDate);

        var now = DateTime.Now;

        var today =
            DateOnly.FromDateTime(now);

        var currentTime =
            TimeOnly.FromDateTime(now);

        if (request.BookingDate < today)
        {
            throw new Exception(
                "Booking cannot be rescheduled to a past date.");
        }

        if (request.BookingDate == today &&
            request.StartTime <= currentTime)
        {
            throw new Exception(
                "Booking cannot be rescheduled to a time that has already passed.");
        }

        // -----------------------------------------------------
        // VALIDATE TIME
        // -----------------------------------------------------

        if (request.StartTime >= request.EndTime)
        {
            throw new Exception(
                "End time must be after start time.");
        }

        // -----------------------------------------------------
        // VALIDATE OFFICE HOURS
        // -----------------------------------------------------

        if (request.StartTime < OfficeStartTime)
        {
            throw new Exception(
                "Bookings can only start from 10:00 AM.");
        }

        if (request.EndTime > OfficeEndTime)
        {
            throw new Exception(
                "Bookings must end by 10:00 PM.");
        }

        // -----------------------------------------------------
        // VALIDATE PARTICIPANT COUNT
        // -----------------------------------------------------

        if (request.ParticipantCount <= 0)
        {
            throw new Exception(
                "Participant count must be at least 1.");
        }

        // -----------------------------------------------------
        // VALIDATE ROOM ID
        // -----------------------------------------------------

        if (!request.RoomId.HasValue ||
            request.RoomId.Value <= 0)
        {
            throw new Exception(
                "Room ID is required.");
        }

        // -----------------------------------------------------
        // GET EXISTING BOOKING
        // -----------------------------------------------------

        var existingBooking =
            await _bookingRepository.GetBookingByIdAsync(
                bookingId,
                employeeId);

        if (existingBooking == null)
        {
            throw new Exception(
                "Booking not found.");
        }

        // -----------------------------------------------------
        // PREVENT RESCHEDULE OF CANCELLED BOOKING
        // -----------------------------------------------------

        if (string.Equals(
                existingBooking.Status,
                "Cancelled",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception(
                "Cancelled bookings cannot be rescheduled.");
        }

        // -----------------------------------------------------
        // PREVENT RESCHEDULE OF REJECTED BOOKING
        // -----------------------------------------------------

        if (string.Equals(
                existingBooking.Status,
                "Rejected",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception(
                "Rejected bookings cannot be rescheduled.");
        }

        // -----------------------------------------------------
        // CHECK RESCHEDULE RESTRICTION
        // -----------------------------------------------------

        var bookingStartDateTime =
            existingBooking.BookingDate.ToDateTime(
                existingBooking.StartTime);

        if (DateTime.Now >=
            bookingStartDateTime.AddHours(-1))
        {
            throw new Exception(
                "Booking cannot be rescheduled within 1 hour before start time.");
        }

        // -----------------------------------------------------
        // VALIDATE ROOM CAPACITY
        // -----------------------------------------------------

        var roomCapacity =
            await _bookingRepository.GetRoomCapacityAsync(
                request.RoomId.Value);

        if (roomCapacity == null)
        {
            throw new Exception(
                "Selected room is not available.");
        }

        if (request.ParticipantCount >
            roomCapacity.Value)
        {
            throw new Exception(
                $"The selected room can accommodate a maximum of {roomCapacity.Value} participants.");
        }

        // -----------------------------------------------------
        // CHECK ROOM AVAILABILITY
        // EXCLUDE CURRENT BOOKING
        // -----------------------------------------------------

        var isAvailable =
            await _bookingRepository.IsRoomAvailableAsync(
                request.RoomId.Value,
                request.BookingDate,
                request.StartTime,
                request.EndTime,
                bookingId);

        if (!isAvailable)
        {
            throw new Exception(
                "Room is already booked for the selected time.");
        }

        // -----------------------------------------------------
        // UPDATE BOOKING
        // -----------------------------------------------------

        var updated =
            await _bookingRepository.UpdateBookingAsync(
                bookingId,
                employeeId,
                request);

        if (!updated)
        {
            return false;
        }

        // -----------------------------------------------------
        // GET EMPLOYEE NAME
        // -----------------------------------------------------

        var employeeName =
            await _bookingRepository.GetEmployeeNameAsync(
                employeeId);

        if (string.IsNullOrWhiteSpace(employeeName))
        {
            employeeName = "Unknown user";
        }

        // -----------------------------------------------------
        // CREATE ADMIN NOTIFICATION
        // -----------------------------------------------------
        //
        // NOTE:
        // The repository currently sets updated bookings
        // to Pending. Therefore the notification correctly
        // says that rescheduling requires approval.
        //
        // If you also want RESCHEDULES to be auto-approved,
        // the repository method UpdateBookingAsync must be
        // changed from Pending to Approved.
        // -----------------------------------------------------

        var notification = new Notification
        {
            EmployeeId =
                employeeId,

            BookingId =
                bookingId,

            Message =
                $"Booking was rescheduled by {employeeName} and has been approved.",

            IsRead =
                false,

            CreatedAt =
                GetDatabaseDateTime()
        };

        await _notificationRepository.AddAsync(
            notification);

        await _notificationRepository.SaveChangesAsync();

        // -----------------------------------------------------
        // SEND RESCHEDULE EMAILS
        // -----------------------------------------------------
        _ = Task.Run(async () =>
        {
            try
            {
                await SendBookingRescheduleEmailsAsync(bookingId, employeeId, employeeName, request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background reschedule email dispatch failed for booking ID {BookingId}", bookingId);
            }
        });

        return true;
    }

    // =========================================================
    // SEARCH AVAILABLE ROOMS
    // =========================================================

    public async Task<List<AvailableRoomDto>>
        SearchAvailableRoomsAsync(
            SearchRoomsRequestDto request)
    {
        if (request == null)
        {
            throw new Exception(
                "Search request is required.");
        }

        var hasModule =
            !string.IsNullOrWhiteSpace(request.Module);

        var hasRoomType =
            request.RoomTypeId.HasValue &&
            request.RoomTypeId.Value > 0;

        var hasParticipantCount =
            request.ParticipantCount.HasValue &&
            request.ParticipantCount.Value > 0;

        var hasBookingDate =
            request.BookingDate.HasValue;

        var hasStartTime =
            request.StartTime.HasValue;

        var hasEndTime =
            request.EndTime.HasValue;

        var hasFacilities =
            request.FacilityIds != null &&
            request.FacilityIds.Any(id => id > 0);

        if (!hasModule &&
            !hasRoomType &&
            !hasParticipantCount &&
            !hasBookingDate &&
            !hasStartTime &&
            !hasEndTime &&
            !hasFacilities)
        {
            throw new Exception(
                "Please provide at least one search criterion.");
        }

        if (hasStartTime != hasEndTime)
        {
            throw new Exception(
                "Both start time and end time are required when searching by time.");
        }

        // -----------------------------------------------------
        // VALIDATE DATE
        // -----------------------------------------------------

        if (hasBookingDate)
        {
            var bookingDate =
                request.BookingDate!.Value;

            ValidateWeekday(bookingDate);

            var today =
                DateOnly.FromDateTime(DateTime.Now);

            if (bookingDate < today)
            {
                throw new Exception(
                    "Cannot search availability for a past date.");
            }
        }

        // -----------------------------------------------------
        // VALIDATE TIME
        // -----------------------------------------------------

        if (hasStartTime && hasEndTime)
        {
            var startTime =
                request.StartTime!.Value;

            var endTime =
                request.EndTime!.Value;

            if (startTime >= endTime)
            {
                throw new Exception(
                    "End time must be after start time.");
            }

            if (startTime < OfficeStartTime)
            {
                throw new Exception(
                    "Rooms can only be searched from 10:00 AM.");
            }

            if (endTime > OfficeEndTime)
            {
                throw new Exception(
                    "Rooms can only be searched until 10:00 PM.");
            }

            if (hasBookingDate &&
                request.BookingDate!.Value ==
                DateOnly.FromDateTime(DateTime.Now))
            {
                var currentSearchTime =
                    TimeOnly.FromDateTime(DateTime.Now);

                if (startTime <= currentSearchTime)
                {
                    throw new Exception(
                        "Cannot search for a time that has already passed.");
                }
            }
        }

        // -----------------------------------------------------
        // PARTICIPANT COUNT VALIDATION
        // -----------------------------------------------------

        if (request.ParticipantCount.HasValue &&
            request.ParticipantCount.Value <= 0)
        {
            throw new Exception(
                "Participant count must be greater than zero.");
        }

        // -----------------------------------------------------
        // SEARCH ROOMS
        // -----------------------------------------------------

        return await _bookingRepository
            .SearchAvailableRoomsAsync(request);
    }

    // =========================================================
    // GET ROOMS BY MODULE
    // =========================================================

    public async Task<List<AvailableRoomDto>>
        GetRoomsByModuleAsync(
            string module)
    {
        if (string.IsNullOrWhiteSpace(module))
        {
            throw new Exception(
                "Module is required.");
        }

        return await _bookingRepository
            .GetRoomsByModuleAsync(
                module.Trim());
    }

    // =========================================================
    // EMAIL DISPATCH HELPERS
    // =========================================================

    private async Task SendBookingConfirmationAndAdminAlertEmailsAsync(
        Booking booking,
        int employeeId,
        string meetingTitle,
        string purpose)
    {
        var employee = await _bookingRepository.GetEmployeeByIdAsync(employeeId);
        var room = await _bookingRepository.GetRoomByIdAsync(booking.RoomId);

        var employeeName = employee?.Name ?? "Colleague";
        var roomName = room != null
            ? (!string.IsNullOrWhiteSpace(room.RoomName) ? room.RoomName : room.RoomNumber)
            : "Meeting Room";

        // 1. Employee Confirmation Email
        if (employee != null && !string.IsNullOrWhiteSpace(employee.Email))
        {
            var empSubject = $"Booking Confirmed: '{meetingTitle}' in {roomName}";
            var empHtml = BuildBookingConfirmedEmailHtml(
                employeeName,
                meetingTitle,
                purpose,
                roomName,
                room?.RoomNumber ?? string.Empty,
                booking.BookingDate,
                booking.StartTime,
                booking.EndTime,
                booking.ParticipantCount);

            await _emailService.SendEmailAsync(employee.Email, empSubject, empHtml, isHtml: true);
        }

        // 2. Admin Alert Emails
        var adminEmails = await _bookingRepository.GetAdminEmailsAsync();
        if (adminEmails.Count > 0)
        {
            var adminSubject = $"[Admin Alert] New Room Booking: '{meetingTitle}' by {employeeName}";
            var adminHtml = BuildAdminBookingAlertEmailHtml(
                employeeName,
                employee?.Email ?? string.Empty,
                employee?.Department ?? string.Empty,
                meetingTitle,
                purpose,
                roomName,
                booking.BookingDate,
                booking.StartTime,
                booking.EndTime,
                booking.ParticipantCount);

            foreach (var adminEmail in adminEmails)
            {
                try
                {
                    await _emailService.SendEmailAsync(adminEmail, adminSubject, adminHtml, isHtml: true);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send admin booking alert email to {Email}", adminEmail);
                }
            }
        }
    }

    private async Task SendBookingCancellationEmailsAsync(
        int bookingId,
        int employeeId,
        string employeeName,
        string reason)
    {
        var employee = await _bookingRepository.GetEmployeeByIdAsync(employeeId);
        var booking = await _bookingRepository.GetBookingByIdAsync(bookingId, employeeId);

        var roomName = booking?.RoomName ?? "Meeting Room";
        var meetingTitle = booking?.MeetingTitle ?? "Room Booking";

        // 1. Employee Cancellation Email
        if (employee != null && !string.IsNullOrWhiteSpace(employee.Email))
        {
            var subject = $"Booking Cancelled: '{meetingTitle}'";
            var body = $@"
<!DOCTYPE html>
<html>
<head><meta charset=""utf-8""><title>Booking Cancelled</title></head>
<body style=""font-family: sans-serif; background-color: #f4f6f9; padding: 24px; color: #1e293b;"">
    <div style=""max-width: 560px; margin: 0 auto; background: #fff; border-radius: 8px; padding: 24px; box-shadow: 0 2px 8px rgba(0,0,0,0.05);"">
        <h2 style=""color: #dc2626; margin-top: 0;"">Booking Cancelled</h2>
        <p>Hello {employeeName},</p>
        <p>Your room booking for <strong>'{meetingTitle}'</strong> in <strong>{roomName}</strong> has been cancelled.</p>
        <div style=""background: #fef2f2; border: 1px solid #fee2e2; border-radius: 6px; padding: 12px 16px; margin: 16px 0;"">
            <strong>Reason for cancellation:</strong> {reason}
        </div>
        <p style=""font-size: 13px; color: #64748b;"">If this was a mistake, please make a new booking in SpaceBook.</p>
    </div>
</body>
</html>";
            await _emailService.SendEmailAsync(employee.Email, subject, body, isHtml: true);
        }

        // 2. Admin Cancellation Alert
        var adminEmails = await _bookingRepository.GetAdminEmailsAsync();
        if (adminEmails.Count > 0)
        {
            var adminSubject = $"[Admin Alert] Booking Cancelled: '{meetingTitle}' by {employeeName}";
            var adminBody = $@"
<!DOCTYPE html>
<html>
<head><meta charset=""utf-8""><title>Booking Cancelled Alert</title></head>
<body style=""font-family: sans-serif; background-color: #f4f6f9; padding: 24px; color: #1e293b;"">
    <div style=""max-width: 560px; margin: 0 auto; background: #fff; border-radius: 8px; padding: 24px; box-shadow: 0 2px 8px rgba(0,0,0,0.05);"">
        <h2 style=""color: #dc2626; margin-top: 0;"">Room Booking Cancelled</h2>
        <p>The following booking has been cancelled by <strong>{employeeName}</strong>:</p>
        <ul>
            <li><strong>Meeting:</strong> {meetingTitle}</li>
            <li><strong>Room:</strong> {roomName}</li>
            <li><strong>Reason:</strong> {reason}</li>
        </ul>
    </div>
</body>
</html>";

            foreach (var adminEmail in adminEmails)
            {
                try
                {
                    await _emailService.SendEmailAsync(adminEmail, adminSubject, adminBody, isHtml: true);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send admin cancellation alert to {Email}", adminEmail);
                }
            }
        }
    }

    private async Task SendBookingRescheduleEmailsAsync(
        int bookingId,
        int employeeId,
        string employeeName,
        UpdateBookingRequestDto request)
    {
        var employee = await _bookingRepository.GetEmployeeByIdAsync(employeeId);
        var room = request.RoomId.HasValue ? await _bookingRepository.GetRoomByIdAsync(request.RoomId.Value) : null;
        var roomName = room?.RoomName ?? "Meeting Room";
        var meetingTitle = !string.IsNullOrWhiteSpace(request.MeetingTitle) ? request.MeetingTitle : "Room Booking";

        // 1. Employee Reschedule Email
        if (employee != null && !string.IsNullOrWhiteSpace(employee.Email))
        {
            var subject = $"Booking Rescheduled: '{meetingTitle}'";
            var body = $@"
<!DOCTYPE html>
<html>
<head><meta charset=""utf-8""><title>Booking Rescheduled</title></head>
<body style=""font-family: sans-serif; background-color: #f4f6f9; padding: 24px; color: #1e293b;"">
    <div style=""max-width: 560px; margin: 0 auto; background: #fff; border-radius: 8px; padding: 24px; box-shadow: 0 2px 8px rgba(0,0,0,0.05);"">
        <h2 style=""color: #2563eb; margin-top: 0;"">Booking Rescheduled & Approved</h2>
        <p>Hello {employeeName},</p>
        <p>Your booking for <strong>'{meetingTitle}'</strong> has been successfully rescheduled:</p>
        <table width=""100%"" style=""background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 6px; padding: 12px;"">
            <tr><td><strong>Room:</strong></td><td>{roomName}</td></tr>
            <tr><td><strong>New Date:</strong></td><td>{request.BookingDate:MMMM dd, yyyy}</td></tr>
            <tr><td><strong>New Time:</strong></td><td>{request.StartTime:hh\\:mm tt} - {request.EndTime:hh\\:mm tt}</td></tr>
        </table>
    </div>
</body>
</html>";
            await _emailService.SendEmailAsync(employee.Email, subject, body, isHtml: true);
        }

        // 2. Admin Reschedule Alert
        var adminEmails = await _bookingRepository.GetAdminEmailsAsync();
        if (adminEmails.Count > 0)
        {
            var adminSubject = $"[Admin Alert] Booking Rescheduled: '{meetingTitle}' by {employeeName}";
            var adminBody = $@"
<!DOCTYPE html>
<html>
<head><meta charset=""utf-8""><title>Booking Rescheduled Alert</title></head>
<body style=""font-family: sans-serif; background-color: #f4f6f9; padding: 24px; color: #1e293b;"">
    <div style=""max-width: 560px; margin: 0 auto; background: #fff; border-radius: 8px; padding: 24px; box-shadow: 0 2px 8px rgba(0,0,0,0.05);"">
        <h2 style=""color: #2563eb; margin-top: 0;"">Booking Rescheduled</h2>
        <p><strong>{employeeName}</strong> has rescheduled their booking:</p>
        <ul>
            <li><strong>Meeting:</strong> {meetingTitle}</li>
            <li><strong>Room:</strong> {roomName}</li>
            <li><strong>Date:</strong> {request.BookingDate:MMMM dd, yyyy}</li>
            <li><strong>Time:</strong> {request.StartTime:hh\\:mm tt} - {request.EndTime:hh\\:mm tt}</li>
        </ul>
    </div>
</body>
</html>";

            foreach (var adminEmail in adminEmails)
            {
                try
                {
                    await _emailService.SendEmailAsync(adminEmail, adminSubject, adminBody, isHtml: true);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send admin reschedule alert to {Email}", adminEmail);
                }
            }
        }
    }

    private static string BuildBookingConfirmedEmailHtml(
        string employeeName,
        string meetingTitle,
        string purpose,
        string roomName,
        string roomNumber,
        DateOnly bookingDate,
        TimeOnly startTime,
        TimeOnly endTime,
        int participantCount)
    {
        return $@"
<!DOCTYPE html>
<html>
<head><meta charset=""utf-8""><title>Booking Confirmed</title></head>
<body style=""font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; background-color: #f4f6f9; margin: 0; padding: 24px; color: #1e293b;"">
    <table align=""center"" border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""max-width: 580px; background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 12px rgba(0, 0, 0, 0.06);"">
        <tr>
            <td style=""background: linear-gradient(135deg, #10b981 0%, #059669 100%); padding: 30px 28px; text-align: center;"">
                <h1 style=""color: #ffffff; margin: 0; font-size: 22px; font-weight: 700;"">SpaceBook</h1>
                <p style=""color: #d1fae5; margin: 6px 0 0; font-size: 14px;"">Booking Confirmed & Approved</p>
            </td>
        </tr>
        <tr>
            <td style=""padding: 28px;"">
                <h2 style=""color: #0f172a; margin: 0 0 12px; font-size: 18px;"">Hello {employeeName},</h2>
                <p style=""color: #475569; font-size: 15px; line-height: 1.5; margin: 0 0 20px;"">
                    Your room booking for <strong>'{meetingTitle}'</strong> has been automatically approved and confirmed.
                </p>
                <table width=""100%"" cellpadding=""6"" cellspacing=""0"" style=""background-color: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; margin-bottom: 20px;"">
                    <tr><td width=""35%"" style=""color: #64748b; font-size: 13px;"">Room</td><td style=""color: #0f172a; font-weight: 600; font-size: 14px;"">{roomName} {(string.IsNullOrWhiteSpace(roomNumber) ? "" : $"({roomNumber})")}</td></tr>
                    <tr><td style=""color: #64748b; font-size: 13px;"">Date</td><td style=""color: #0f172a; font-size: 14px;"">{bookingDate:MMMM dd, yyyy}</td></tr>
                    <tr><td style=""color: #64748b; font-size: 13px;"">Time</td><td style=""color: #059669; font-weight: 600; font-size: 14px;"">{startTime:hh\\:mm tt} - {endTime:hh\\:mm tt}</td></tr>
                    {(participantCount > 0 ? $"<tr><td style=\"color: #64748b; font-size: 13px;\">Attendees</td><td style=\"color: #0f172a; font-size: 14px;\">{participantCount} people</td></tr>" : "")}
                    {(!string.IsNullOrWhiteSpace(purpose) ? $"<tr><td style=\"color: #64748b; font-size: 13px;\">Purpose</td><td style=\"color: #0f172a; font-size: 14px;\">{purpose}</td></tr>" : "")}
                </table>
                <p style=""color: #64748b; font-size: 13px; margin: 0;"">You will receive an email reminder 15 minutes before your meeting starts.</p>
            </td>
        </tr>
    </table>
</body>
</html>";
    }

    private static string BuildAdminBookingAlertEmailHtml(
        string employeeName,
        string employeeEmail,
        string department,
        string meetingTitle,
        string purpose,
        string roomName,
        DateOnly bookingDate,
        TimeOnly startTime,
        TimeOnly endTime,
        int participantCount)
    {
        return $@"
<!DOCTYPE html>
<html>
<head><meta charset=""utf-8""><title>New Booking Alert</title></head>
<body style=""font-family: sans-serif; background-color: #f4f6f9; padding: 24px; color: #1e293b;"">
    <div style=""max-width: 580px; margin: 0 auto; background: #ffffff; border-radius: 8px; padding: 24px; border-left: 4px solid #3b82f6; box-shadow: 0 2px 8px rgba(0,0,0,0.05);"">
        <h2 style=""color: #1e40af; margin-top: 0;"">[Admin Alert] New Room Booking</h2>
        <p>A new room booking has been automatically approved in SpaceBook:</p>
        <table width=""100%"" cellpadding=""4"" cellspacing=""0"" style=""background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 6px; margin: 16px 0;"">
            <tr><td><strong>Booked By:</strong></td><td>{employeeName} ({employeeEmail})</td></tr>
            {(!string.IsNullOrWhiteSpace(department) ? $"<tr><td><strong>Department:</strong></td><td>{department}</td></tr>" : "")}
            <tr><td><strong>Meeting:</strong></td><td>{meetingTitle}</td></tr>
            <tr><td><strong>Room:</strong></td><td>{roomName}</td></tr>
            <tr><td><strong>Date:</strong></td><td>{bookingDate:MMMM dd, yyyy}</td></tr>
            <tr><td><strong>Time:</strong></td><td>{startTime:hh\\:mm tt} - {endTime:hh\\:mm tt}</td></tr>
            <tr><td><strong>Attendees:</strong></td><td>{participantCount}</td></tr>
        </table>
    </div>
</body>
</html>";
    }
}