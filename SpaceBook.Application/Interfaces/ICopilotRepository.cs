using SpaceBook.Application.DTOs.Copilot;

namespace SpaceBook.Application.Interfaces;

public interface ICopilotRepository
{
    // =========================================================
    // OFFICES
    // =========================================================

    Task<List<OfficeCopilotDto>> GetOfficesAsync(
        string? search);

    // =========================================================
    // ROOMS
    // =========================================================

    Task<List<RoomCopilotDto>> GetRoomsAsync(
        string? search,
        int? officeId,
        int? roomTypeId,
        int? minCapacity,
        string? facility);

    // =========================================================
    // AVAILABILITY
    // =========================================================

    Task<CopilotAvailabilityResponseDto> GetAvailabilityAsync(
        DateOnly date,
        int? roomTypeId);

    // =========================================================
    // RECOMMENDATIONS
    // =========================================================

    Task<List<CopilotRecommendationDto>> GetRecommendationsAsync(
        CopilotRecommendationRequestDto request);
}