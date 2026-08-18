using SpaceBook.Domain.Entities;

namespace SpaceBook.Application.Interfaces;

public interface IHotseatRepository
{
    Task<IEnumerable<HotseatBooking>> GetHotseatBookingsAsync(
        DateOnly? date,
        string? city,
        string? building,
        string? module);
}