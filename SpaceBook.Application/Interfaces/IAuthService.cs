using SpaceBook.Application.DTOs.Auth;
namespace SpaceBook.Application.Interfaces;
public interface IAuthService{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
    Task<LoginResponse?> SsoLoginAsync(SsoLoginRequest request);
}