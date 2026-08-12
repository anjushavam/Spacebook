using Microsoft.EntityFrameworkCore;
using SpaceBook.Application.DTOs.Room;
using SpaceBook.Application.Interfaces;
using SpaceBook.Domain.Entities;
using SpaceBook.Infrastructure.Data;

namespace SpaceBook.Infrastructure.Repositories;

public class RoomRepository : IRoomRepository
{
    private readonly ApplicationDbContext _context;

    public RoomRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    // =========================================================
    // Dashboard
    // =========================================================

    public async Task<RoomDashboardDto> GetDashboardAsync()
    {
        return new RoomDashboardDto
        {
            TotalRooms = await _context.Rooms.CountAsync(),

            AvailableRooms = await _context.Rooms.CountAsync(
                r => r.Status == "Available" && !r.IsBlocked),

            BookedRooms = await _context.Rooms.CountAsync(
                r => r.Status == "Booked" && !r.IsBlocked)
        };
    }

    // =========================================================
    // Get All Rooms
    // =========================================================

    public async Task<IEnumerable<RoomDto>> GetAllAsync(
        RoomFilterDto filter)
    {
        var query = _context.Rooms
            .Include(r => r.RoomType)
            .Include(r => r.RoomFacilities)
                .ThenInclude(rf => rf.Facility)
            .Where(r =>
                r.Status != "Blocked" &&
                !r.IsBlocked)
            .AsQueryable();

        // Search
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            query = query.Where(r =>
                r.RoomName.Contains(filter.Search) ||
                r.Module.Contains(filter.Search));
        }

        // Room Type Filter
        if (filter.RoomTypeId.HasValue)
        {
            query = query.Where(r =>
                r.RoomTypeId == filter.RoomTypeId.Value);
        }

