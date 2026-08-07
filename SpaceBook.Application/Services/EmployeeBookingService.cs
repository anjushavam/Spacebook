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

    // Create Booking
    public async Task<int> CreateBookingAsync(
        int employeeId,
        CreateBookingRequestDto request)
    {
        // Validate time
        if (request.StartTime >= request.EndTime)
        {
            throw new Exception("End time must be after start time.");
        }

        // Check availability
        bool isAvailable = await _bookingRepository.IsRoomAvailableAsync(
            request.RoomId,
            request.BookingDate,
            request.StartTime,
            request.EndTime);

        if (!isAvailable)
        {
            throw new Exception("Room is already booked for the selected time.");
        }

        // Fallback resolution to guarantee Purpose and MeetingTitle are never saved as empty strings
        string resolvedPurpose = !string.IsNullOrWhiteSpace(request.Purpose)
            ? request.Purpose
            : (!string.IsNullOrWhiteSpace(request.MeetingTitle) ? request.MeetingTitle : "Reserved Workspace");

        string resolvedTitle = !string.IsNullOrWhiteSpace(request.MeetingTitle)
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

            // CREATE NOTIFICATION FOR ADMIN
            var adminNotification = new Notification
            {
                EmployeeId = null, // Global notification accessible by Admin
                Message = $"New booking request submitted for {resolvedTitle}.",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _notificationRepository.AddAsync(adminNotification);

            // Flush changes so both Booking and Notification persist in DB
            await _bookingRepository.SaveChangesAsync();

            return booking.BookingId;
        }
        catch (Exception ex)
        {
            throw new Exception(
                ex.InnerException?.Message ?? ex.Message,
                ex);
        }
    }

    // View Booking
    public async Task<BookingDetailsDto?> GetBookingByIdAsync(
        int bookingId,
        int employeeId)
    {
        return await _bookingRepository.GetBookingByIdAsync(
            bookingId,
            employeeId);
    }

    // Cancel Booking
    public async Task<bool> CancelBookingAsync(
        int bookingId,
        int employeeId)
    {
        var result = await _bookingRepository.CancelBookingAsync(
            bookingId,
            employeeId);

        if (result)
        {
            var adminNotification = new Notification
            {
                EmployeeId = null,
                Message = $"Booking #{bookingId} was cancelled by employee.",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _notificationRepository.AddAsync(adminNotification);
            await _bookingRepository.SaveChangesAsync();
        }

        return result;
    }

    // Update / Reschedule Booking
    public async Task<bool> UpdateBookingAsync(
        int bookingId,
        int employeeId,
        UpdateBookingRequestDto request)
    {
        // Validate time
        if (request.StartTime >= request.EndTime)
        {
            throw new Exception("End time must be after start time.");
        }

        // Get existing booking
        var existingBooking = await _bookingRepository.GetBookingByIdAsync(
            bookingId,
            employeeId);

        if (existingBooking == null)
        {
            throw new Exception("Booking not found.");
        }

        // SLA check - cannot update within 1 hour
        var bookingStartDateTime = existingBooking.BookingDate.ToDateTime(existingBooking.StartTime);

        if (DateTime.Now >= bookingStartDateTime.AddHours(-1))
        {
            throw new Exception("Booking cannot be rescheduled within 1 hour before start time.");
        }

        // Check new slot availability
        bool isAvailable = await _bookingRepository.IsRoomAvailableAsync(
            request.RoomId,
            request.BookingDate,
            request.StartTime,
            request.EndTime,
            bookingId);

        if (!isAvailable)
        {
            throw new Exception("Room is already booked for the selected time.");
        }

        bool updated = await _bookingRepository.UpdateBookingAsync(
            bookingId,
            employeeId,
            request);

        if (updated)
        {
            var adminNotification = new Notification
            {
                EmployeeId = null,
                Message = $"Booking #{bookingId} was rescheduled by employee.",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _notificationRepository.AddAsync(adminNotification);
            await _bookingRepository.SaveChangesAsync();
        }

        return updated;
    }

    // Search Available Rooms
    public async Task<List<AvailableRoomDto>> SearchAvailableRoomsAsync(
        SearchRoomsRequestDto request)
    {
        return await _bookingRepository.SearchAvailableRoomsAsync(request);
    }
}