using SpaceBook.Application.DTOs.Reports;
using SpaceBook.Application.Interfaces;

namespace SpaceBook.Application.Services;

public class ReportService : IReportService
{
    private readonly IReportRepository _repository;

    public ReportService(IReportRepository repository)
    {
        _repository = repository;
    }

    public async Task<BookingTrendDto> GetBookingTrendAsync(
        ReportFilterDto filter)
    {
        return await _repository.GetBookingTrendAsync(filter);
    }

    public async Task<List<BookingStatusDto>> GetBookingStatusAsync(
        ReportFilterDto filter)
    {
        return await _repository.GetBookingStatusAsync(filter);
    }

    public async Task<List<RoomUsageDto>> GetRoomUsageAsync(
        ReportFilterDto filter)
    {
        return await _repository.GetRoomUsageAsync(filter);
    }
}