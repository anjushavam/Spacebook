using SpaceBook.Application.DTOs.Employee;
 
public interface IEmployeeDashboardRepository
{
    Task<EmployeeDashboardDto> GetDashboardAsync(int employeeId);
 
    Task<AvailabilityCalendarDto> GetAvailabilityAsync(DateOnly date);
 
    Task<List<MyBookingDto>> GetMyBookingsAsync(int employeeId);
}