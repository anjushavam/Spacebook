using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using SpaceBook.Application.DTOs.Room;

using SpaceBook.Application.Interfaces;
 
namespace SpaceBook.API.Controllers;
 
[ApiController]

[Route("api/admin/rooms")]

[Authorize(Roles = "Admin")]

public class RoomController : ControllerBase

{

    private readonly IRoomService _roomService;
 
    public RoomController(IRoomService roomService)

    {

        _roomService = roomService;

    }
 
    // =========================================================

    // GET: api/admin/rooms/dashboard

    // =========================================================
 
    [HttpGet("dashboard")]

    public async Task<IActionResult> GetDashboard()

    {

        var result = await _roomService.GetDashboardAsync();
 
        return Ok(result);

    }
 
    // =========================================================

    // GET: api/admin/rooms

    // =========================================================
 
    [HttpGet]

    public async Task<IActionResult> GetAll(

        [FromQuery] RoomFilterDto filter)

    {

        var rooms = await _roomService.GetAllAsync(filter);
 
        return Ok(rooms);

    }
 
    // =========================================================

    // GET: api/admin/rooms/{id}

    // =========================================================
 
    [HttpGet("{id:int}")]

    public async Task<IActionResult> GetById(int id)

    {

        if (id <= 0)

        {

            return BadRequest(new

            {

                message = "Room ID must be greater than zero."

            });

        }
 
        var room = await _roomService.GetByIdAsync(id);
 
        if (room == null)

        {

            return NotFound(new

            {

                message = "Room not found."

            });

        }
 
        return Ok(room);

    }
 
    // =========================================================

    // POST: api/admin/rooms

    // =========================================================
 
    [HttpPost]

    public async Task<IActionResult> Create(

        [FromBody] CreateRoomDto dto)

    {

        // ---------------------------------------------------------

        // Validate request body

        // ---------------------------------------------------------
 
        if (dto == null)

        {

            return BadRequest(new

            {

                message = "Request body is required."

            });

        }
 
        // ---------------------------------------------------------

        // TC080 FIX

        // roomName is mandatory

        // ---------------------------------------------------------
 
        if (string.IsNullOrWhiteSpace(dto.RoomName))

        {

            return BadRequest(new

            {

                message = "Room name is required."

            });

        }
 
        // ---------------------------------------------------------

        // Validate Room Type

        // ---------------------------------------------------------
 
        if (dto.RoomTypeId <= 0)

        {

            return BadRequest(new

            {

                message = "Room type is required."

            });

        }
 
        // ---------------------------------------------------------

        // Validate Module

        // ---------------------------------------------------------
 
        if (dto.ModuleId <= 0)

        {

            return BadRequest(new

            {

                message = "Module is required."

            });

        }
 
        // ---------------------------------------------------------

        // Validate Capacity

        // ---------------------------------------------------------
 
        if (dto.Capacity <= 0)

        {

            return BadRequest(new

            {

                message = "Room capacity must be greater than zero."

            });

        }
 
        // ---------------------------------------------------------

        // Validate Status

        // ---------------------------------------------------------
 
        if (string.IsNullOrWhiteSpace(dto.Status))

        {

            return BadRequest(new

            {

                message = "Room status is required."

            });

        }
 
        // ---------------------------------------------------------

        // Create room only after validation succeeds

        // ---------------------------------------------------------
 
        await _roomService.CreateAsync(dto);
 
        return Ok(new

        {

            message = "Room created successfully."

        });

    }
 
    // =========================================================

    // PUT: api/admin/rooms/{id}

    // =========================================================
 
    [HttpPut("{id:int}")]

    public async Task<IActionResult> Update(

        int id,

        [FromBody] UpdateRoomDto dto)

