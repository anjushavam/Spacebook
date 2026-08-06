using SpaceBook.Application.DTOs.Reports;
 
namespace SpaceBook.Application.Interfaces;
 
public interface IReportService
{
    Task<BookingTrendDto> GetBookingTrendAsync(ReportFilterDto filter);
 
    Task<List<BookingStatusDto>> GetBookingStatusAsync(ReportFilterDto filter);
 
    Task<List<RoomUsageDto>> GetRoomUsageAsync(ReportFilterDto filter);
}