        // Status Filter
        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            query = query.Where(r =>
                r.Status == filter.Status);
        }

        // Return rooms INCLUDING FACILITIES
        return await query
            .Select(r => new RoomDto
            {
                RoomId = r.RoomId,

                RoomName = r.RoomName,

                RoomType = r.RoomType!.TypeName,

                Capacity = r.Capacity,

                Module = r.Module,

                Status = r.Status,

                Facilities = r.RoomFacilities
                    .Select(rf => rf.Facility!.FacilityName)
                    .ToList()
            })
            .ToListAsync();
    }

    // =========================================================
    // Get Room By ID
    // =========================================================

    public async Task<RoomDetailsDto?> GetByIdAsync(
        int roomId)
    {
        return await _context.Rooms
            .Include(r => r.RoomType)
            .Include(r => r.RoomFacilities)
                .ThenInclude(rf => rf.Facility)
            .Where(r =>
                r.RoomId == roomId &&
                r.Status != "Blocked" &&
                !r.IsBlocked)
            .Select(r => new RoomDetailsDto
            {
                RoomId = r.RoomId,

                RoomTypeId = r.RoomTypeId,

                RoomName = r.RoomName,

                Capacity = r.Capacity,

                Module = r.Module,

                Status = r.Status,

                Facilities = r.RoomFacilities
                    .Select(rf => rf.Facility!.FacilityName)
                    .ToList()
            })
            .FirstOrDefaultAsync();
    }

    // =========================================================
    // Create Room
    // =========================================================

    public async Task AddAsync(
        Room room,
        List<int> facilityIds)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            // -----------------------------------------------
            // 1. Add Room
            // -----------------------------------------------

            await _context.Rooms.AddAsync(room);

            await _context.SaveChangesAsync();

            // At this point RoomId has been generated.
            // Example:
            // room.RoomId = 15

            // -----------------------------------------------
            // 2. Add Facilities
            // -----------------------------------------------

            if (facilityIds != null &&
                facilityIds.Count > 0)
            {
                foreach (var facilityId in facilityIds)
                {
                    var facilityExists =
                        await _context.Facilities.AnyAsync(
                            f => f.FacilityId == facilityId);

                    if (!facilityExists)
                    {
                        throw new KeyNotFoundException(
                            $"Facility with ID {facilityId} not found.");
                    }

                    await _context.RoomFacilities.AddAsync(
                        new RoomFacility
                        {
                            RoomId = room.RoomId,
                            FacilityId = facilityId
                        });
                }

                await _context.SaveChangesAsync();
            }

            // -----------------------------------------------
            // 3. Commit
            // -----------------------------------------------

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // =========================================================
    // Update Room
    // =========================================================

    public async Task UpdateAsync(
        Room room,
        List<int> facilityIds)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            // -----------------------------------------------
            // 1. Find existing room
            // -----------------------------------------------

            var existingRoom =
                await _context.Rooms
                    .FirstOrDefaultAsync(
                        r => r.RoomId == room.RoomId);

            if (existingRoom == null)
            {
                throw new KeyNotFoundException(
                    "Room not found.");
            }

            // -----------------------------------------------
            // 2. Update room fields
            // -----------------------------------------------

            existingRoom.RoomTypeId = room.RoomTypeId;

            existingRoom.RoomName = room.RoomName;

            existingRoom.Capacity = room.Capacity;

            existingRoom.Module = room.Module;

            existingRoom.Status = room.Status;

            // -----------------------------------------------
            // 3. Remove old facilities
            // -----------------------------------------------

            var existingFacilities =
                await _context.RoomFacilities
                    .Where(x =>
                        x.RoomId == room.RoomId)
                    .ToListAsync();

            _context.RoomFacilities.RemoveRange(
                existingFacilities);

            await _context.SaveChangesAsync();

            // -----------------------------------------------
            // 4. Add new facilities
            // -----------------------------------------------

            if (facilityIds != null &&
                facilityIds.Count > 0)
            {
                foreach (var facilityId in facilityIds)
                {
                    var facilityExists =
                        await _context.Facilities.AnyAsync(
                            f => f.FacilityId == facilityId);

                    if (!facilityExists)
                    {
                        throw new KeyNotFoundException(
                            $"Facility with ID {facilityId} not found.");
                    }

                    await _context.RoomFacilities.AddAsync(
                        new RoomFacility
                        {
                            RoomId = room.RoomId,
                            FacilityId = facilityId
                        });
                }

                await _context.SaveChangesAsync();
            }

            // -----------------------------------------------
            // 5. Commit
            // -----------------------------------------------

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // =========================================================
    // Delete / Block Room
    // =========================================================

    public async Task DeleteAsync(int roomId)
    {
        var room =
            await _context.Rooms.FindAsync(roomId);

        if (room == null)
        {
            return;
        }

        room.Status = "Blocked";
        room.IsBlocked = true;

        await _context.SaveChangesAsync();
    }

    // =========================================================
    // Check Room Exists
    // =========================================================

    public async Task<bool> ExistsAsync(int roomId)
    {
        return await _context.Rooms.AnyAsync(
            r =>
                r.RoomId == roomId &&
                r.Status != "Blocked" &&
                !r.IsBlocked);
    }

    // =========================================================
    // Block / Unblock Room
    // =========================================================

    public async Task<bool> UpdateRoomStatusAsync(
        int roomId,
        bool isBlocked)
    {
        var room =
            await _context.Rooms
                .FirstOrDefaultAsync(
                    r => r.RoomId == roomId);

        if (room == null)
        {
            return false;
        }

        room.IsBlocked = isBlocked;

        if (isBlocked)
        {
            room.Status = "Blocked";
        }
        else
        {
            room.Status = "Available";
        }

        await _context.SaveChangesAsync();

        return true;
    }

    // =========================================================
    // Get Rooms By Module
    // =========================================================

    public async Task<List<RoomDetailsDto>>
        GetRoomsByModuleAsync(string module)
    {
        return await _context.Rooms
            .Include(r => r.RoomType)
            .Include(r => r.RoomFacilities)
                .ThenInclude(rf => rf.Facility)
            .Where(r =>
                r.Status != "Blocked" &&
                !r.IsBlocked &&
                r.Module == module)
            .Select(r => new RoomDetailsDto
            {
                RoomId = r.RoomId,

                RoomTypeId = r.RoomTypeId,

                RoomName = r.RoomName,

                Capacity = r.Capacity,

                Module = r.Module,

                Status = r.Status,

                Facilities = r.RoomFacilities
                    .Select(rf =>
                        rf.Facility!.FacilityName)
                    .ToList()
            })
            .ToListAsync();
    }
}