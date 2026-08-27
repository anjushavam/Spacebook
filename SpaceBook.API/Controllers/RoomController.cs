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
        // -----------------------------------------------------
        // Validate Room ID
        // -----------------------------------------------------

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
        // -----------------------------------------------------
        // Request body validation
        // -----------------------------------------------------

        if (dto == null)
        {
            return BadRequest(new
            {
                message = "Request body is required."
            });
        }


        // -----------------------------------------------------
        // TC080 FIX
        // Room name is mandatory
        // -----------------------------------------------------

        if (string.IsNullOrWhiteSpace(dto.RoomName))
        {
            return BadRequest(new
            {
                message = "Room name is required."
            });
        }


        // -----------------------------------------------------
        // Room Type validation
        // -----------------------------------------------------

        if (dto.RoomTypeId <= 0)
        {
            return BadRequest(new
            {
                message = "Room type is required."
            });
        }


        // -----------------------------------------------------
        // Module validation
        // -----------------------------------------------------

        if (dto.ModuleId <= 0)
        {
            return BadRequest(new
            {
                message = "Module is required."
            });
        }


        // -----------------------------------------------------
        // Capacity validation
        // -----------------------------------------------------

        if (dto.Capacity <= 0)
        {
            return BadRequest(new
            {
                message = "Room capacity must be greater than zero."
            });
        }


        // -----------------------------------------------------
        // Status validation
        // -----------------------------------------------------

        if (string.IsNullOrWhiteSpace(dto.Status))
        {
            return BadRequest(new
            {
                message = "Room status is required."
            });
        }

        if (!string.Equals(dto.Status.Trim(), "Available", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(dto.Status.Trim(), "Maintenance", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                message = "Room status must be either 'Available' or 'Maintenance'."
            });
        }


        // -----------------------------------------------------
        // Create only after validation succeeds
        // -----------------------------------------------------

        try
        {
            await _roomService.CreateAsync(dto);

            return Ok(new
            {
                message = "Room created successfully."
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new
            {
                message = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }


    // =========================================================
    // PUT: api/admin/rooms/{id}
    // =========================================================

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateRoomDto dto)
    {
        // -----------------------------------------------------
        // Validate Room ID
        // -----------------------------------------------------

        if (id <= 0)
        {
            return BadRequest(new
            {
                message = "Room ID must be greater than zero."
            });
        }


        // -----------------------------------------------------
        // Validate request body
        // -----------------------------------------------------

        if (dto == null)
        {
            return BadRequest(new
            {
                message = "Request body is required."
            });
        }


        // -----------------------------------------------------
        // Validate Room Name
        // -----------------------------------------------------

        if (string.IsNullOrWhiteSpace(dto.RoomName))
        {
            return BadRequest(new
            {
                message = "Room name is required."
            });
        }


        // -----------------------------------------------------
        // Validate Room Type
        // -----------------------------------------------------

        if (dto.RoomTypeId <= 0)
        {
            return BadRequest(new
            {
                message = "Room type is required."
            });
        }


        // -----------------------------------------------------
        // Validate Module
        // -----------------------------------------------------

        if (dto.ModuleId <= 0)
        {
            return BadRequest(new
            {
                message = "Module is required."
            });
        }


        // -----------------------------------------------------
        // Validate Capacity
        // -----------------------------------------------------

        if (dto.Capacity <= 0)
        {
            return BadRequest(new
            {
                message = "Room capacity must be greater than zero."
            });
        }


        // -----------------------------------------------------
        // Validate Status
        // -----------------------------------------------------

        if (string.IsNullOrWhiteSpace(dto.Status))
        {
            return BadRequest(new
            {
                message = "Room status is required."
            });
        }

        if (!string.Equals(dto.Status.Trim(), "Available", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(dto.Status.Trim(), "Maintenance", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                message = "Room status must be either 'Available' or 'Maintenance'."
            });
        }


        // -----------------------------------------------------
        // TC084 FIX
        // Check whether room exists before updating
        // -----------------------------------------------------

        var existingRoom = await _roomService.GetByIdAsync(id);

        if (existingRoom == null)
        {
            return NotFound(new
            {
                message = "Room not found."
            });
        }


        // -----------------------------------------------------
        // Update room
        // -----------------------------------------------------

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
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }


    // =========================================================
    // DELETE: api/admin/rooms/{id}
    // =========================================================

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        // -----------------------------------------------------
        // Validate Room ID
        // -----------------------------------------------------

        if (id <= 0)
        {
            return BadRequest(new
            {
                message = "Room ID must be greater than zero."
            });
        }


        // -----------------------------------------------------
        // Delete room
        // -----------------------------------------------------

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
        // -----------------------------------------------------
        // Validate Room ID
        // -----------------------------------------------------

        if (id <= 0)
        {
            return BadRequest(new
            {
                message = "Room ID must be greater than zero."
            });
        }


        // -----------------------------------------------------
        // Validate request body
        // -----------------------------------------------------

        if (dto == null)
        {
            return BadRequest(new
            {
                message = "Request body is required."
            });
        }

        if (string.IsNullOrWhiteSpace(dto.Status) && !dto.IsBlocked.HasValue)
        {
            return BadRequest(new
            {
                message = "Either status ('Available' or 'Maintenance') or isBlocked must be provided."
            });
        }

        if (!string.IsNullOrWhiteSpace(dto.Status) &&
            !string.Equals(dto.Status.Trim(), "Available", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(dto.Status.Trim(), "Maintenance", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                message = "Room status must be either 'Available' or 'Maintenance'."
            });
        }

        // -----------------------------------------------------
        // Update room status
        // -----------------------------------------------------

        try
        {
            var result =
                await _roomService.UpdateRoomStatusAsync(
                    id,
                    dto.Status,
                    dto.IsBlocked);

            if (!result)
            {
                return NotFound(new
                {
                    message = "Room not found."
                });
            }

            var updatedRoom = await _roomService.GetByIdAsync(id);

            return Ok(new
            {
                message = "Room status updated successfully.",
                roomId = id,
                status = updatedRoom?.Status,
                isBlocked = updatedRoom?.IsBlocked
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }


    // =========================================================
    // POST: api/admin/rooms/bulk
    // =========================================================

    [HttpPost("bulk")]
    public async Task<IActionResult> BulkCreateRooms(
        [FromBody] BulkCreateRoomDto dto)
    {
        // -----------------------------------------------------
        // Request body validation
        // -----------------------------------------------------

        if (dto == null)
        {
            return BadRequest(new
            {
                message = "Request body is required."
            });
        }


        // -----------------------------------------------------
        // TC086 FIX
        // Count must be greater than zero
        // -----------------------------------------------------

        if (dto.Count <= 0)
        {
            return BadRequest(new
            {
                message = "Room count must be greater than zero."
            });
        }


        // -----------------------------------------------------
        // Room Type validation
        // -----------------------------------------------------

        if (dto.RoomTypeId <= 0)
        {
            return BadRequest(new
            {
                message = "Room type is required."
            });
        }


        // -----------------------------------------------------
        // Module validation
        // -----------------------------------------------------

        if (dto.ModuleId <= 0)
        {
            return BadRequest(new
            {
                message = "Module is required."
            });
        }


        // -----------------------------------------------------
        // Capacity validation
        // -----------------------------------------------------

        if (dto.Capacity <= 0)
        {
            return BadRequest(new
            {
                message = "Room capacity must be greater than zero."
            });
        }


        // -----------------------------------------------------
        // Status validation
        // -----------------------------------------------------

        if (string.IsNullOrWhiteSpace(dto.Status))
        {
            return BadRequest(new
            {
                message = "Room status is required."
            });
        }

        if (!string.Equals(dto.Status.Trim(), "Available", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(dto.Status.Trim(), "Maintenance", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                message = "Room status must be either 'Available' or 'Maintenance'."
            });
        }


        // -----------------------------------------------------
        // Create rooms only after validation succeeds
        // -----------------------------------------------------

        try
        {
            await _roomService.BulkCreateAsync(dto);

            return Ok(new
            {
                message = $"{dto.Count} rooms created successfully."
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new
            {
                message = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }
}