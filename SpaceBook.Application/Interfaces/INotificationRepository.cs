using SpaceBook.Domain.Entities;
 
namespace SpaceBook.Application.Interfaces;
 
public interface INotificationRepository
{
    Task AddAsync(Notification notification);
 
    Task<List<Notification>> GetAllAsync();
 
    Task<List<Notification>> GetEmployeeNotificationsAsync(int employeeId);
}