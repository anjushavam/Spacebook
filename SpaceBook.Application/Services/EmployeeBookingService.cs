using SpaceBook.Application.DTOs.Booking;
using SpaceBook.Application.Interfaces;
using SpaceBook.Domain.Entities;

namespace SpaceBook.Application.Services;

public class EmployeeBookingService : IEmployeeBookingService
{
    private readonly IEmployeeBookingRepository _bookingRepository;
    private readonly INotificationRepository _notificationRepository;

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
        INotificationRepository notificationRepository)
    {
        _bookingRepository = bookingRepository;
        _notificationRepository = notificationRepository;
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
                $"Booking was rescheduled by {employeeName} and requires approval.",

            IsRead =
                false,

            CreatedAt =
                GetDatabaseDateTime()
        };

        await _notificationRepository.AddAsync(
            notification);

        await _notificationRepository.SaveChangesAsync();

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
}