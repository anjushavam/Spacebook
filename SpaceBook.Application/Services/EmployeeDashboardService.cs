using SpaceBook.Application.DTOs.Employee;
using SpaceBook.Application.Interfaces;

namespace SpaceBook.Application.Services;

public class EmployeeDashboardService : IEmployeeDashboardService
{
    private readonly IEmployeeDashboardRepository _repository;

    public EmployeeDashboardService(IEmployeeDashboardRepository repository)
    {
        _repository = repository;
    }

    public async Task<EmployeeDashboardDto> GetDashboardAsync(int employeeId)
    {
        return await _repository.GetDashboardAsync(employeeId);
    }

    public async Task<List<RecentReservationDto>> GetRecentReservationsAsync(int employeeId)
    {
        return await _repository.GetRecentReservationsAsync(employeeId);
    }

    // Updated Method
    public async Task<AvailabilityCalendarDto> GetAvailabilityAsync(
        DateOnly date,
        int? roomTypeId)
    {
        return await _repository.GetAvailabilityAsync(date, roomTypeId);
    }

    public async Task<List<MyBookingDto>> GetMyBookingsAsync(int employeeId)
    {
        return await _repository.GetMyBookingsAsync(employeeId);
    }
}