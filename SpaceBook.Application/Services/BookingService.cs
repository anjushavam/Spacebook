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

    public async Task<BookingDashboardDto> GetDashboardAsync()
    {
        return await _bookingRepository
            .GetDashboardAsync();
    }

    public async Task<IEnumerable<BookingDto>> GetAllAsync(
        BookingFilterDto filter)
    {
        return await _bookingRepository
            .GetAllAsync(filter);
    }

    public async Task<BookingDetailsDto?> GetByIdAsync(
        int bookingId)
    {
        return await _bookingRepository
            .GetByIdAsync(bookingId);
    }

    public async Task ApproveAsync(
        int bookingId)
    {
        if (!await _bookingRepository.ExistsAsync(bookingId))
        {
            throw new Exception(
                "Booking not found.");
        }

        await _bookingRepository
            .ApproveAsync(bookingId);

        // Fetch booking details to get Employee ID
        var booking = await _bookingRepository.GetByIdAsync(bookingId);
        if (booking != null)
        {
            var employeeNotification = new Notification
            {
                EmployeeId = booking.EmployeeId, // FIXED: Changed EmployeeId to UserId
                Message = $"Your booking for {booking.Purpose ?? booking.MeetingTitle ?? "Workspace"} has been approved.",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _notificationRepository.AddAsync(employeeNotification);
            await _notificationRepository.SaveChangesAsync(); // FIXED: Added SaveChangesAsync so it commits to DB
        }
    }

    public async Task RejectAsync(
        int bookingId)
    {
        if (!await _bookingRepository.ExistsAsync(bookingId))
        {
            throw new Exception(
                "Booking not found.");
        }

        await _bookingRepository
            .RejectAsync(bookingId);

        // Fetch booking details to get Employee ID
        var booking = await _bookingRepository.GetByIdAsync(bookingId);
        if (booking != null)
        {
            var employeeNotification = new Notification
            {
                EmployeeId = booking.EmployeeId, // FIXED: Changed EmployeeId to UserId
                Message = $"Your booking for {booking.Purpose ?? booking.MeetingTitle ?? "Workspace"} has been rejected.",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _notificationRepository.AddAsync(employeeNotification);
            await _notificationRepository.SaveChangesAsync(); // FIXED: Added SaveChangesAsync so it commits to DB
        }
    }

    public async Task DeleteAsync(
        int bookingId)
    {
        if (!await _bookingRepository.ExistsAsync(bookingId))
        {
            throw new Exception(
                "Booking not found.");
        }

        await _bookingRepository
            .DeleteAsync(bookingId);
    }
}