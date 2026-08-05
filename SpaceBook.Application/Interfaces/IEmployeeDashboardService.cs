using SpaceBook.Application.DTOs.Employee;
 
namespace SpaceBook.Application.Interfaces;
 
public interface IEmployeeDashboardService
{
    Task<EmployeeDashboardDto> GetDashboardAsync(int employeeId);
 
    Task<AvailabilityCalendarDto> GetAvailabilityAsync(DateOnly date);

    Task<List<MyBookingDto>> GetMyBookingsAsync(int employeeId);

    Task<List<RecentReservationDto>> GetRecentReservationsAsync(int employeeId);
}