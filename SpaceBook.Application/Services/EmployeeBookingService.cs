using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SpaceBook.Application.DTOs.Booking;
using SpaceBook.Application.Interfaces;
using SpaceBook.Domain.Entities;
using SpaceBook.Domain.Enums;

namespace SpaceBook.Application.Services;

public class EmployeeBookingService : IEmployeeBookingService
{
    private readonly IEmployeeBookingRepository _bookingRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IEmailService _emailService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmployeeBookingService> _logger;

    // =========================================================
    // OFFICE HOURS
    // =========================================================
    //
    // SpaceBook business hours:
    //
    // 10:00 AM - 10:00 PM IST
    //
    // =========================================================

    private static readonly TimeOnly OfficeStartTime =
        new TimeOnly(10, 0);

    private static readonly TimeOnly OfficeEndTime =
        new TimeOnly(22, 0);

    // =========================================================
    // INDIA TIMEZONE
    // =========================================================

    private static readonly TimeZoneInfo IndiaTimeZone =
        GetIndiaTimeZone();

    private static TimeZoneInfo GetIndiaTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(
                "Asia/Kolkata");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById(
                "India Standard Time");
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById(
                "India Standard Time");
        }
    }

    // =========================================================
    // GET CURRENT INDIA DATE/TIME
    // =========================================================

    private static DateTime GetIndiaNow()
    {
        return TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.UtcNow,
            IndiaTimeZone);
    }

    // =========================================================
    // GET CURRENT INDIA DATE
    // =========================================================

    private static DateOnly GetIndiaToday()
    {
        return DateOnly.FromDateTime(
            GetIndiaNow());
    }

    // =========================================================
    // GET CURRENT INDIA TIME
    // =========================================================

    private static TimeOnly GetIndiaCurrentTime()
    {
        return TimeOnly.FromDateTime(
            GetIndiaNow());
    }

    // =========================================================
    // DATABASE DATETIME
    // =========================================================
    //
    // PostgreSQL timestamp with time zone:
    //
    // ALWAYS UTC
    //
    // =========================================================

    private static DateTime GetDatabaseDateTime()
    {
        return DateTime.UtcNow;
    }

    // =========================================================
    // CHECK WEEKEND
    // =========================================================

    private static bool IsWeekend(
        DateOnly date)
    {
        return date.DayOfWeek ==
                   DayOfWeek.Saturday ||

               date.DayOfWeek ==
                   DayOfWeek.Sunday;
    }

    // =========================================================
    // VALIDATE WEEKDAY
    // =========================================================

    private static void ValidateWeekday(
        DateOnly date)
    {
        if (IsWeekend(date))
        {
            throw new Exception(
                "Bookings and room availability are not allowed on Saturdays and Sundays.");
        }
    }

    public EmployeeBookingService(
        IEmployeeBookingRepository bookingRepository,
        INotificationRepository notificationRepository,
        IEmailService emailService,
        IServiceScopeFactory scopeFactory,
        ILogger<EmployeeBookingService> logger)
    {
        _bookingRepository =
            bookingRepository;

        _notificationRepository =
            notificationRepository;

        _emailService =
            emailService;

        _scopeFactory =
            scopeFactory;

        _logger =
            logger;
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
        // VALIDATE MEETING TITLE
        // -----------------------------------------------------

        if (string.IsNullOrWhiteSpace(
            request.MeetingTitle))
        {
            throw new Exception(
                "Meeting title is required.");
        }

        // -----------------------------------------------------
        // VALIDATE DATE
        // -----------------------------------------------------

        ValidateWeekday(
            request.BookingDate);

        var today =
            GetIndiaToday();

        var currentTime =
            GetIndiaCurrentTime();

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

        if (request.StartTime >=
            request.EndTime)
        {
            throw new Exception(
                "End time must be after start time.");
        }

        // -----------------------------------------------------
        // VALIDATE OFFICE HOURS
        // -----------------------------------------------------

        if (request.StartTime <
            OfficeStartTime)
        {
            throw new Exception(
                "Bookings can only start from 10:00 AM.");
        }

        if (request.EndTime >
            OfficeEndTime)
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
            await _bookingRepository
                .GetRoomCapacityAsync(
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
            await _bookingRepository
                .IsRoomAvailableAsync(
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
            request.MeetingTitle.Trim();

        // =====================================================
        // CREATE BOOKING
        // =====================================================

        var booking = new Booking
        {
            RoomId =
                request.RoomId,

            EmployeeId =
                employeeId,

            MeetingTitle =
                resolvedTitle,

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

            await _bookingRepository
                .CreateBookingAsync(
                    booking);

            await _bookingRepository
                .SaveChangesAsync();

            // -------------------------------------------------
            // CREATE EMPLOYEE NOTIFICATION
            // -------------------------------------------------

            var notification =
                new Notification
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

            await _notificationRepository
                .AddAsync(notification);

            await _notificationRepository
                .SaveChangesAsync();

            // -------------------------------------------------
            // CAPTURE VALUES FOR BACKGROUND TASK
            // -------------------------------------------------

            var createdBookingId =
                booking.BookingId;

            var createdRoomId =
                booking.RoomId;

            // -------------------------------------------------
            // SEND CONFIRMATION EMAIL IN BACKGROUND
            // -------------------------------------------------

            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope =
                        _scopeFactory
                            .CreateScope();

                    var repo =
                        scope.ServiceProvider
                            .GetRequiredService<
                                IEmployeeBookingRepository>();

                    var reminderRepo =
                        scope.ServiceProvider
                            .GetRequiredService<
                                IBookingReminderRepository>();

                    var emailService =
                        scope.ServiceProvider
                            .GetRequiredService<
                                IEmailService>();

                    // -----------------------------------------
                    // GET EMPLOYEE
                    // -----------------------------------------

                    var employee =
                        await repo.GetEmployeeByIdAsync(
                            employeeId);

                    // -----------------------------------------
                    // GET ROOM
                    // -----------------------------------------

                    var room =
                        await repo.GetRoomByIdAsync(
                            createdRoomId);

                    // -----------------------------------------
                    // GET ADMIN EMAILS
                    // -----------------------------------------

                    var adminEmails =
                        await repo.GetAdminEmailsAsync();

                    var employeeName =
                        employee?.Name ??
                        "Colleague";

                    var roomName =
                        room != null
                            ? (!string.IsNullOrWhiteSpace(
                                    room.RoomName)
                                ? room.RoomName
                                : room.RoomNumber)
                            : "Meeting Room";

                    // -----------------------------------------
                    // SEND BOOKING CONFIRMATION
                    // -----------------------------------------

                    await emailService
                        .SendBookingConfirmationAsync(
                            booking,
                            employee ??
                                new Employee
                                {
                                    Name =
                                        employeeName,

                                    Email =
                                        employee?.Email ??
                                        string.Empty
                                },
                            room ??
                                new Room
                                {
                                    RoomName =
                                        roomName
                                },
                            adminEmails);

                    // -----------------------------------------
                    // RECORD EMAIL NOTIFICATION
                    // -----------------------------------------

                    await reminderRepo
                        .RecordNotificationSentAsync(
                            createdBookingId,
                            BookingNotificationType.BookingConfirmed,
                            "Sent");
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Background confirmation email dispatch failed for booking ID {BookingId}",
                        createdBookingId);
                }
            });

            return booking.BookingId;
        }
        catch (Exception ex)
        {
            throw new Exception(
                ex.InnerException?.Message ??
                ex.Message,
                ex);
        }
    }

    // =========================================================
    // GET BOOKING DETAILS
    // =========================================================

    public async Task<BookingDetailsDto?>
        GetBookingByIdAsync(
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

        return await _bookingRepository
            .GetBookingByIdAsync(
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
        // VALIDATE REASON
        // -----------------------------------------------------

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new Exception(
                "Cancellation reason is required.");
        }

        reason =
            reason.Trim();

        // -----------------------------------------------------
        // GET EMPLOYEE NAME
        // -----------------------------------------------------

        var employeeName =
            await _bookingRepository
                .GetEmployeeNameAsync(
                    employeeId);

        if (string.IsNullOrWhiteSpace(
            employeeName))
        {
            employeeName =
                "Unknown user";
        }

        // -----------------------------------------------------
        // CANCEL BOOKING
        // -----------------------------------------------------

        var result =
            await _bookingRepository
                .CancelBookingAsync(
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

        var notification =
            new Notification
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

        await _notificationRepository
            .AddAsync(notification);

        await _notificationRepository
            .SaveChangesAsync();

        // -----------------------------------------------------
        // SEND CANCELLATION EMAILS
        // -----------------------------------------------------

        _ = Task.Run(async () =>
        {
            try
            {
                using var scope =
                    _scopeFactory
                        .CreateScope();

                var repo =
                    scope.ServiceProvider
                        .GetRequiredService<
                            IEmployeeBookingRepository>();

                var emailService =
                    scope.ServiceProvider
                        .GetRequiredService<
                            IEmailService>();

                // ---------------------------------------------
                // GET EMPLOYEE
                // ---------------------------------------------

                var employee =
                    await repo.GetEmployeeByIdAsync(
                        employeeId);

                // ---------------------------------------------
                // GET BOOKING
                // ---------------------------------------------

                var booking =
                    await repo.GetBookingByIdAsync(
                        bookingId,
                        employeeId);

                var roomName =
                    booking?.RoomName ??
                    "Meeting Room";

                var meetingTitle =
                    booking?.MeetingTitle ??
                    "Room Booking";

                // ---------------------------------------------
                // EMPLOYEE EMAIL
                // ---------------------------------------------

                if (employee != null &&
                    !string.IsNullOrWhiteSpace(
                        employee.Email))
                {
                    var subject =
                        $"Booking Cancelled: '{meetingTitle}'";

                    var body =
                        BuildBookingCancelledEmailHtml(
                            employeeName,
                            meetingTitle,
                            roomName,
                            reason);

                    try
                    {
                        await emailService
                            .SendEmailAsync(
                                employee.Email,
                                subject,
                                body,
                                isHtml: true);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(
                            ex,
                            "Failed to send employee cancellation email to {Email}",
                            employee.Email);
                    }
                }

                // ---------------------------------------------
                // ADMIN EMAIL
                // ---------------------------------------------

                var adminEmails =
                    await repo.GetAdminEmailsAsync();

                if (adminEmails != null &&
                    adminEmails.Count > 0)
                {
                    var adminSubject =
                        $"[Admin Alert] Booking Cancelled: '{meetingTitle}' by {employeeName}";

                    var adminBody =
                        BuildAdminBookingCancelledEmailHtml(
                            employeeName,
                            meetingTitle,
                            roomName,
                            reason);

                    try
                    {
                        await emailService
                            .SendEmailsAsync(
                                adminEmails,
                                adminSubject,
                                adminBody,
                                isHtml: true);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(
                            ex,
                            "Failed to send admin cancellation alert emails");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Background cancellation email dispatch failed for booking ID {BookingId}",
                    bookingId);
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
        // VALIDATE MEETING TITLE
        // -----------------------------------------------------

        if (string.IsNullOrWhiteSpace(
            request.MeetingTitle))
        {
            throw new Exception(
                "Meeting title is required.");
        }

        // -----------------------------------------------------
        // VALIDATE DATE
        // -----------------------------------------------------

        ValidateWeekday(
            request.BookingDate);

        var today =
            GetIndiaToday();

        var currentTime =
            GetIndiaCurrentTime();

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

        if (request.StartTime >=
            request.EndTime)
        {
            throw new Exception(
                "End time must be after start time.");
        }

        // -----------------------------------------------------
        // VALIDATE OFFICE HOURS
        // -----------------------------------------------------

        if (request.StartTime <
            OfficeStartTime)
        {
            throw new Exception(
                "Bookings can only start from 10:00 AM.");
        }

        if (request.EndTime >
            OfficeEndTime)
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
            await _bookingRepository
                .GetBookingByIdAsync(
                    bookingId,
                    employeeId);

        if (existingBooking == null)
        {
            throw new Exception(
                "Booking not found.");
        }

        // -----------------------------------------------------
        // PREVENT RESCHEDULE OF CANCELLED
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
        // PREVENT RESCHEDULE OF REJECTED
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
        //
        // Employee can reschedule only if more than 1 hour
        // remains before the ORIGINAL booking start time.
        //
        // The original booking DateOnly + TimeOnly are treated
        // as IST business time.
        // -----------------------------------------------------

        var bookingStartDateTime =
            existingBooking.BookingDate
                .ToDateTime(
                    existingBooking.StartTime);

        var indiaNow =
            GetIndiaNow();

        if (indiaNow >=
            bookingStartDateTime.AddHours(-1))
        {
            throw new Exception(
                "Booking cannot be rescheduled within 1 hour before start time.");
        }

        // -----------------------------------------------------
        // VALIDATE ROOM CAPACITY
        // -----------------------------------------------------

        var roomCapacity =
            await _bookingRepository
                .GetRoomCapacityAsync(
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
            await _bookingRepository
                .IsRoomAvailableAsync(
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
            await _bookingRepository
                .UpdateBookingAsync(
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
            await _bookingRepository
                .GetEmployeeNameAsync(
                    employeeId);

        if (string.IsNullOrWhiteSpace(
            employeeName))
        {
            employeeName =
                "Unknown user";
        }

        // -----------------------------------------------------
        // CREATE ADMIN NOTIFICATION
        // -----------------------------------------------------

        var notification =
            new Notification
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

        await _notificationRepository
            .AddAsync(notification);

        await _notificationRepository
            .SaveChangesAsync();

        // -----------------------------------------------------
        // SEND RESCHEDULE EMAILS
        // -----------------------------------------------------

        _ = Task.Run(async () =>
        {
            try
            {
                using var scope =
                    _scopeFactory
                        .CreateScope();

                var repo =
                    scope.ServiceProvider
                        .GetRequiredService<
                            IEmployeeBookingRepository>();

                var reminderRepo =
                    scope.ServiceProvider
                        .GetRequiredService<
                            IBookingReminderRepository>();

                var emailService =
                    scope.ServiceProvider
                        .GetRequiredService<
                            IEmailService>();

                // ---------------------------------------------
                // RESET OLD REMINDER STATE
                // ---------------------------------------------

                await reminderRepo
                    .ResetRemindersForBookingAsync(
                        bookingId);

                // ---------------------------------------------
                // GET EMPLOYEE
                // ---------------------------------------------

                var employee =
                    await repo.GetEmployeeByIdAsync(
                        employeeId);

                // ---------------------------------------------
                // GET ROOM
                // ---------------------------------------------

                var room =
                    await repo.GetRoomByIdAsync(
                        request.RoomId.Value);

                var roomName =
                    room?.RoomName ??
                    "Meeting Room";

                var meetingTitle =
                    request.MeetingTitle.Trim();

                // ---------------------------------------------
                // EMPLOYEE RESCHEDULE EMAIL
                // ---------------------------------------------

                if (employee != null &&
                    !string.IsNullOrWhiteSpace(
                        employee.Email))
                {
                    var subject =
                        $"Booking Rescheduled: '{meetingTitle}'";

                    var body =
                        BuildBookingRescheduledEmailHtml(
                            employeeName,
                            meetingTitle,
                            roomName,
                            request);

                    try
                    {
                        await emailService
                            .SendEmailAsync(
                                employee.Email,
                                subject,
                                body,
                                isHtml: true);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(
                            ex,
                            "Failed to send employee reschedule email to {Email}",
                            employee.Email);
                    }
                }

                // ---------------------------------------------
                // ADMIN RESCHEDULE EMAIL
                // ---------------------------------------------

                var adminEmails =
                    await repo.GetAdminEmailsAsync();

                if (adminEmails != null &&
                    adminEmails.Count > 0)
                {
                    var adminSubject =
                        $"[Admin Alert] Booking Rescheduled: '{meetingTitle}' by {employeeName}";

                    var adminBody =
                        BuildAdminBookingRescheduledEmailHtml(
                            employeeName,
                            meetingTitle,
                            roomName,
                            request);

                    try
                    {
                        await emailService
                            .SendEmailsAsync(
                                adminEmails,
                                adminSubject,
                                adminBody,
                                isHtml: true);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(
                            ex,
                            "Failed to send admin reschedule alert emails");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Background reschedule email dispatch failed for booking ID {BookingId}",
                    bookingId);
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
            !string.IsNullOrWhiteSpace(
                request.Module);

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
            request.FacilityIds.Any(
                id => id > 0);

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

            ValidateWeekday(
                bookingDate);

            var today =
                GetIndiaToday();

            if (bookingDate < today)
            {
                throw new Exception(
                    "Cannot search availability for a past date.");
            }
        }

        // -----------------------------------------------------
        // VALIDATE TIME
        // -----------------------------------------------------

        if (hasStartTime &&
            hasEndTime)
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

            if (startTime <
                OfficeStartTime)
            {
                throw new Exception(
                    "Rooms can only be searched from 10:00 AM.");
            }

            if (endTime >
                OfficeEndTime)
            {
                throw new Exception(
                    "Rooms can only be searched until 10:00 PM.");
            }

            if (hasBookingDate &&
                request.BookingDate!.Value ==
                GetIndiaToday())
            {
                var currentSearchTime =
                    GetIndiaCurrentTime();

                if (startTime <=
                    currentSearchTime)
                {
                    throw new Exception(
                        "Cannot search for a time that has already passed.");
                }
            }
        }

        // -----------------------------------------------------
        // PARTICIPANT COUNT
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
            .SearchAvailableRoomsAsync(
                request);
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
    // GET ALL MODULES
    // =========================================================

    public async Task<List<ModuleDropdownDto>> GetModulesAsync()
    {
        return await _bookingRepository.GetModulesAsync();
    }

    // =========================================================
    // EMAIL HTML
    // BOOKING CANCELLED
    // =========================================================

    private static string
        BuildBookingCancelledEmailHtml(
            string employeeName,
            string meetingTitle,
            string roomName,
            string reason)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <title>Booking Cancelled</title>
</head>

<body style=""font-family: sans-serif; background-color: #f4f6f9; padding: 24px; color: #1e293b;"">

    <div style=""max-width: 560px; margin: 0 auto; background: #fff; border-radius: 8px; padding: 24px; box-shadow: 0 2px 8px rgba(0,0,0,0.05);"">

        <h2 style=""color: #dc2626; margin-top: 0;"">
            Booking Cancelled
        </h2>

        <p>
            Hello {employeeName},
        </p>

        <p>
            Your room booking for
            <strong>'{meetingTitle}'</strong>
            in <strong>{roomName}</strong>
            has been cancelled.
        </p>

        <div style=""background: #fef2f2; border: 1px solid #fee2e2; border-radius: 6px; padding: 12px 16px; margin: 16px 0;"">
            <strong>Reason for cancellation:</strong>
            {reason}
        </div>

        <p style=""font-size: 13px; color: #64748b;"">
            If this was a mistake, please make a new booking in SpaceBook.
        </p>

    </div>

</body>
</html>";
    }

    // =========================================================
    // EMAIL HTML
    // ADMIN BOOKING CANCELLED
    // =========================================================

    private static string
        BuildAdminBookingCancelledEmailHtml(
            string employeeName,
            string meetingTitle,
            string roomName,
            string reason)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <title>Booking Cancelled Alert</title>
</head>

<body style=""font-family: sans-serif; background-color: #f4f6f9; padding: 24px; color: #1e293b;"">

    <div style=""max-width: 560px; margin: 0 auto; background: #fff; border-radius: 8px; padding: 24px; box-shadow: 0 2px 8px rgba(0,0,0,0.05);"">

        <h2 style=""color: #dc2626; margin-top: 0;"">
            Room Booking Cancelled
        </h2>

        <p>
            The following booking has been cancelled by
            <strong>{employeeName}</strong>:
        </p>

        <ul>
            <li><strong>Meeting:</strong> {meetingTitle}</li>
            <li><strong>Room:</strong> {roomName}</li>
            <li><strong>Reason:</strong> {reason}</li>
        </ul>

    </div>

</body>
</html>";
    }

    // =========================================================
    // EMAIL HTML
    // BOOKING RESCHEDULED
    // =========================================================

    private static string
        BuildBookingRescheduledEmailHtml(
            string employeeName,
            string meetingTitle,
            string roomName,
            UpdateBookingRequestDto request)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <title>Booking Rescheduled</title>
</head>

<body style=""font-family: sans-serif; background-color: #f4f6f9; padding: 24px; color: #1e293b;"">

    <div style=""max-width: 560px; margin: 0 auto; background: #fff; border-radius: 8px; padding: 24px; box-shadow: 0 2px 8px rgba(0,0,0,0.05);"">

        <h2 style=""color: #2563eb; margin-top: 0;"">
            Booking Rescheduled & Approved
        </h2>

        <p>
            Hello {employeeName},
        </p>

        <p>
            Your booking for
            <strong>'{meetingTitle}'</strong>
            has been successfully rescheduled:
        </p>

        <table
            width=""100%""
            style=""background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 6px; padding: 12px;"">

            <tr>
                <td>
                    <strong>Room:</strong>
                </td>

                <td>
                    {roomName}
                </td>
            </tr>

            <tr>
                <td>
                    <strong>New Date:</strong>
                </td>

                <td>
                    {request.BookingDate:MMMM dd, yyyy}
                </td>
            </tr>

            <tr>
                <td>
                    <strong>New Time:</strong>
                </td>

                <td>
                    {request.StartTime:hh\:mm tt}
                    -
                    {request.EndTime:hh\:mm tt}
                </td>
            </tr>

        </table>

    </div>

</body>
</html>";
    }

    // =========================================================
    // EMAIL HTML
    // ADMIN BOOKING RESCHEDULED
    // =========================================================

    private static string
        BuildAdminBookingRescheduledEmailHtml(
            string employeeName,
            string meetingTitle,
            string roomName,
            UpdateBookingRequestDto request)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <title>Booking Rescheduled Alert</title>
</head>

<body style=""font-family: sans-serif; background-color: #f4f6f9; padding: 24px; color: #1e293b;"">

    <div style=""max-width: 560px; margin: 0 auto; background: #fff; border-radius: 8px; padding: 24px; box-shadow: 0 2px 8px rgba(0,0,0,0.05);"">

        <h2 style=""color: #2563eb; margin-top: 0;"">
            Booking Rescheduled
        </h2>

        <p>
            <strong>{employeeName}</strong>
            has rescheduled their booking:
        </p>

        <ul>
            <li>
                <strong>Meeting:</strong>
                {meetingTitle}
            </li>

            <li>
                <strong>Room:</strong>
                {roomName}
            </li>

            <li>
                <strong>Date:</strong>
                {request.BookingDate:MMMM dd, yyyy}
            </li>

            <li>
                <strong>Time:</strong>
                {request.StartTime:hh\:mm tt}
                -
                {request.EndTime:hh\:mm tt}
            </li>
        </ul>

    </div>

</body>
</html>";
    }
}