using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpaceBook.Application.DTOs.Room;
using SpaceBook.Application.Interfaces;

namespace SpaceBook.API.Controllers;

[Route("api/admin/rooms")]
[ApiController]
[Authorize(Roles = "Admin")]
public class RoomController : ControllerBase
{
    private readonly IRoomService _roomService;

    public RoomController(IRoomService roomService)
    {
        _roomService = roomService;
    }

    // GET: api/admin/rooms/dashboard
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var result = await _roomService.GetDashboardAsync();
        return Ok(result);
    }

    // GET: api/admin/rooms
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] RoomFilterDto filter)
    {
        var rooms = await _roomService.GetAllAsync(filter);
        return Ok(rooms);
    }

    // GET: api/admin/rooms/1
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var room = await _roomService.GetByIdAsync(id);

        if (room == null)
            return NotFound(new { message = "Room not found." });

        return Ok(room);
    }

    // POST: api/admin/rooms
    [HttpPost]
    public async Task<IActionResult> Create(CreateRoomDto dto)
    {
        await _roomService.CreateAsync(dto);

        return Ok(new
        {
            message = "Room created successfully."
        });
    }

    // PUT: api/admin/rooms/1
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateRoomDto dto)
    {
        await _roomService.UpdateAsync(id, dto);

        return Ok(new
        {
            message = "Room updated successfully."
        });
    }

    // DELETE: api/admin/rooms/1
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _roomService.DeleteAsync(id);

        return Ok(new
        {
            message = "Room deleted successfully."
        });
    }
}