    {

        if (id <= 0)

        {

            return BadRequest(new

            {

                message = "Room ID must be greater than zero."

            });

        }
 
        if (dto == null)

        {

            return BadRequest(new

            {

                message = "Request body is required."

            });

        }
 
        // ---------------------------------------------------------

        // Validate mandatory room name

        // ---------------------------------------------------------
 
        if (string.IsNullOrWhiteSpace(dto.RoomName))

        {

            return BadRequest(new

            {

                message = "Room name is required."

            });

        }
 
        // ---------------------------------------------------------

        // Validate Room Type

        // ---------------------------------------------------------
 
        if (dto.RoomTypeId <= 0)

        {

            return BadRequest(new

            {

                message = "Room type is required."

            });

        }
 
        // ---------------------------------------------------------

        // Validate Module

        // ---------------------------------------------------------
 
        if (dto.ModuleId <= 0)

        {

            return BadRequest(new

            {

                message = "Module is required."

            });

        }
 
        // ---------------------------------------------------------

        // Validate Capacity

        // ---------------------------------------------------------
 
        if (dto.Capacity <= 0)

        {

            return BadRequest(new

            {

                message = "Room capacity must be greater than zero."

            });

        }
 
        // ---------------------------------------------------------

        // Validate Status

        // ---------------------------------------------------------
 
        if (string.IsNullOrWhiteSpace(dto.Status))

        {

            return BadRequest(new

            {

                message = "Room status is required."

            });

        }
 
        try

        {

            await _roomService.UpdateAsync(id, dto);
 
            return Ok(new

            {

                message = "Room updated successfully."

            });

        }

        catch (KeyNotFoundException)

        {

            return NotFound(new

            {

                message = "Room not found."

            });

        }

    }
 
    // =========================================================

    // DELETE: api/admin/rooms/{id}

    // =========================================================
 
    [HttpDelete("{id:int}")]

    public async Task<IActionResult> Delete(int id)

    {

        if (id <= 0)

        {

            return BadRequest(new

            {

                message = "Room ID must be greater than zero."

            });

        }
 
        try

        {

            await _roomService.DeleteAsync(id);
 
            return Ok(new

            {

                message = "Room deleted successfully."

            });

        }

        catch (KeyNotFoundException)

        {

            return NotFound(new

            {

                message = "Room not found."

            });

        }

    }
 
    // =========================================================

    // PATCH: api/admin/rooms/{id}/status

    // =========================================================
 
    [HttpPatch("{id:int}/status")]

    public async Task<IActionResult> UpdateRoomStatus(

        int id,

        [FromBody] UpdateRoomStatusDto dto)

    {

        if (id <= 0)

        {

            return BadRequest(new

            {

                message = "Room ID must be greater than zero."

            });

        }
 
        if (dto == null)

        {

            return BadRequest(new

            {

                message = "Request body is required."

            });

        }
 
        var result = await _roomService.UpdateRoomStatusAsync(

            id,

            dto.IsBlocked);
 
        if (!result)

        {

            return NotFound(new

            {

                message = "Room not found."

            });

        }
 
        return Ok(new

        {

            message = dto.IsBlocked

                ? "Room blocked successfully."

                : "Room unblocked successfully."

        });

    }
 
    // =========================================================

    // POST: api/admin/rooms/bulk

    // =========================================================
 
    [HttpPost("bulk")]

    public async Task<IActionResult> BulkCreateRooms(

        [FromBody] BulkCreateRoomDto dto)

    {

        if (dto == null)

        {

            return BadRequest(new

            {

                message = "Request body is required."

            });

        }
 
        // ---------------------------------------------------------

        // Validate count

        // ---------------------------------------------------------
 
        if (dto.Count <= 0)

        {

            return BadRequest(new

            {

                message = "Room count must be greater than zero."

            });

        }
 
        // ---------------------------------------------------------

        // Validate room type

        // ---------------------------------------------------------
 
        if (dto.RoomTypeId <= 0)

        {

            return BadRequest(new

            {

                message = "Room type is required."

            });

        }
 
        // ---------------------------------------------------------

        // Validate module

        // ---------------------------------------------------------
 
        if (dto.ModuleId <= 0)

        {

            return BadRequest(new

            {

                message = "Module is required."

            });

        }
 
        // ---------------------------------------------------------

        // Validate capacity

        // ---------------------------------------------------------
 
        if (dto.Capacity <= 0)

        {

            return BadRequest(new

            {

                message = "Room capacity must be greater than zero."

            });

        }
 
        // ---------------------------------------------------------

        // Validate status

        // ---------------------------------------------------------
 
        if (string.IsNullOrWhiteSpace(dto.Status))

        {

            return BadRequest(new

            {

                message = "Room status is required."

            });

        }
 
        await _roomService.BulkCreateAsync(dto);
 
        return Ok(new

        {

            message = $"{dto.Count} rooms created successfully."

        });

    }

}
 