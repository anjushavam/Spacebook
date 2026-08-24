using SpaceBook.Application.DTOs.Auth;
using SpaceBook.Application.Interfaces;
 
namespace SpaceBook.Application.Services;
 
public class AuthService : IAuthService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IJwtService _jwtService;
 
    public AuthService(
        IEmployeeRepository employeeRepository,
        IJwtService jwtService)
    {
        _employeeRepository = employeeRepository;
        _jwtService = jwtService;
    }
 
    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var employee = await _employeeRepository.GetByEmailAsync(request.Email);
 
        if (employee == null)
            return null;
 
        if (!employee.IsActive)
            return null;
 
        // Replace with BCrypt later
        if (employee.PasswordHash != request.Password)
            return null;
 
        var token = _jwtService.GenerateToken(employee);
 
        return new LoginResponse
        {
            EmployeeId = employee.EmployeeId,
            Name = employee.Name,
            Email = employee.Email,
            Role = employee.Role!.RoleName,
            Token = token
        };
    }

    public async Task<LoginResponse?> SsoLoginAsync(SsoLoginRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Email))
            return null;

        var employee = await _employeeRepository.GetByEmailAsync(request.Email);

        if (employee == null)
            return null;

        if (!employee.IsActive)
            return null;

        var token = _jwtService.GenerateToken(employee);

        return new LoginResponse
        {
            EmployeeId = employee.EmployeeId,
            Name = employee.Name,
            Email = employee.Email,
            Role = employee.Role?.RoleName ?? "Employee",
            Token = token
        };
    }
}