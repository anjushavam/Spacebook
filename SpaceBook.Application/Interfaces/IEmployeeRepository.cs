using SpaceBook.Domain.Entities;
namespace SpaceBook.Application.Interfaces;
public interface IEmployeeRepository{
    Task<Employee?> GetByEmailAsync(string email);
    
}