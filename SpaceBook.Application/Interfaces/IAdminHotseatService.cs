using SpaceBook.Application.DTOs.Hotseat;

namespace SpaceBook.Application.Interfaces;

public interface IAdminHotseatService
{
    Task<HotseatManagementDashboardDto> GetHotseatDashboardAnalyticsAsync(HotseatFilterDto filter);

    Task<HotseatAuditPagedResultDto> GetHotseatAuditRecordsAsync(HotseatFilterDto filter);

    Task<byte[]> ExportHotseatsCsvAsync(HotseatFilterDto filter);

    Task<HotseatFilterOptionsDto> GetFilterOptionsAsync();
}
