using SpaceBook.Application.DTOs.Booking;
using SpaceBook.Application.Interfaces;
using SpaceBook.Domain.Entities;

namespace SpaceBook.Application.Services;

public class EmployeeBookingService : IEmployeeBookingService
{
    private readonly IEmployeeBookingRepository _bookingRepository;
    private readonly INotificationRepository _notificationRepository;

    public EmployeeBookingService(
        IEmployeeBookingRepository bookingRepository,
        INotificationRepository notificationRepository)
    {
        _bookingRepository = bookingRepository;
        _notificationRepository = notificationRepository;
    }

    // =========================================================
    // Create Booking
    // =========================================================

    public async Task<int> CreateBookingAsync(
        int employeeId,
        CreateBookingRequestDto request)
    {
        if (request.StartTime >= request.EndTime)
        {
            throw new Exception(
                "End time must be after start time.");
        }

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

        var booking = new Booking
        {
            RoomId = request.RoomId,
            EmployeeId = employeeId,
            MeetingTitle = resolvedTitle,
            Purpose = resolvedPurpose,
            ParticipantCount = request.ParticipantCount,
            BookingDate = request.BookingDate,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            BookedOn = DateTime.UtcNow,
            Status = "Pending"
        };

        try
        {
            await _bookingRepository.CreateBookingAsync(booking);

            await _bookingRepository.SaveChangesAsync();

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
    // View Booking
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
    // Cancel Booking
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
    // Update / Reschedule Booking
    // =========================================================

    public async Task<bool> UpdateBookingAsync(
        int bookingId,
        int employeeId,
        UpdateBookingRequestDto request)
    {
        if (request.StartTime >= request.EndTime)
        {
            throw new Exception(
                "End time must be after start time.");
        }

        var existingBooking =
            await _bookingRepository.GetBookingByIdAsync(
                bookingId,
                employeeId);

        if (existingBooking == null)
        {
            throw new Exception(
                "Booking not found.");
        }

        var bookingStartDateTime =
            existingBooking.BookingDate.ToDateTime(
                existingBooking.StartTime);

        if (DateTime.Now >= bookingStartDateTime.AddHours(-1))
        {
            throw new Exception(
                "Booking cannot be rescheduled within 1 hour before start time.");
        }

        bool isAvailable =
            await _bookingRepository.IsRoomAvailableAsync(
                request.RoomId,
                request.BookingDate,
                request.StartTime,
                request.EndTime,
                bookingId);

        if (!isAvailable)
        {
            throw new Exception(
                "Room is already booked for the selected time.");
        }

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
    // Search Available Rooms
    // =========================================================

    public async Task<List<AvailableRoomDto>> SearchAvailableRoomsAsync(
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

        if (hasStartTime &&
            hasEndTime &&
            request.StartTime!.Value >= request.EndTime!.Value)
        {
            throw new Exception(
                "End time must be after start time.");
        }

        return await _bookingRepository.SearchAvailableRoomsAsync(
            request);
    }


    // =========================================================
    // Get Rooms By Module
    // =========================================================
    //
    // Used when the employee selects only:
    //
    // Module 2
    //
    // This allows the UI to immediately get rooms belonging
    // to that module.
    // =========================================================

    public async Task<List<AvailableRoomDto>> GetRoomsByModuleAsync(
        string module)
    {
        if (string.IsNullOrWhiteSpace(module))
        {
            throw new Exception(
                "Module is required.");
        }

        return await _bookingRepository.GetRoomsByModuleAsync(
            module);
    }
}