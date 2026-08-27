using SpaceBook.Application.DTOs.Copilot;

namespace SpaceBook.Application.Interfaces;

public interface ICopilotService
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

    // =========================================================
    // HOTSEATS - AVAILABILITY & SUMMARY
    // =========================================================

    Task<HotseatSummaryCopilotDto> GetHotseatSummaryAsync(
        DateOnly? date,
        string? location,
        string? office,
        string? module);

    // =========================================================
    // HOTSEATS - SEARCH & DETAILS
    // =========================================================

    Task<List<HotseatCopilotDto>> GetHotseatsAsync(
        HotseatSearchFilterCopilotDto filter);

    // =========================================================
    // HOTSEATS - LOCATIONS
    // =========================================================

    Task<List<HotseatLocationCopilotDto>> GetHotseatLocationsAsync();
}