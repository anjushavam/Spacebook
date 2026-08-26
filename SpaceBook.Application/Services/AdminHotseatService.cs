using SpaceBook.Application.DTOs.Hotseat;
using SpaceBook.Application.Interfaces;

namespace SpaceBook.Application.Services;

public class AdminHotseatService : IAdminHotseatService
{
    private readonly IAdminHotseatRepository _repository;

    public AdminHotseatService(IAdminHotseatRepository repository)
    {
        _repository = repository;
    }

    public async Task<HotseatManagementDashboardDto> GetHotseatDashboardAnalyticsAsync(
        HotseatFilterDto filter)
    {
        return await _repository.GetHotseatDashboardAnalyticsAsync(filter);
    }

    public async Task<HotseatAuditPagedResultDto> GetHotseatAuditRecordsAsync(
        HotseatFilterDto filter)
    {
        return await _repository.GetHotseatAuditRecordsAsync(filter);
    }

    public async Task<byte[]> ExportHotseatsCsvAsync(
        HotseatFilterDto filter)
    {
        return await _repository.ExportHotseatsCsvAsync(filter);
    }

    public async Task<HotseatFilterOptionsDto> GetFilterOptionsAsync()
    {
        return await _repository.GetFilterOptionsAsync();
    }
}
