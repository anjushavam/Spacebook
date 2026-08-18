using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpaceBook.Application.DTOs.Hotseat;
using SpaceBook.Infrastructure.Data;

namespace SpaceBook.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HotseatController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public HotseatController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<HotseatSeatDto>>> GetOfficeMap(
            [FromQuery] string date,
            [FromQuery] string city,
            [FromQuery] string building,
            [FromQuery] string module)
        {
            var seats = await _context.Seats
                .Where(s => s.IsActive)
                .OrderBy(s => s.ModuleId)
                .ThenBy(s => s.Section)
                .ThenBy(s => s.RowNumber)
                .ThenBy(s => s.ColumnNumber)
                .Select(s => new HotseatSeatDto
                {
                    SeatNumber = s.SeatNumber,
                    Section = s.Section ?? "",
                    Row = s.RowNumber,
                    Status = "Vacant"
                })
                .ToListAsync();

            return Ok(seats);
        }
    }
}