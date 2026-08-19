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
    public async Task<CopilotAvailabilityResponseDto> GetAvailabilityAsync(
    DateOnly date,
    int? roomTypeId)
{
    var roomsQuery = _context.Rooms
        .AsNoTracking()
        .Include(r => r.RoomType)
        .Include(r => r.Module)
        .Include(r => r.RoomFacilities)
            .ThenInclude(rf => rf.Facility)
        .Where(r =>
            !r.IsBlocked &&
            r.Status != "Blocked");

    if (roomTypeId.HasValue)
    {
        roomsQuery = roomsQuery.Where(r =>
            r.RoomTypeId == roomTypeId.Value);
    }

    var rooms = await roomsQuery
        .OrderBy(r => r.RoomName)
        .ToListAsync();

    var bookings = await _context.Bookings
        .AsNoTracking()
        .Where(b =>
            b.BookingDate == date &&
            b.Status != "Cancelled" &&
            b.Status != "Rejected")
        .ToListAsync();

    var timeSlots = new List<(TimeOnly Start, TimeOnly End)>
    {
        (new TimeOnly(9, 0), new TimeOnly(10, 0)),
        (new TimeOnly(10, 0), new TimeOnly(11, 0)),
        (new TimeOnly(11, 0), new TimeOnly(12, 0)),
        (new TimeOnly(12, 0), new TimeOnly(13, 0)),
        (new TimeOnly(13, 0), new TimeOnly(14, 0)),
        (new TimeOnly(14, 0), new TimeOnly(15, 0)),
        (new TimeOnly(15, 0), new TimeOnly(16, 0)),
        (new TimeOnly(16, 0), new TimeOnly(17, 0)),
        (new TimeOnly(17, 0), new TimeOnly(18, 0)),
        (new TimeOnly(18, 0), new TimeOnly(19, 0)),
        (new TimeOnly(19, 0), new TimeOnly(19, 30))
    };

    var result = new CopilotAvailabilityResponseDto
    {
        Date = date
    };

    foreach (var room in rooms)
    {
        var roomBookings = bookings
            .Where(b => b.RoomId == room.RoomId)
            .ToList();

        var slots = timeSlots
            .Select(slot =>
            {
                var booked = roomBookings.Any(b =>
                    b.StartTime < slot.End &&
                    b.EndTime > slot.Start);

                return new CopilotTimeSlotDto
                {
                    StartTime = slot.Start,
                    EndTime = slot.End,
                    IsBooked = booked
                };
            })
            .ToList();

        var currentBooking = roomBookings
            .OrderBy(b => b.StartTime)
            .FirstOrDefault();

        result.Rooms.Add(new CopilotRoomAvailabilityDto
        {
            RoomId = room.RoomId,

            RoomName = room.RoomName,

            RoomType =
                room.RoomType != null
                    ? room.RoomType.TypeName
                    : string.Empty,

            Module =
                room.Module != null
                    ? room.Module.ModuleName
                    : string.Empty,

            Capacity = room.Capacity,

            Facilities =
                room.RoomFacilities
                    .Where(rf => rf.Facility != null)
                    .Select(rf => rf.Facility!.FacilityName)
                    .ToList(),

            Status = room.Status,

            AvailableSlots =
                slots.Count(s => !s.IsBooked),

            TimeSlots = slots,

            CurrentBooking =
                currentBooking == null
                    ? null
                    : new CopilotCurrentBookingDto
                    {
                        Purpose =
                            currentBooking.Purpose ?? string.Empty,

                        StartTime =
                            currentBooking.StartTime,

                        EndTime =
                            currentBooking.EndTime,

                        Status =
                            currentBooking.Status
                    }
        });
    }

    return result;
}
}