using SpaceBook.Application.DTOs.Hotseat;

namespace SpaceBook.Application.Interfaces;

public interface IHotseatRepository
{
    Task<IEnumerable<HotseatSeatDto>> GetSeatsAsync(
        DateOnly? date,
        string? city,
        string? building,
        string? module);
}