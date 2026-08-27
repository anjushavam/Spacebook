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

    // =========================================================
    // HOTSEATS - AVAILABILITY & SUMMARY
    // =========================================================

    public async Task<HotseatSummaryCopilotDto> GetHotseatSummaryAsync(
        DateOnly? date,
        string? location,
        string? office,
        string? module)
    {
        return await _copilotRepository.GetHotseatSummaryAsync(
            date,
            location,
            office,
            module);
    }

    // =========================================================
    // HOTSEATS - SEARCH & DETAILS
    // =========================================================

    public async Task<List<HotseatCopilotDto>> GetHotseatsAsync(
        HotseatSearchFilterCopilotDto filter)
    {
        if (filter == null)
        {
            filter = new HotseatSearchFilterCopilotDto();
        }

        return await _copilotRepository.GetHotseatsAsync(filter);
    }

    // =========================================================
    // HOTSEATS - LOCATIONS
    // =========================================================

    public async Task<List<HotseatLocationCopilotDto>> GetHotseatLocationsAsync()
    {
        return await _copilotRepository.GetHotseatLocationsAsync();
    }

    // =========================================================
    // USER PROFILE & BOOKINGS
    // =========================================================

    public async Task<CopilotUserProfileDto?> GetUserProfileAsync(
        int? employeeId,
        string? email)
    {
        return await _copilotRepository.GetUserProfileAsync(
            employeeId,
            email);
    }

    public async Task<CopilotUserBookingsDto?> GetUserBookingsAsync(
        int? employeeId,
        string? email,
        DateOnly? date)
    {
        return await _copilotRepository.GetUserBookingsAsync(
            employeeId,
            email,
            date);
    }

    public async Task<List<CopilotUserProfileDto>> GetEmployeesAsync(
        string? search)
    {
        return await _copilotRepository.GetEmployeesAsync(search);
    }
}