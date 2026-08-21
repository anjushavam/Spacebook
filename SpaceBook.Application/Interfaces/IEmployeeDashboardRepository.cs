using SpaceBook.Application.DTOs.Employee;

namespace SpaceBook.Application.Interfaces;

public interface IEmployeeDashboardRepository
{
    Task<EmployeeDashboardDto> GetDashboardAsync(
        int employeeId);

    Task<AvailabilityCalendarDto> GetAvailabilityAsync(
        DateOnly date,
        int? roomTypeId);

    Task<List<MyBookingDto>> GetMyBookingsAsync(
        int employeeId);

    Task<List<RecentReservationDto>> GetRecentReservationsAsync(
        int employeeId);

    
}