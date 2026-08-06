using SpaceBook.Domain.Entities;
 
namespace SpaceBook.Application.Interfaces;
 
public interface IEmployeeCheckInRepository
{
    Task<Booking?> GetBookingAsync(
        int bookingId,
        int employeeId);
 
 
    Task AddAsync(CheckIn checkIn);
}