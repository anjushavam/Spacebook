using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpaceBook.Application.Interfaces;
 
namespace SpaceBook.API.Controllers;
 
[ApiController]
[Route("api/employee")]
[Authorize(Roles = "Employee")]
public class EmployeeController : ControllerBase
{
    private readonly IEmployeeDashboardService _dashboardService;
 
    public EmployeeController(IEmployeeDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }
 
    // Employee Dashboard
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        try
        {
            var employeeIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
 
            if (employeeIdClaim == null)
            {
                return Unauthorized(new
                {
                    Message = "Invalid token. Employee Id not found."
                });
            }
 
            int employeeId = int.Parse(employeeIdClaim.Value);
 
            var result = await _dashboardService.GetDashboardAsync(employeeId);
 
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                Message = "Something went wrong.",
                Error = ex.Message
            });
        }
    }
 
    // Employee Availability Calendar
    [HttpGet("availability")]
    public async Task<IActionResult> GetAvailability([FromQuery] DateOnly date)
    {
        try
        {
            var result = await _dashboardService.GetAvailabilityAsync(date);
 
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                Message = "Something went wrong.",
                Error = ex.Message
            });
        }
    }
    [HttpGet("mybookings")]
public async Task<IActionResult> GetMyBookings()
{
    try
    {
        var employeeIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
 
        if (employeeIdClaim == null)
        {
            return Unauthorized(new
            {
                Message = "Invalid token."
            });
        }
 
        int employeeId = int.Parse(employeeIdClaim.Value);
 
        var result = await _dashboardService.GetMyBookingsAsync(employeeId);
 
        return Ok(result);
    }
    catch (Exception ex)
    {
        return StatusCode(500, new
        {
            Message = ex.Message
        });
    }
}
}