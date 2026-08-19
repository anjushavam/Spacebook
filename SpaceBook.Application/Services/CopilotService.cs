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

    // =========================================================
    // GET OFFICES
    // =========================================================

    public async Task<List<OfficeCopilotDto>> GetOfficesAsync(
        string? search)
    {
        return await _copilotRepository.GetOfficesAsync(search);
    }

    // =========================================================
    // GET / SEARCH ROOMS
    // =========================================================

    public async Task<List<RoomCopilotDto>> GetRoomsAsync(
        string? search,
        int? officeId,
        int? roomTypeId,
        int? minCapacity,
        string? facility)
    {
        if (minCapacity.HasValue && minCapacity.Value < 0)
        {
            throw new ArgumentException(
                "Minimum capacity cannot be negative.");
        }

        return await _copilotRepository.GetRoomsAsync(
            search,
            officeId,
            roomTypeId,
            minCapacity,
            facility);
    }

    // =========================================================
    // GET ROOM AVAILABILITY
    // =========================================================

    public async Task<CopilotAvailabilityResponseDto> GetAvailabilityAsync(
        DateOnly date,
        int? roomTypeId)
    {
        if (date == default)
        {
            throw new ArgumentException(
                "A valid date is required.");
        }

        return await _copilotRepository.GetAvailabilityAsync(
            date,
            roomTypeId);
    }

    // =========================================================
    // GET ROOM RECOMMENDATIONS
    // =========================================================

    public async Task<List<CopilotRecommendationDto>>
        GetRecommendationsAsync(
            CopilotRecommendationRequestDto request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (request.Date == default)
        {
            throw new ArgumentException(
                "A valid booking date is required.");
        }

        if (request.ParticipantCount <= 0)
        {
            throw new ArgumentException(
                "Participant count must be at least 1.");
        }

        if (request.StartTime >= request.EndTime)
        {
            throw new ArgumentException(
                "Start time must be before end time.");
        }

        return await _copilotRepository
            .GetRecommendationsAsync(request);
    }
}