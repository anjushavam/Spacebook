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

    // =========================================================
    // GET ROOM DASHBOARD
    // =========================================================

    public async Task<RoomDashboardDto> GetDashboardAsync()
    {
        return await _roomRepository.GetDashboardAsync();
    }

    // =========================================================
    // GET ALL ROOMS
    // =========================================================

    public async Task<IEnumerable<RoomDto>> GetAllAsync(
        RoomFilterDto filter)
    {
        return await _roomRepository.GetAllAsync(filter);
    }

    // =========================================================
    // GET ROOM BY ID
    // =========================================================

    public async Task<RoomDetailsDto?> GetByIdAsync(int roomId)
    {
        return await _roomRepository.GetByIdAsync(roomId);
    }

    // =========================================================
    // CREATE ROOM
    // =========================================================

    public async Task CreateAsync(CreateRoomDto dto)
    {
        // =====================================================
        // VALIDATE ROOM NAME
        // =====================================================

        if (string.IsNullOrWhiteSpace(dto.RoomName))
        {
            throw new ArgumentException(
                "Room name is required.");
        }

        dto.RoomName = dto.RoomName.Trim();

        // =====================================================
        // VALIDATE ROOM NAME LENGTH
        // =====================================================

        if (dto.RoomName.Length > 100)
        {
            throw new ArgumentException(
                "Room name cannot exceed 100 characters.");
        }

        // =====================================================
        // VALIDATE ROOM NUMBER
        // =====================================================

        if (string.IsNullOrWhiteSpace(dto.RoomNumber))
        {
            throw new ArgumentException(
                "Room number is required.");
        }

        dto.RoomNumber = dto.RoomNumber.Trim();

        if (dto.RoomNumber.Length > 50)
        {
            throw new ArgumentException(
                "Room number cannot exceed 50 characters.");
        }

        // =====================================================
        // VALIDATE CAPACITY
        // =====================================================

        if (dto.Capacity <= 0)
        {
            throw new ArgumentException(
                "Room capacity must be greater than zero.");
        }

        // =====================================================
        // VALIDATE ROOM TYPE
        // =====================================================

        if (dto.RoomTypeId <= 0)
        {
            throw new ArgumentException(
                "Room type is required.");
        }

        // =====================================================
        // VALIDATE MODULE
        // =====================================================

        if (dto.ModuleId <= 0)
        {
            throw new ArgumentException(
                "Module is required.");
        }

        // =====================================================
        // VALIDATE STATUS
        // =====================================================

        if (string.IsNullOrWhiteSpace(dto.Status))
        {
            dto.Status = "Available";
        }
        else
        {
            dto.Status = dto.Status.Trim();
            if (!string.Equals(dto.Status, "Available", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(dto.Status, "Maintenance", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    "Room status must be either 'Available' or 'Maintenance'.");
            }

            dto.Status = string.Equals(dto.Status, "Maintenance", StringComparison.OrdinalIgnoreCase)
                ? "Maintenance"
                : "Available";
        }

        // =====================================================
        // CREATE ROOM ENTITY
        // =====================================================

        var room = new Room
        {
            RoomNumber = dto.RoomNumber,

            RoomTypeId = dto.RoomTypeId,

            RoomName = dto.RoomName,

            Capacity = dto.Capacity,

            ModuleId = dto.ModuleId,

            Status = dto.Status,

            IsBlocked = false
        };

        // =====================================================
        // SAVE ROOM
        // =====================================================

        await _roomRepository.AddAsync(
            room,
            dto.FacilityIds ?? new List<int>());
    }

    // =========================================================
    // UPDATE ROOM
    // =========================================================

    public async Task UpdateAsync(
        int roomId,
        UpdateRoomDto dto)
    {
        // =====================================================
        // VALIDATE ROOM NAME
        // =====================================================

        if (string.IsNullOrWhiteSpace(dto.RoomName))
        {
            throw new ArgumentException(
                "Room name is required.");
        }

        dto.RoomName = dto.RoomName.Trim();

        if (dto.RoomName.Length > 100)
        {
            throw new ArgumentException(
                "Room name cannot exceed 100 characters.");
        }

        // =====================================================
        // VALIDATE ROOM NUMBER
        // =====================================================

        if (string.IsNullOrWhiteSpace(dto.RoomNumber))
        {
            throw new ArgumentException(
                "Room number is required.");
        }

        dto.RoomNumber = dto.RoomNumber.Trim();

        if (dto.RoomNumber.Length > 50)
        {
            throw new ArgumentException(
                "Room number cannot exceed 50 characters.");
        }

        // =====================================================
        // VALIDATE CAPACITY
        // =====================================================

        if (dto.Capacity <= 0)
        {
            throw new ArgumentException(
                "Room capacity must be greater than zero.");
        }

        // =====================================================
        // VALIDATE ROOM TYPE
        // =====================================================

        if (dto.RoomTypeId <= 0)
        {
            throw new ArgumentException(
                "Room type is required.");
        }

        // =====================================================
        // VALIDATE MODULE
        // =====================================================

        if (dto.ModuleId <= 0)
        {
            throw new ArgumentException(
                "Module is required.");
        }

        // =====================================================
        // VALIDATE STATUS
        // =====================================================

        if (string.IsNullOrWhiteSpace(dto.Status))
        {
            dto.Status = "Available";
        }
        else
        {
            dto.Status = dto.Status.Trim();
            if (!string.Equals(dto.Status, "Available", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(dto.Status, "Maintenance", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    "Room status must be either 'Available' or 'Maintenance'.");
            }

            dto.Status = string.Equals(dto.Status, "Maintenance", StringComparison.OrdinalIgnoreCase)
                ? "Maintenance"
                : "Available";
        }

        // =====================================================
        // CHECK EXISTING ROOM
        // =====================================================

        var existingRoom =
            await _roomRepository.GetByIdAsync(roomId);

        if (existingRoom == null)
        {
            throw new KeyNotFoundException(
                "Room not found.");
        }

        // =====================================================
        // CREATE UPDATED ROOM
        // =====================================================

        var room = new Room
        {
            RoomId = roomId,

            RoomNumber = dto.RoomNumber,

            RoomTypeId = dto.RoomTypeId,

            RoomName = dto.RoomName,

            Capacity = dto.Capacity,

            ModuleId = dto.ModuleId,

            Status = dto.Status,

            IsBlocked = existingRoom.IsBlocked
        };

        await _roomRepository.UpdateAsync(
            room,
            dto.FacilityIds ?? new List<int>());
    }

    // =========================================================
    // DELETE ROOM
    // =========================================================

    public async Task DeleteAsync(int roomId)
    {
        var exists =
            await _roomRepository.ExistsAsync(roomId);

        if (!exists)
        {
            throw new KeyNotFoundException(
                "Room not found.");
        }

        await _roomRepository.DeleteAsync(roomId);
    }

    // =========================================================
    // BLOCK / UNBLOCK ROOM
    // =========================================================

    public async Task<bool> UpdateRoomStatusAsync(
        int roomId,
        bool isBlocked)
    {
        return await _roomRepository
            .UpdateRoomStatusAsync(
                roomId,
                isBlocked);
    }

    // =========================================================
    // BULK CREATE ROOMS
    // =========================================================

    public async Task BulkCreateAsync(
        BulkCreateRoomDto dto)
    {
        if (dto.Count <= 0)
        {
            throw new ArgumentException(
                "Room count must be greater than zero.");
        }

        if (dto.Capacity <= 0)
        {
            throw new ArgumentException(
                "Room capacity must be greater than zero.");
        }

        if (dto.RoomTypeId <= 0)
        {
            throw new ArgumentException(
                "Room type is required.");
        }

        if (dto.ModuleId <= 0)
        {
            throw new ArgumentException(
                "Module is required.");
        }

        // =====================================================
        // IMPORTANT:
        // Bulk room numbers need a real room-number strategy.
        // This generates placeholder values based on sequence.
        // Replace this logic if your business format is different.
        // =====================================================

        var roomStatus = "Available";
        if (!string.IsNullOrWhiteSpace(dto.Status))
        {
            var trimmedStatus = dto.Status.Trim();
            if (!string.Equals(trimmedStatus, "Available", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(trimmedStatus, "Maintenance", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    "Room status must be either 'Available' or 'Maintenance'.");
            }

            roomStatus = string.Equals(trimmedStatus, "Maintenance", StringComparison.OrdinalIgnoreCase)
                ? "Maintenance"
                : "Available";
        }

        for (int i = 1; i <= dto.Count; i++)
        {
            var room = new Room
            {
                RoomNumber = $"ROOM-{i:D3}",

                RoomTypeId = dto.RoomTypeId,

                RoomName = $"Room-{i:D2}",

                Capacity = dto.Capacity,

                ModuleId = dto.ModuleId,

                Status = roomStatus,

                IsBlocked = false
            };

            await _roomRepository.AddAsync(
                room,
                dto.FacilityIds ?? new List<int>());
        }
    }
}