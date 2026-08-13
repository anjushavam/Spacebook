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
    // Office Hours:
    // 09:00 AM to 07:30 PM
    // =========================================================

    private static readonly TimeOnly OfficeStartTime =
        new TimeOnly(9, 0);

    private static readonly TimeOnly OfficeEndTime =
        new TimeOnly(19, 30);

    public EmployeeBookingService(
        IEmployeeBookingRepository bookingRepository,
        INotificationRepository notificationRepository)
    {
        _bookingRepository = bookingRepository;
        _notificationRepository = notificationRepository;
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
        // =====================================================
        // VALIDATE WEEKEND
        // =====================================================

        ValidateWeekday(request.BookingDate);

        // =====================================================
        // VALIDATE TIME
        // =====================================================

        if (request.StartTime >= request.EndTime)
        {
            throw new Exception(
                "End time must be after start time.");
        }

        // =====================================================
        // VALIDATE OFFICE HOURS
        // =====================================================

        if (request.StartTime < OfficeStartTime)
        {
            throw new Exception(
                "Bookings can only start from 09:00 AM.");
        }

        if (request.EndTime > OfficeEndTime)
        {
            throw new Exception(
                "Bookings must end by 07:30 PM.");
        }

        // =====================================================
        // VALIDATE PAST DATE/TIME
        // =====================================================

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

        // =====================================================
        // VALIDATE PARTICIPANT COUNT
        // =====================================================

        if (request.ParticipantCount <= 0)
        {
            throw new Exception(
                "Participant count must be at least 1.");
        }

        // =====================================================
        // VALIDATE ROOM ID
        // =====================================================

        if (request.RoomId <= 0)
        {
            throw new Exception(
                "Room ID is required.");
        }

        // =====================================================
        // VALIDATE ROOM CAPACITY
        // =====================================================

        var roomCapacity =
            await _bookingRepository.GetRoomCapacityAsync(
                request.RoomId);

        if (roomCapacity == null)
        {
            throw new Exception(
                "Selected room is not available.");
        }

        if (request.ParticipantCount > roomCapacity.Value)
        {
            throw new Exception(
                $"The selected room can accommodate a maximum of {roomCapacity.Value} participants.");
        }

        // =====================================================
        // CHECK ROOM AVAILABILITY
        // =====================================================

        bool isAvailable =
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

        // =====================================================
        // RESOLVE MEETING TITLE AND PURPOSE
        // =====================================================

        string resolvedPurpose =
            !string.IsNullOrWhiteSpace(request.Purpose)
                ? request.Purpose
                : !string.IsNullOrWhiteSpace(request.MeetingTitle)
                    ? request.MeetingTitle
                    : "Reserved Workspace";

        string resolvedTitle =
            !string.IsNullOrWhiteSpace(request.MeetingTitle)
                ? request.MeetingTitle
                : resolvedPurpose;

        // =====================================================
        // CREATE BOOKING
        // =====================================================

        var booking = new Booking
        {
            RoomId = request.RoomId,

            EmployeeId = employeeId,

            MeetingTitle = resolvedTitle,

            Purpose = resolvedPurpose,

            ParticipantCount =
                request.ParticipantCount,

            BookingDate =
                request.BookingDate,

            StartTime =
                request.StartTime,

            EndTime =
                request.EndTime,

            BookedOn =
                DateTime.UtcNow,

            Status = "Pending"
        };

        try
        {
            await _bookingRepository.CreateBookingAsync(
                booking);

            await _bookingRepository.SaveChangesAsync();

            // =================================================
            // CREATE ADMIN NOTIFICATION
            // =================================================

            var adminNotification = new Notification
            {
                EmployeeId = employeeId,

                BookingId = booking.BookingId,

                Message =
                    $"New booking request submitted for {resolvedTitle}.",

                IsRead = false,

                CreatedAt = DateTime.UtcNow
            };

            await _notificationRepository.AddAsync(
                adminNotification);

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
    // VIEW BOOKING
    // =========================================================

    public async Task<BookingDetailsDto?> GetBookingByIdAsync(
        int bookingId,
        int employeeId)
    {
        return await _bookingRepository.GetBookingByIdAsync(
            bookingId,
            employeeId);
    }

    // =========================================================
    // CANCEL BOOKING
    // =========================================================

    public async Task<bool> CancelBookingAsync(
        int bookingId,
        int employeeId)
    {
        var result =
            await _bookingRepository.CancelBookingAsync(
                bookingId,
                employeeId);

        if (result)
        {
            var adminNotification = new Notification
            {
                EmployeeId = employeeId,

                BookingId = bookingId,

                Message =
                    $"Booking #{bookingId} was cancelled by employee.",

                IsRead = false,

                CreatedAt = DateTime.UtcNow
            };

            await _notificationRepository.AddAsync(
                adminNotification);

            await _notificationRepository.SaveChangesAsync();
        }

        return result;
    }

    // =========================================================
    // UPDATE / RESCHEDULE BOOKING
    // =========================================================

    public async Task<bool> UpdateBookingAsync(
        int bookingId,
        int employeeId,
        UpdateBookingRequestDto request)
    {
        // =====================================================
        // VALIDATE WEEKEND
        // =====================================================

        ValidateWeekday(request.BookingDate);

        // =====================================================
        // VALIDATE TIME
        // =====================================================

        if (request.StartTime >= request.EndTime)
        {
            throw new Exception(
                "End time must be after start time.");
        }

        // =====================================================
        // VALIDATE OFFICE HOURS
        // =====================================================

        if (request.StartTime < OfficeStartTime)
        {
            throw new Exception(
                "Bookings can only start from 09:00 AM.");
        }

        if (request.EndTime > OfficeEndTime)
        {
            throw new Exception(
                "Bookings must end by 07:30 PM.");
        }

        // =====================================================
        // VALIDATE PARTICIPANT COUNT
        // =====================================================

        if (request.ParticipantCount <= 0)
        {
            throw new Exception(
                "Participant count must be at least 1.");
        }

        // =====================================================
        // VALIDATE ROOM ID
        // =====================================================

        if (!request.RoomId.HasValue ||
            request.RoomId.Value <= 0)
        {
            throw new Exception(
                "Room ID is required.");
        }

        // =====================================================
        // CHECK EXISTING BOOKING
        // =====================================================

        var existingBooking =
            await _bookingRepository.GetBookingByIdAsync(
                bookingId,
                employeeId);

        if (existingBooking == null)
        {
            throw new Exception(
                "Booking not found.");
        }

        // =====================================================
        // CHECK RESCHEDULE TIME RESTRICTION
        // =====================================================

        var bookingStartDateTime =
            existingBooking.BookingDate.ToDateTime(
                existingBooking.StartTime);

        if (DateTime.Now >=
            bookingStartDateTime.AddHours(-1))
        {
            throw new Exception(
                "Booking cannot be rescheduled within 1 hour before start time.");
        }

        // =====================================================
        // VALIDATE NEW BOOKING DATE/TIME
        // =====================================================

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

        // =====================================================
        // VALIDATE ROOM CAPACITY
        // =====================================================

        var roomCapacity =
            await _bookingRepository.GetRoomCapacityAsync(
                request.RoomId.Value);

        if (roomCapacity == null)
        {
            throw new Exception(
                "Selected room is not available.");
        }

        if (request.ParticipantCount > roomCapacity.Value)
        {
            throw new Exception(
                $"The selected room can accommodate a maximum of {roomCapacity.Value} participants.");
        }

        // =====================================================
        // CHECK ROOM AVAILABILITY
        // =====================================================

        bool isAvailable =
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

        // =====================================================
        // UPDATE BOOKING
        // =====================================================

        bool updated =
            await _bookingRepository.UpdateBookingAsync(
                bookingId,
                employeeId,
                request);

        if (updated)
        {
            var adminNotification = new Notification
            {
                EmployeeId = employeeId,

                BookingId = bookingId,

                Message =
                    $"Booking #{bookingId} was rescheduled by employee.",

                IsRead = false,

                CreatedAt = DateTime.UtcNow
            };

            await _notificationRepository.AddAsync(
                adminNotification);

            await _notificationRepository.SaveChangesAsync();
        }

        return updated;
    }

    // =========================================================
    // SEARCH AVAILABLE ROOMS
    // =========================================================

    public async Task<List<AvailableRoomDto>>
        SearchAvailableRoomsAsync(
            SearchRoomsRequestDto request)
    {
        bool hasModule =
            !string.IsNullOrWhiteSpace(request.Module);

        bool hasRoomType =
            request.RoomTypeId.HasValue &&
            request.RoomTypeId.Value > 0;

        bool hasParticipantCount =
            request.ParticipantCount.HasValue &&
            request.ParticipantCount.Value > 0;

        bool hasBookingDate =
            request.BookingDate.HasValue;

        bool hasStartTime =
            request.StartTime.HasValue;

        bool hasEndTime =
            request.EndTime.HasValue;

        bool hasFacilities =
            request.FacilityIds != null &&
            request.FacilityIds.Any(id => id > 0);

        // =====================================================
        // VALIDATE AT LEAST ONE SEARCH CRITERION
        // =====================================================

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

        // =====================================================
        // VALIDATE WEEKEND
        // =====================================================

        if (hasBookingDate)
        {
            ValidateWeekday(
                request.BookingDate!.Value);
        }

        // =====================================================
        // VALIDATE TIME
        // =====================================================

        if (hasStartTime &&
            hasEndTime &&
            request.StartTime!.Value >=
            request.EndTime!.Value)
        {
            throw new Exception(
                "End time must be after start time.");
        }

        // =====================================================
        // VALIDATE OFFICE HOURS
        // =====================================================

        if (hasStartTime &&
            request.StartTime!.Value <
            OfficeStartTime)
        {
            throw new Exception(
                "Rooms can only be booked between 09:00 AM and 07:30 PM.");
        }

        if (hasEndTime &&
            request.EndTime!.Value >
            OfficeEndTime)
        {
            throw new Exception(
                "Rooms can only be booked between 09:00 AM and 07:30 PM.");
        }

        // =====================================================
        // VALIDATE PAST DATE/TIME
        // =====================================================

        var now = DateTime.Now;

        var today =
            DateOnly.FromDateTime(now);

        var currentTime =
            TimeOnly.FromDateTime(now);

        if (hasBookingDate &&
            request.BookingDate!.Value < today)
        {
            throw new Exception(
                "Cannot search availability for a past date.");
        }

        if (hasBookingDate &&
            request.BookingDate!.Value == today &&
            hasStartTime &&
            request.StartTime!.Value <= currentTime)
        {
            throw new Exception(
                "Cannot search for a time that has already passed.");
        }

        // =====================================================
        // SEARCH AVAILABLE ROOMS
        // =====================================================

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
            .GetRoomsByModuleAsync(module);
    }
}