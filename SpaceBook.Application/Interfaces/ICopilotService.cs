using SpaceBook.Application.DTOs.Copilot;

namespace SpaceBook.Application.Interfaces;

public interface ICopilotService
{
    Task<List<OfficeCopilotDto>> GetOfficesAsync();

    Task<List<RoomCopilotDto>> GetRoomsAsync(
        string? search,
        int? officeId,
        int? roomTypeId,
        int? minCapacity,
        string? facility);

    Task<CopilotAvailabilityResponseDto> GetAvailabilityAsync(
        DateOnly date,
        int? roomTypeId);
    Task<List<CopilotRecommendationDto>> GetRecommendationsAsync(
    CopilotRecommendationRequestDto request);    
}