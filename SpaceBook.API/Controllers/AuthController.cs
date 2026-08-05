using Microsoft.AspNetCore.Mvc;using SpaceBook.Application.DTOs.Auth;using SpaceBook.Application.Interfaces;
namespace SpaceBook.API.Controllers;

[ApiController]
[Route("api/[controller]")]public class AuthController : ControllerBase{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);

        if (result == null)
            return Unauthorized(new            {
                Message = "Invalid Email or Password"            });

        return Ok(result);
    }
}