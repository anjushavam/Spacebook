using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpaceBook.Application.Interfaces;
 
namespace SpaceBook.API.Controllers;
 
[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _service;
 
    public AdminController(IAdminService service)
    {
        _service = service;
    }
 
    // GET: api/admin/dashboard
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        var data = await _service.GetDashboardAsync();
 
        return Ok(data);
    }
}