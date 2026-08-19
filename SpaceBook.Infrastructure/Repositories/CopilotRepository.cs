using Microsoft.EntityFrameworkCore;
using SpaceBook.Application.DTOs.Copilot;
using SpaceBook.Application.Interfaces;
using SpaceBook.Infrastructure.Data;
 
namespace SpaceBook.Infrastructure.Repositories;
 
public class CopilotRepository : ICopilotRepository
{
    private readonly ApplicationDbContext _context;
 
    public CopilotRepository(ApplicationDbContext context)
    {
        _context = context;
    }
 
    // =========================================================
    // GET OFFICES
    // =========================================================
 
    public async Task<List<OfficeCopilotDto>> GetOfficesAsync()
    {
        return await _context.Offices
            .AsNoTracking()
            .Include(o => o.Location)
            .OrderBy(o => o.OfficeName)
            .Select(o => new OfficeCopilotDto
            {
                OfficeId = o.OfficeId,
 
                OfficeName = o.OfficeName,
 
                LocationName =
                    o.Location != null
                        ? o.Location.LocationName
                        : string.Empty
            })
            .ToListAsync();
    }
 
    // =========================================================
    // GET / SEARCH ROOMS
    // =========================================================
 
    public async Task<List<RoomCopilotDto>> GetRoomsAsync(
        string? search,
        int? officeId,
        int? roomTypeId,
        int? minCapacity,
        string? facility)
    {
        var query = _context.Rooms
            .AsNoTracking()
            .Include(r => r.Module)
                .ThenInclude(m => m!.Office)
                    .ThenInclude(o => o.Location)
            .Include(r => r.RoomFacilities)
                .ThenInclude(rf => rf.Facility)
            .AsQueryable();
 
        // =====================================================
        // SEARCH
        // =====================================================
 
        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
 
            query = query.Where(r =>
                r.RoomName.Contains(search) ||
                (
                    r.Module != null &&
                    r.Module.ModuleName.Contains(search)
                ) ||
                (
                    r.Module != null &&
                    r.Module.Office != null &&
                    r.Module.Office.OfficeName.Contains(search)
                ) ||
                (
                    r.Module != null &&
                    r.Module.Office != null &&
                    r.Module.Office.Location != null &&
                    r.Module.Office.Location.LocationName.Contains(search)
                ));
        }
 
        // =====================================================
        // FILTER BY OFFICE
        // =====================================================
 
        if (officeId.HasValue)
        {
            query = query.Where(r =>
                r.Module != null &&
                r.Module.OfficeId == officeId.Value);
        }
 
        // =====================================================
        // FILTER BY ROOM TYPE
        // =====================================================
 
        if (roomTypeId.HasValue)
        {
            query = query.Where(r =>
                r.RoomTypeId == roomTypeId.Value);
        }
 
        // =====================================================
        // FILTER BY MINIMUM CAPACITY
        // =====================================================
 
        if (minCapacity.HasValue)
        {
            query = query.Where(r =>
                r.Capacity >= minCapacity.Value);
        }
 
        // =====================================================
        // FILTER BY FACILITY
        // =====================================================
 
        if (!string.IsNullOrWhiteSpace(facility))
        {
            facility = facility.Trim();
 
            query = query.Where(r =>
                r.RoomFacilities.Any(rf =>
                    rf.Facility != null &&
                    rf.Facility.FacilityName.Contains(facility)));
        }
 
        // =====================================================
        // PROJECT TO COPILOT DTO
        // =====================================================
 
        return await query
            .OrderBy(r => r.RoomName)
            .Select(r => new RoomCopilotDto
            {
                RoomId = r.RoomId,
 
                RoomName = r.RoomName,
 
                OfficeName =
                    r.Module != null &&
                    r.Module.Office != null
                        ? r.Module.Office.OfficeName
                        : string.Empty,
 
                LocationName =
                    r.Module != null &&
                    r.Module.Office != null &&
                    r.Module.Office.Location != null
                        ? r.Module.Office.Location.LocationName
                        : string.Empty,
 
                ModuleName =
                    r.Module != null
                        ? r.Module.ModuleName
                        : string.Empty,
 
                Capacity = r.Capacity,
 
                Status = r.Status,
 
                IsBlocked = r.IsBlocked,
 
                Facilities =
                    r.RoomFacilities
                        .Where(rf => rf.Facility != null)
                        .Select(rf => rf.Facility!.FacilityName)
                        .ToList()
            })
            .ToListAsync();
    }
}