using SpaceBook.Application.DTOs.Hotseat;

namespace SpaceBook.Application.Interfaces;

public interface IHotseatService
{
    Task<IEnumerable<HotseatDto>> GetHotseatBookingsAsync(
        DateOnly? date,
        string? city,
        string? building,
        string? module);
}