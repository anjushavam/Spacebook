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

    // =========================================================
    // USER PROFILE & BOOKINGS
    // =========================================================

    Task<CopilotUserProfileDto?> GetUserProfileAsync(
        int? employeeId,
        string? email);

    Task<CopilotUserBookingsDto?> GetUserBookingsAsync(
        int? employeeId,
        string? email,
        DateOnly? date);

    Task<List<CopilotUserProfileDto>> GetEmployeesAsync(
        string? search);
}