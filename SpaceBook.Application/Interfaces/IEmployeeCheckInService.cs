using SpaceBook.Application.DTOs.Booking;
 
namespace SpaceBook.Application.Interfaces;
 
public interface IEmployeeCheckInService
{
    Task<CheckInDto> CheckInAsync(
        int bookingId,
        int employeeId);
}