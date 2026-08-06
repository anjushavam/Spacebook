using SpaceBook.Application.DTOs.Room;
using SpaceBook.Application.Interfaces;
using SpaceBook.Domain.Entities;

namespace SpaceBook.Application.Services;

public class RoomService : IRoomService
{
    private readonly IRoomRepository _roomRepository;

    public RoomService(IRoomRepository roomRepository)
    {
        _roomRepository = roomRepository;
    }


    public async Task<RoomDashboardDto> GetDashboardAsync()
    {
        return await _roomRepository.GetDashboardAsync();
    }


    public async Task<IEnumerable<RoomDto>> GetAllAsync(RoomFilterDto filter)
    {
        return await _roomRepository.GetAllAsync(filter);
    }


    public async Task<RoomDetailsDto?> GetByIdAsync(int roomId)
    {
        return await _roomRepository.GetByIdAsync(roomId);
    }


    public async Task CreateAsync(CreateRoomDto dto)
    {
        var room = new Room
        {
            RoomTypeId = dto.RoomTypeId,
            RoomName = dto.RoomName,
            Capacity = dto.Capacity,
            Module = dto.Module,
            Status = dto.Status
        };

        await _roomRepository.AddAsync(
            room,
            dto.FacilityIds);
    }


    public async Task UpdateAsync(
        int roomId,
        UpdateRoomDto dto)
    {
        var existingRoom = await _roomRepository
            .GetByIdAsync(roomId);

        if (existingRoom == null)
            throw new KeyNotFoundException("Room not found.");


        var room = new Room
        {
            RoomId = roomId,
            RoomTypeId = dto.RoomTypeId,
            RoomName = dto.RoomName,
            Capacity = dto.Capacity,
            Module = dto.Module,
            Status = dto.Status
        };


        await _roomRepository.UpdateAsync(
            room,
            dto.FacilityIds);
    }


    public async Task DeleteAsync(int roomId)
    {
        var exists = await _roomRepository
            .ExistsAsync(roomId);

        if (!exists)
            throw new KeyNotFoundException("Room not found.");


        await _roomRepository.DeleteAsync(roomId);
    }


    public async Task<bool> UpdateRoomStatusAsync(
        int roomId,
        bool isBlocked)
    {
        return await _roomRepository
            .UpdateRoomStatusAsync(
                roomId,
                isBlocked);
    }
    public async Task BulkCreateAsync(BulkCreateRoomDto dto)
{
    for (int i = 1; i <= dto.Count; i++)
    {
        var room = new Room
        {
            RoomTypeId = dto.RoomTypeId,
            RoomName = $"Room-{i:D2}",
            Capacity = dto.Capacity,
            Module = dto.Module,
            Status = dto.Status
        };
 
        await _roomRepository.AddAsync(room, dto.FacilityIds);
    }
}
}