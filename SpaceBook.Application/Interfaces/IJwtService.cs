using SpaceBook.Domain.Entities;
namespace SpaceBook.Application.Interfaces;
public interface IJwtService{
    string GenerateToken(Employee employee);
}