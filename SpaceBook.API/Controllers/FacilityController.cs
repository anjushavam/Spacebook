using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpaceBook.Application.Interfaces;

namespace SpaceBook.API.Controllers;

[ApiController]
[Route("api/admin/facilities")]
[Authorize(Roles = "Admin")]
public class FacilityController : ControllerBase
{
    private readonly IFacilityService _facilityService;

    public FacilityController(IFacilityService facilityService)
    {
        _facilityService = facilityService;
    }

    [HttpGet]
    public async Task<IActionResult> GetFacilities()
    {
        try
        {
            var facilities =
                await _facilityService.GetFacilitiesAsync();

            return Ok(facilities);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                Message = "Unable to load facilities.",
                Error = ex.Message
            });
        }
    }
}