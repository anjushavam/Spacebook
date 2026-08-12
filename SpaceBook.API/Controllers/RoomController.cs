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
        await _roomService.UpdateAsync(id, dto);

        return Ok(new
        {
            message = "Room updated successfully."
        });
    }

    // =========================================================
    // DELETE: api/admin/rooms/{id}
    // =========================================================

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _roomService.DeleteAsync(id);

        return Ok(new
        {
            message = "Room deleted successfully."
        });
    }

    // =========================================================
    // PATCH: api/admin/rooms/{id}/status
    // =========================================================

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateRoomStatus(
        int id,
        [FromBody] UpdateRoomStatusDto dto)
    {
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
        await _roomService.BulkCreateAsync(dto);

        return Ok(new
        {
            message = $"{dto.Count} rooms created successfully."
        });
    }
}