using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SpaceBook.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HotseatController : ControllerBase
    {
        // GET: api/hotseat?date=08-18-2026&city=Coimbatore&building=ELCOT&module=Module 1
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SeatMapDto>>> GetOfficeMap(
            [FromQuery] string date,
            [FromQuery] string city,
            [FromQuery] string building,
            [FromQuery] string module)
        {
            // TODO: Query database using the exact filters shown in the UI dropdowns
            var seatMap = new List<SeatMapDto>
            {
                new SeatMapDto { SeatNumber = 1, Section = "A", Row = "R1", Status = "Vacant" },
                new SeatMapDto { SeatNumber = 2, Section = "A", Row = "R1", Status = "Reserved" },
                new SeatMapDto { SeatNumber = 3, Section = "A", Row = "R1", Status = "Occupied" }
            };

            return Ok(seatMap);
        }
    }

    public class SeatMapDto
    {
        public int SeatNumber { get; set; }
        public string Section { get; set; } // Section A, Section B, Section C
        public string Row { get; set; }     // R1 to R8
        public string Status { get; set; }  // Vacant, Occupied, Reserved
    }
}