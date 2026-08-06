using SpaceBook.Domain.Entities;

namespace SpaceBook.Application.Interfaces;

public interface IMissedCheckInRepository
{
    Task<List<Booking>> GetTodayApprovedBookingsAsync();

    Task<bool> HasCheckInAsync(int bookingId);
}