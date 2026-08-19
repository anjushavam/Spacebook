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
        // PROJECT TO DTO
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

    // =========================================================
    // GET ROOM AVAILABILITY
    // =========================================================

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

        // =====================================================
        // FILTER BY ROOM TYPE
        // =====================================================

        if (roomTypeId.HasValue)
        {
            roomsQuery = roomsQuery.Where(r =>
                r.RoomTypeId == roomTypeId.Value);
        }

        var rooms = await roomsQuery
            .OrderBy(r => r.RoomName)
            .ToListAsync();

        // =====================================================
        // GET BOOKINGS FOR DATE
        // =====================================================

        var bookings = await _context.Bookings
            .AsNoTracking()
            .Where(b =>
                b.BookingDate == date &&
                b.Status != "Cancelled" &&
                b.Status != "Rejected")
            .ToListAsync();

        // =====================================================
        // OFFICE TIME SLOTS
        // 09:00 AM - 07:30 PM
        // =====================================================

        var timeSlots = new List<(TimeOnly Start, TimeOnly End)>
        {
            (new TimeOnly(9, 0),  new TimeOnly(10, 0)),
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

        // =====================================================
        // BUILD ROOM AVAILABILITY
        // =====================================================

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

    // =========================================================
    // GET ROOM RECOMMENDATIONS
    // =========================================================

    public async Task<List<CopilotRecommendationDto>> GetRecommendationsAsync(
        CopilotRecommendationRequestDto request)
    {
        // =====================================================
        // VALIDATE REQUEST
        // =====================================================

        if (request.ParticipantCount <= 0)
        {
            throw new ArgumentException(
                "Participant count must be at least 1.");
        }

        if (request.StartTime >= request.EndTime)
        {
            throw new ArgumentException(
                "Start time must be before end time.");
        }

        // =====================================================
        // FIND ACTIVE ROOMS
        // =====================================================

        var query = _context.Rooms
            .AsNoTracking()
            .Include(r => r.RoomType)
            .Include(r => r.Module)
                .ThenInclude(m => m!.Office)
                    .ThenInclude(o => o.Location)
            .Include(r => r.RoomFacilities)
                .ThenInclude(rf => rf.Facility)
            .Where(r =>
                !r.IsBlocked &&
                r.Status != "Blocked");

        // =====================================================
        // OFFICE FILTER
        // =====================================================

        if (request.OfficeId.HasValue)
        {
            query = query.Where(r =>
                r.Module != null &&
                r.Module.OfficeId == request.OfficeId.Value);
        }

        // =====================================================
        // ROOM TYPE FILTER
        // =====================================================

        if (request.RoomTypeId.HasValue)
        {
            query = query.Where(r =>
                r.RoomTypeId == request.RoomTypeId.Value);
        }

        // =====================================================
        // CAPACITY FILTER
        // =====================================================

        query = query.Where(r =>
            r.Capacity >= request.ParticipantCount);

        // =====================================================
        // FACILITY FILTER
        // =====================================================

        if (!string.IsNullOrWhiteSpace(request.Facility))
        {
            var facility = request.Facility.Trim();

            query = query.Where(r =>
                r.RoomFacilities.Any(rf =>
                    rf.Facility != null &&
                    rf.Facility.FacilityName.Contains(facility)));
        }

        var rooms = await query
            .OrderBy(r => r.RoomName)
            .ToListAsync();

        // =====================================================
        // GET BOOKINGS FOR REQUESTED DATE
        // =====================================================

        var bookings = await _context.Bookings
            .AsNoTracking()
            .Where(b =>
                b.BookingDate == request.Date &&
                b.Status != "Cancelled" &&
                b.Status != "Rejected")
            .ToListAsync();

        var recommendations = new List<CopilotRecommendationDto>();

        // =====================================================
        // CHECK EACH ROOM
        // =====================================================

        foreach (var room in rooms)
        {
            var roomBookings = bookings
                .Where(b => b.RoomId == room.RoomId)
                .ToList();

            // =================================================
            // CHECK BOOKING OVERLAP
            // =================================================

            var isBooked = roomBookings.Any(b =>
                b.StartTime < request.EndTime &&
                b.EndTime > request.StartTime);

            if (isBooked)
            {
                continue;
            }

            // =================================================
            // CALCULATE MATCH SCORE
            // =================================================

            var score = 0;

            // Exact capacity is preferred
            if (room.Capacity == request.ParticipantCount)
            {
                score += 40;
            }
            else
            {
                score += 25;
            }

            // Requested facility
            if (!string.IsNullOrWhiteSpace(request.Facility))
            {
                var facilityMatch = room.RoomFacilities.Any(rf =>
                    rf.Facility != null &&
                    rf.Facility.FacilityName.Contains(
                        request.Facility.Trim(),
                        StringComparison.OrdinalIgnoreCase));

                if (facilityMatch)
                {
                    score += 30;
                }
            }

            // Requested room type
            if (request.RoomTypeId.HasValue &&
                room.RoomTypeId == request.RoomTypeId.Value)
            {
                score += 20;
            }

            // Room is available
            score += 10;

            // =================================================
            // ADD RECOMMENDATION
            // =================================================

            recommendations.Add(new CopilotRecommendationDto
            {
                RoomId = room.RoomId,

                RoomName = room.RoomName,

                RoomType =
                    room.RoomType != null
                        ? room.RoomType.TypeName
                        : string.Empty,

                OfficeName =
                    room.Module != null &&
                    room.Module.Office != null
                        ? room.Module.Office.OfficeName
                        : string.Empty,

                LocationName =
                    room.Module != null &&
                    room.Module.Office != null &&
                    room.Module.Office.Location != null
                        ? room.Module.Office.Location.LocationName
                        : string.Empty,

                ModuleName =
                    room.Module != null
                        ? room.Module.ModuleName
                        : string.Empty,

                Capacity = room.Capacity,

                Facilities =
                    room.RoomFacilities
                        .Where(rf => rf.Facility != null)
                        .Select(rf => rf.Facility!.FacilityName)
                        .ToList(),

                IsAvailable = true,

                MatchScore = score
            });
        }

        // =====================================================
        // SORT RESULTS
        // =====================================================

        return recommendations
            .OrderByDescending(r => r.MatchScore)
            .ThenBy(r => r.Capacity)
            .ThenBy(r => r.RoomName)
            .ToList();
    }
}