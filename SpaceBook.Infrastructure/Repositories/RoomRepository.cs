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
    // DASHBOARD
    // =========================================================

    public async Task<RoomDashboardDto> GetDashboardAsync()
    {
        return new RoomDashboardDto
        {
            TotalRooms =
                await _context.Rooms.CountAsync(),

            AvailableRooms =
                await _context.Rooms.CountAsync(
                    r =>
                        r.Status == "Available" &&
                        !r.IsBlocked),

            BookedRooms =
                await _context.Rooms.CountAsync(
                    r =>
                        r.Status == "Booked" &&
                        !r.IsBlocked)
        };
    }

    // =========================================================
    // GET ALL ROOMS
    // =========================================================

    public async Task<IEnumerable<RoomDto>> GetAllAsync(
        RoomFilterDto filter)
    {
        var query = _context.Rooms
            .AsNoTracking()
            .Include(r => r.RoomType)
            .Include(r => r.Module)
            .Include(r => r.RoomFacilities)
                .ThenInclude(rf => rf.Facility)
            .Where(r =>
                r.Status != "Blocked" &&
                !r.IsBlocked)
            .AsQueryable();

        // -----------------------------------------------------
        // SEARCH
        // -----------------------------------------------------

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();

            query = query.Where(r =>
                r.RoomName.Contains(search) ||
                r.RoomNumber.Contains(search) ||
                (
                    r.Module != null &&
                    r.Module.ModuleName.Contains(search)
                ));
        }

        // -----------------------------------------------------
        // ROOM TYPE FILTER
        // -----------------------------------------------------

        if (filter.RoomTypeId.HasValue)
        {
            query = query.Where(r =>
                r.RoomTypeId ==
                filter.RoomTypeId.Value);
        }

        // -----------------------------------------------------
        // STATUS FILTER
        // -----------------------------------------------------

        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            query = query.Where(r =>
                r.Status == filter.Status);
        }

        // -----------------------------------------------------
        // RETURN ROOMS
        // -----------------------------------------------------

        return await query
            .Select(r => new RoomDto
            {
                RoomId = r.RoomId,

                RoomNumber = r.RoomNumber,

                RoomName = r.RoomName,

                RoomType =
                    r.RoomType != null
                        ? r.RoomType.TypeName
                        : string.Empty,

                Capacity = r.Capacity,

                ModuleId = r.ModuleId,

                Module =
                    r.Module != null
                        ? r.Module.ModuleName
                        : string.Empty,

                Status = r.Status,

                IsBlocked = r.IsBlocked,

                Facilities =
                    r.RoomFacilities
                        .Where(rf =>
                            rf.Facility != null)
                        .Select(rf =>
                            rf.Facility!.FacilityName)
                        .ToList()
            })
            .ToListAsync();
    }

    // =========================================================
    // GET ROOM BY ID
    // =========================================================

    public async Task<RoomDetailsDto?> GetByIdAsync(
        int roomId)
    {
        return await _context.Rooms
            .AsNoTracking()
            .Include(r => r.RoomType)
            .Include(r => r.Module)
            .Include(r => r.RoomFacilities)
                .ThenInclude(rf => rf.Facility)
            .Where(r =>
                r.RoomId == roomId &&
                r.Status != "Blocked" &&
                !r.IsBlocked)
            .Select(r => new RoomDetailsDto
            {
                RoomId = r.RoomId,

                RoomNumber = r.RoomNumber,

                RoomTypeId = r.RoomTypeId,

                RoomName = r.RoomName,

                Capacity = r.Capacity,

                ModuleId = r.ModuleId,

                Module =
                    r.Module != null
                        ? r.Module.ModuleName
                        : string.Empty,

                Status = r.Status,

                IsBlocked = r.IsBlocked,

                Facilities =
                    r.RoomFacilities
                        .Where(rf =>
                            rf.Facility != null)
                        .Select(rf =>
                            rf.Facility!.FacilityName)
                        .ToList()
            })
            .FirstOrDefaultAsync();
    }

    // =========================================================
    // GET ROOMS BY MODULE
    // =========================================================

    public async Task<List<RoomDetailsDto>>
        GetRoomsByModuleAsync(string module)
    {
        if (string.IsNullOrWhiteSpace(module))
        {
            return new List<RoomDetailsDto>();
        }

        var moduleName = module.Trim();

        return await _context.Rooms
            .AsNoTracking()
            .Include(r => r.RoomType)
            .Include(r => r.Module)
            .Include(r => r.RoomFacilities)
                .ThenInclude(rf => rf.Facility)
            .Where(r =>
                r.Status != "Blocked" &&
                !r.IsBlocked &&
                r.Module != null &&
                r.Module.ModuleName == moduleName)
            .Select(r => new RoomDetailsDto
            {
                RoomId = r.RoomId,

                RoomNumber = r.RoomNumber,

                RoomTypeId = r.RoomTypeId,

                RoomName = r.RoomName,

                Capacity = r.Capacity,

                ModuleId = r.ModuleId,

                Module =
                    r.Module != null
                        ? r.Module.ModuleName
                        : string.Empty,

                Status = r.Status,

                IsBlocked = r.IsBlocked,

                Facilities =
                    r.RoomFacilities
                        .Where(rf =>
                            rf.Facility != null)
                        .Select(rf =>
                            rf.Facility!.FacilityName)
                        .ToList()
            })
            .ToListAsync();
    }

    // =========================================================
    // CREATE ROOM
    // =========================================================

    public async Task AddAsync(
        Room room,
        List<int> facilityIds)
    {
        await using var transaction =
            await _context.Database
                .BeginTransactionAsync();

        try
        {
            // -------------------------------------------------
            // VALIDATE MODULE
            // -------------------------------------------------

            var moduleExists =
                await _context.Modules
                    .AnyAsync(m =>
                        m.ModuleId == room.ModuleId);

            if (!moduleExists)
            {
                throw new KeyNotFoundException(
                    $"Module with ID {room.ModuleId} not found.");
            }

            // -------------------------------------------------
            // VALIDATE ROOM NUMBER
            // -------------------------------------------------

            if (string.IsNullOrWhiteSpace(room.RoomNumber))
            {
                throw new ArgumentException(
                    "Room number is required.");
            }

            var roomNumberExists =
                await _context.Rooms.AnyAsync(
                    r => r.RoomNumber == room.RoomNumber);

            if (roomNumberExists)
            {
                throw new ArgumentException(
                    $"Room number '{room.RoomNumber}' already exists.");
            }

            // -------------------------------------------------
            // ADD ROOM
            // -------------------------------------------------

            await _context.Rooms.AddAsync(room);

            await _context.SaveChangesAsync();

            // -------------------------------------------------
            // ADD FACILITIES
            // -------------------------------------------------

            if (facilityIds != null &&
                facilityIds.Count > 0)
            {
                foreach (var facilityId in facilityIds)
                {
                    var facilityExists =
                        await _context.Facilities
                            .AnyAsync(f =>
                                f.FacilityId == facilityId);

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

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // =========================================================
    // UPDATE ROOM
    // =========================================================

    public async Task UpdateAsync(
        Room room,
        List<int> facilityIds)
    {
        await using var transaction =
            await _context.Database
                .BeginTransactionAsync();

        try
        {
            // -------------------------------------------------
            // FIND EXISTING ROOM
            // -------------------------------------------------

            var existingRoom =
                await _context.Rooms
                    .FirstOrDefaultAsync(
                        r => r.RoomId == room.RoomId);

            if (existingRoom == null)
            {
                throw new KeyNotFoundException(
                    "Room not found.");
            }

            // -------------------------------------------------
            // VALIDATE MODULE
            // -------------------------------------------------

            var moduleExists =
                await _context.Modules
                    .AnyAsync(m =>
                        m.ModuleId == room.ModuleId);

            if (!moduleExists)
            {
                throw new KeyNotFoundException(
                    $"Module with ID {room.ModuleId} not found.");
            }

            // -------------------------------------------------
            // VALIDATE ROOM NUMBER
            // -------------------------------------------------

            if (string.IsNullOrWhiteSpace(room.RoomNumber))
            {
                throw new ArgumentException(
                    "Room number is required.");
            }

            var roomNumberExists =
                await _context.Rooms.AnyAsync(
                    r =>
                        r.RoomNumber == room.RoomNumber &&
                        r.RoomId != room.RoomId);

            if (roomNumberExists)
            {
                throw new ArgumentException(
                    $"Room number '{room.RoomNumber}' already exists.");
            }

            // -------------------------------------------------
            // UPDATE ROOM FIELDS
            // -------------------------------------------------

            existingRoom.RoomNumber =
                room.RoomNumber;

            existingRoom.RoomTypeId =
                room.RoomTypeId;

            existingRoom.RoomName =
                room.RoomName;

            existingRoom.Capacity =
                room.Capacity;

            existingRoom.ModuleId =
                room.ModuleId;

            existingRoom.Status =
                room.Status;

            // -------------------------------------------------
            // REMOVE OLD FACILITIES
            // -------------------------------------------------

            var existingFacilities =
                await _context.RoomFacilities
                    .Where(x =>
                        x.RoomId == room.RoomId)
                    .ToListAsync();

            _context.RoomFacilities
                .RemoveRange(existingFacilities);

            await _context.SaveChangesAsync();

            // -------------------------------------------------
            // ADD NEW FACILITIES
            // -------------------------------------------------

            if (facilityIds != null &&
                facilityIds.Count > 0)
            {
                foreach (var facilityId in facilityIds)
                {
                    var facilityExists =
                        await _context.Facilities
                            .AnyAsync(f =>
                                f.FacilityId == facilityId);

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

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // =========================================================
    // DELETE / BLOCK ROOM
    // =========================================================

    public async Task DeleteAsync(
        int roomId)
    {
        var room =
            await _context.Rooms
                .FindAsync(roomId);

        if (room == null)
        {
            return;
        }

        room.Status = "Blocked";
        room.IsBlocked = true;

        await _context.SaveChangesAsync();
    }

    // =========================================================
    // CHECK ROOM EXISTS
    // =========================================================

    public async Task<bool> ExistsAsync(
        int roomId)
    {
        return await _context.Rooms
            .AnyAsync(
                r =>
                    r.RoomId == roomId &&
                    r.Status != "Blocked" &&
                    !r.IsBlocked);
    }

    // =========================================================
    // BLOCK / UNBLOCK ROOM
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
}