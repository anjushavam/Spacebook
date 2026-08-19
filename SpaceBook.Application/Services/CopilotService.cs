using SpaceBook.Application.DTOs.Copilot;
using SpaceBook.Application.Interfaces;
 
namespace SpaceBook.Application.Services;
 
public class CopilotService : ICopilotService
{
    private readonly ICopilotRepository _copilotRepository;
 
    public CopilotService(
        ICopilotRepository copilotRepository)
    {
        _copilotRepository = copilotRepository;
    }
 
    // =========================================================
    // GET OFFICES
    // =========================================================
 
    public async Task<List<OfficeCopilotDto>> GetOfficesAsync()
    {
        return await _copilotRepository
            .GetOfficesAsync();
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
        return await _copilotRepository
            .GetRoomsAsync(
                search,
                officeId,
                roomTypeId,
                minCapacity,
                facility);
    }
}