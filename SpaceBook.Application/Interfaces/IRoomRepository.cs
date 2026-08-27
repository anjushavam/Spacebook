using SpaceBook.Application.DTOs.Room;
using SpaceBook.Domain.Entities;

namespace SpaceBook.Application.Interfaces;

public interface IRoomRepository
{
    // Dashboard
    Task<RoomDashboardDto> GetDashboardAsync();

    // Get all rooms
    Task<IEnumerable<RoomDto>> GetAllAsync(
        RoomFilterDto filter);

    // Get room by id
    Task<RoomDetailsDto?> GetByIdAsync(
        int roomId);

    // Get rooms by module
    Task<List<RoomDetailsDto>> GetRoomsByModuleAsync(
        string module);

    // Create
    Task AddAsync(
        Room room,
        List<int> facilityIds);

    // Update
    Task UpdateAsync(
        Room room,
        List<int> facilityIds);

    // Delete
    Task DeleteAsync(
        int roomId);

    // Exists
    Task<bool> ExistsAsync(
        int roomId);

    // Update status
    Task<bool> UpdateRoomStatusAsync(
        int roomId,
        string? status = null,
        bool? isBlocked = null);
}