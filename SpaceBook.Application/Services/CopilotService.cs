using SpaceBook.Application.DTOs.Copilot;
using SpaceBook.Application.Interfaces;

namespace SpaceBook.Application.Services;

public class CopilotService : ICopilotService
{
    private readonly ICopilotRepository _copilotRepository;

    public CopilotService(ICopilotRepository copilotRepository)
    {
        _copilotRepository = copilotRepository;
    }

    public async Task<List<OfficeCopilotDto>> GetOfficesAsync(
    string? search)
{
    return await _copilotRepository.GetOfficesAsync(search);
}

    public async Task<List<RoomCopilotDto>> GetRoomsAsync(
        string? search,
        int? officeId,
        int? roomTypeId,
        int? minCapacity,
        string? facility)
    {
        return await _copilotRepository.GetRoomsAsync(
            search,
            officeId,
            roomTypeId,
            minCapacity,
            facility);
    }

    public async Task<CopilotAvailabilityResponseDto> GetAvailabilityAsync(
        DateOnly date,
        int? roomTypeId)
    {
        return await _copilotRepository.GetAvailabilityAsync(
            date,
            roomTypeId);
    }
    public async Task<List<CopilotRecommendationDto>> GetRecommendationsAsync(
    CopilotRecommendationRequestDto request)
{
    return await _copilotRepository.GetRecommendationsAsync(request);
}
}