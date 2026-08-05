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

    // Dashboard
    public async Task<RoomDashboardDto> GetDashboardAsync()
    {
        return new RoomDashboardDto
        {
            TotalRooms = await _context.Rooms.CountAsync(),
            AvailableRooms = await _context.Rooms.CountAsync(r => r.Status == "Available"),
            BookedRooms = await _context.Rooms.CountAsync(r => r.Status == "Booked")
        };
    }

    // Get all rooms
    public async Task<IEnumerable<RoomDto>> GetAllAsync(RoomFilterDto filter)
    {
        var query = _context.Rooms
            .Include(r => r.RoomType)
            .Where(r => r.Status != "Blocked")
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            query = query.Where(r =>
                r.RoomName.Contains(filter.Search) ||
                r.Module.Contains(filter.Search));
        }

        if (filter.RoomTypeId.HasValue)
        {
            query = query.Where(r => r.RoomTypeId == filter.RoomTypeId);
        }

        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            query = query.Where(r => r.Status == filter.Status);
        }

        return await query
            .Select(r => new RoomDto
            {
                RoomId = r.RoomId,
                RoomName = r.RoomName,
                RoomType = r.RoomType!.TypeName,
                Capacity = r.Capacity,
                Module = r.Module,
                Status = r.Status
            })
            .ToListAsync();
    }

    // Get room by id
    public async Task<RoomDetailsDto?> GetByIdAsync(int roomId)
    {
        return await _context.Rooms
            .Include(r => r.RoomFacilities)
                .ThenInclude(rf => rf.Facility)
            .Where(r => r.RoomId == roomId && r.Status != "Blocked")
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

    // Create Room
    public async Task AddAsync(Room room, List<int> facilityIds)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            await _context.Rooms.AddAsync(room);
            await _context.SaveChangesAsync();

            foreach (var facilityId in facilityIds)
            {
                await _context.RoomFacilities.AddAsync(new RoomFacility
                {
                    RoomId = room.RoomId,
                    FacilityId = facilityId
                });
            }

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // Update Room
    public async Task UpdateAsync(Room room, List<int> facilityIds)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            _context.Rooms.Update(room);

            var existingFacilities = await _context.RoomFacilities
                .Where(x => x.RoomId == room.RoomId)
                .ToListAsync();

            _context.RoomFacilities.RemoveRange(existingFacilities);

            foreach (var facilityId in facilityIds)
            {
                await _context.RoomFacilities.AddAsync(new RoomFacility
                {
                    RoomId = room.RoomId,
                    FacilityId = facilityId
                });
            }

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // Block Room
    public async Task DeleteAsync(int roomId)
    {
        var room = await _context.Rooms.FindAsync(roomId);

        if (room == null)
            return;

        room.Status = "Blocked";

        await _context.SaveChangesAsync();
    }

    // Check Room Exists
    public async Task<bool> ExistsAsync(int roomId)
    {
        return await _context.Rooms.AnyAsync(r =>
            r.RoomId == roomId &&
            r.Status != "Blocked");
    }
}