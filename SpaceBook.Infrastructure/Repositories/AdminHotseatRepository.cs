using Microsoft.EntityFrameworkCore;
using SpaceBook.Application.DTOs.Hotseat;
using SpaceBook.Application.Interfaces;
using SpaceBook.Domain.Entities;
using SpaceBook.Infrastructure.Data;
using System.Text;

namespace SpaceBook.Infrastructure.Repositories;

public class AdminHotseatRepository : IAdminHotseatRepository
{
    private readonly ApplicationDbContext _context;
    private static readonly TimeZoneInfo IndiaTimeZone = GetIndiaTimeZone();

    public AdminHotseatRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    private static TimeZoneInfo GetIndiaTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");
        }
        catch (TimeZoneNotFoundException)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            }
            catch
            {
                return TimeZoneInfo.Utc;
            }
        }
        catch (InvalidTimeZoneException)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            }
            catch
            {
                return TimeZoneInfo.Utc;
            }
        }
    }

    private static DateTime GetIndiaNow()
    {
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, IndiaTimeZone);
    }

    private static DateOnly GetIndiaToday()
    {
        return DateOnly.FromDateTime(GetIndiaNow());
    }

    private async Task AutoExpireOverdueHotseatBookingsAsync()
    {
        try
        {
            var nowUtc = DateTime.UtcNow;
            var today = GetIndiaToday();

            // Find Confirmed bookings where CheckInTime is null and check-in window has expired
            var overdueBookings = await _context.HotseatBookings
                .Where(b => b.BookingStatus == "Confirmed" &&
                            b.CheckInTime == null &&
                            (b.BookingDate < today ||
                             (b.BookingDate == today && b.CheckInDeadline.HasValue && b.CheckInDeadline.Value < nowUtc)))
                .ToListAsync();

            if (overdueBookings.Any())
            {
                foreach (var b in overdueBookings)
                {
                    b.BookingStatus = "Expired";
                    b.RecordModifiedBy = "System (Auto-Expired)";
                    b.RecordModifiedOn = nowUtc;
                }

                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AdminHotseatRepository] AutoExpire error: {ex.Message}");
        }
    }

    // =========================================================
    // 1. DASHBOARD ANALYTICS (Strictly Hotseat Bookings Only)
    // =========================================================

    public async Task<HotseatManagementDashboardDto> GetHotseatDashboardAnalyticsAsync(
        HotseatFilterDto filter)
    {
        await AutoExpireOverdueHotseatBookingsAsync();

        var today = GetIndiaToday();
        var (startDate, endDate) = ResolveDateRange(filter, today);

        // Base query for hotseat bookings (strictly HotseatBookings only)
        // NOTE: Excludes Status filter so TotalReservations is independent of Status filter
        var query = _context.HotseatBookings
            .AsNoTracking()
            .Include(h => h.Employee)
            .Include(h => h.Seat)
                .ThenInclude(s => s!.Module)
                    .ThenInclude(m => m!.Office)
                        .ThenInclude(o => o!.Location)
            .AsQueryable();

        // Apply Date filter
        if (startDate.HasValue)
        {
            query = query.Where(h => h.BookingDate >= startDate.Value);
        }
        if (endDate.HasValue)
        {
            query = query.Where(h => h.BookingDate <= endDate.Value);
        }

        // Apply Module filter
        if (!string.IsNullOrWhiteSpace(filter.Module) &&
            !filter.Module.Equals("All", StringComparison.OrdinalIgnoreCase) &&
            !filter.Module.Equals("All Modules", StringComparison.OrdinalIgnoreCase))
        {
            var targetModule = filter.Module.Trim().ToLowerInvariant();
            query = query.Where(h =>
                h.Seat != null &&
                h.Seat.Module != null &&
                (
                    h.Seat.Module.ModuleName.ToLower() == targetModule ||
                    targetModule.Contains(h.Seat.Module.ModuleName.ToLower()) ||
                    h.Seat.Module.ModuleName.ToLower().Contains(targetModule) ||
                    (h.Seat.Module.Office != null &&
                     (h.Seat.Module.ModuleName + " - " + h.Seat.Module.Office.OfficeName).ToLower() == targetModule) ||
                    (h.Seat.Module.Office != null &&
                     (h.Seat.Module.ModuleName + " - " + h.Seat.Module.Office.OfficeName + " - " + h.Seat.Module.Office.Location.LocationName).ToLower() == targetModule) ||
                    (h.Seat.Module.Office != null &&
                     targetModule.Contains(h.Seat.Module.ModuleName.ToLower()) &&
                     targetModule.Contains(h.Seat.Module.Office.OfficeName.ToLower()))
                ));
        }

        // Apply Section filter
        if (!string.IsNullOrWhiteSpace(filter.Section) &&
            !filter.Section.Equals("All", StringComparison.OrdinalIgnoreCase) &&
            !filter.Section.Equals("All Sections", StringComparison.OrdinalIgnoreCase))
        {
            var targetSection = NormalizeSection(filter.Section);
            query = query.Where(h =>
                h.Seat != null &&
                h.Seat.Section != null &&
                (h.Seat.Section.ToUpper() == targetSection.ToUpper() ||
                 ("Section " + h.Seat.Section).ToLower() == filter.Section.Trim().ToLower()));
        }

        var baseBookings = await query.ToListAsync();

        // -----------------------------------------------------
        // Active Seats Capacity Query (Strictly Hotseat Seats)
        // -----------------------------------------------------
        var seatsQuery = _context.Seats
            .AsNoTracking()
            .Include(s => s.Module)
                .ThenInclude(m => m!.Office)
                    .ThenInclude(o => o!.Location)
            .Where(s => s.IsActive)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Module) &&
            !filter.Module.Equals("All", StringComparison.OrdinalIgnoreCase) &&
            !filter.Module.Equals("All Modules", StringComparison.OrdinalIgnoreCase))
        {
            var targetModule = filter.Module.Trim().ToLowerInvariant();
            seatsQuery = seatsQuery.Where(s =>
                s.Module != null &&
                (
                    s.Module.ModuleName.ToLower() == targetModule ||
                    targetModule.Contains(s.Module.ModuleName.ToLower()) ||
                    s.Module.ModuleName.ToLower().Contains(targetModule) ||
                    (s.Module.Office != null &&
                     (s.Module.ModuleName + " - " + s.Module.Office.OfficeName).ToLower() == targetModule) ||
                    (s.Module.Office != null && s.Module.Office.Location != null &&
                     (s.Module.ModuleName + " - " + s.Module.Office.OfficeName + " - " + s.Module.Office.Location.LocationName).ToLower() == targetModule) ||
                    (s.Module.Office != null &&
                     targetModule.Contains(s.Module.ModuleName.ToLower()) &&
                     targetModule.Contains(s.Module.Office.OfficeName.ToLower()))
                ));
        }

        var activeSeats = await seatsQuery.ToListAsync();
        int activeHotseatsCount = activeSeats.Count;

        // -----------------------------------------------------
        // KPI Calculations (Calculated from Base Dataset - NEVER changed by Status Filter!)
        // -----------------------------------------------------
        int totalReservations = baseBookings.Count;

        int confirmedBookings = baseBookings.Count(b =>
            string.Equals(b.BookingStatus, "Confirmed", StringComparison.OrdinalIgnoreCase));

        int checkedInBookings = baseBookings.Count(b =>
            string.Equals(b.BookingStatus, "CheckedIn", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(b.BookingStatus, "Checked In", StringComparison.OrdinalIgnoreCase));

        int cancelledBookings = baseBookings.Count(b =>
            string.Equals(b.BookingStatus, "Cancelled", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(b.BookingStatus, "Canceled", StringComparison.OrdinalIgnoreCase));

        int releasedBookings = baseBookings.Count(b =>
            string.Equals(b.BookingStatus, "Released", StringComparison.OrdinalIgnoreCase));

        int expiredBookings = baseBookings.Count(b =>
            string.Equals(b.BookingStatus, "Expired", StringComparison.OrdinalIgnoreCase));

        double confirmedRate = totalReservations > 0
            ? Math.Round((confirmedBookings + checkedInBookings) * 100.0 / totalReservations, 1)
            : 0.0;

        double cancelledRate = totalReservations > 0
            ? Math.Round(cancelledBookings * 100.0 / totalReservations, 1)
            : 0.0;

        // Utilization Calculation
        int daysCount;
        if (startDate.HasValue && endDate.HasValue)
        {
            daysCount = Math.Max(1, endDate.Value.DayNumber - startDate.Value.DayNumber + 1);
        }
        else
        {
            var distinctDates = baseBookings.Select(b => b.BookingDate).Distinct().Count();
            daysCount = Math.Max(1, distinctDates > 0 ? distinctDates : 1);
        }

        double totalAvailableSeatDays = activeHotseatsCount * daysCount;
        double utilization = totalAvailableSeatDays > 0
            ? Math.Round(((confirmedBookings + checkedInBookings) / totalAvailableSeatDays) * 100.0, 1)
            : 0.0;
        utilization = Math.Min(100.0, utilization);

        // -----------------------------------------------------
        // Status-Filtered Dataset (Used for Status-Specific Charts)
        // -----------------------------------------------------
        var bookings = baseBookings;
        if (!string.IsNullOrWhiteSpace(filter.Status) &&
            !filter.Status.Equals("All", StringComparison.OrdinalIgnoreCase) &&
            !filter.Status.Equals("All Status", StringComparison.OrdinalIgnoreCase) &&
            !filter.Status.Equals("All Statuses", StringComparison.OrdinalIgnoreCase))
        {
            var targetStatus = filter.Status.Trim();
            if (string.Equals(targetStatus, "Checked In", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(targetStatus, "CheckedIn", StringComparison.OrdinalIgnoreCase))
            {
                bookings = baseBookings.Where(h => h.BookingStatus == "CheckedIn" || h.BookingStatus == "Checked In").ToList();
            }
            else if (string.Equals(targetStatus, "Cancelled", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(targetStatus, "Canceled", StringComparison.OrdinalIgnoreCase))
            {
                bookings = baseBookings.Where(h => h.BookingStatus == "Cancelled" || h.BookingStatus == "Canceled").ToList();
            }
            else if (string.Equals(targetStatus, "Confirmed", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(targetStatus, "Confirmed Bookings", StringComparison.OrdinalIgnoreCase))
            {
                bookings = baseBookings.Where(h => h.BookingStatus == "Confirmed").ToList();
            }
            else if (string.Equals(targetStatus, "Released", StringComparison.OrdinalIgnoreCase))
            {
                bookings = baseBookings.Where(h => h.BookingStatus == "Released").ToList();
            }
            else if (string.Equals(targetStatus, "Expired", StringComparison.OrdinalIgnoreCase))
            {
                bookings = baseBookings.Where(h => h.BookingStatus == "Expired").ToList();
            }
            else
            {
                bookings = baseBookings.Where(h => string.Equals(h.BookingStatus, targetStatus, StringComparison.OrdinalIgnoreCase)).ToList();
            }
        }

        // -----------------------------------------------------
        // Chart 1: Hotseat Volume by Facility & Zone (Donut Chart)
        // -----------------------------------------------------
        var volumeByFacilityZone = bookings
            .Where(b => b.Seat != null && b.Seat.Module != null)
            .GroupBy(b => new
            {
                ModuleName = b.Seat!.Module!.ModuleName,
                OfficeName = b.Seat.Module.Office != null ? b.Seat.Module.Office.OfficeName : "Main Facility",
                LocationName = b.Seat.Module.Office?.Location != null ? b.Seat.Module.Office.Location.LocationName : "",
                Label = b.Seat.Module.Office != null && b.Seat.Module.Office.Location != null
                    ? $"{b.Seat.Module.ModuleName} - {b.Seat.Module.Office.OfficeName} - {b.Seat.Module.Office.Location.LocationName}"
                    : (b.Seat.Module.Office != null
                        ? $"{b.Seat.Module.ModuleName} - {b.Seat.Module.Office.OfficeName}"
                        : b.Seat.Module.ModuleName)
            })
            .Select(g => new HotseatVolumeByFacilityZoneDto
            {
                Label = g.Key.Label,
                ModuleName = g.Key.ModuleName,
                FacilityName = !string.IsNullOrWhiteSpace(g.Key.LocationName)
                    ? $"{g.Key.OfficeName} ({g.Key.LocationName})"
                    : g.Key.OfficeName,
                BookingCount = g.Count(),
                Percentage = totalReservations > 0
                    ? Math.Round(g.Count() * 100.0 / totalReservations, 1)
                    : 0.0
            })
            .OrderByDescending(v => v.BookingCount)
            .ToList();

        // -----------------------------------------------------
        // Chart 2: Floor Section Workstation Demand (Bar Chart)
        // -----------------------------------------------------
        var sectionGroups = bookings
            .Where(b => b.Seat != null)
            .GroupBy(b => NormalizeSection(b.Seat!.Section))
            .ToDictionary(g => g.Key, g => g.Count());

        var standardSections = new[] { "Section A", "Section B", "Section C", "Section D" };
        var floorSectionDemand = new List<FloorSectionDemandDto>();

        foreach (var sec in standardSections)
        {
            var rawLetter = sec.Replace("Section ", "").Trim();
            int count = 0;
            if (sectionGroups.TryGetValue(sec, out int fullCount))
            {
                count = fullCount;
            }
            else if (sectionGroups.TryGetValue(rawLetter, out int shortCount))
            {
                count = shortCount;
            }

            floorSectionDemand.Add(new FloorSectionDemandDto
            {
                Section = sec,
                BookingCount = count,
                Percentage = totalReservations > 0
                    ? Math.Round(count * 100.0 / totalReservations, 1)
                    : 0.0
            });
        }

        // Add any other dynamic sections not in A, B, C, D
        foreach (var kvp in sectionGroups)
        {
            var normalized = kvp.Key.StartsWith("Section ") ? kvp.Key : $"Section {kvp.Key}";
            if (!floorSectionDemand.Any(f => f.Section.Equals(normalized, StringComparison.OrdinalIgnoreCase)))
            {
                floorSectionDemand.Add(new FloorSectionDemandDto
                {
                    Section = normalized,
                    BookingCount = kvp.Value,
                    Percentage = totalReservations > 0
                        ? Math.Round(kvp.Value * 100.0 / totalReservations, 1)
                        : 0.0
                });
            }
        }

        // -----------------------------------------------------
        // Chart 3: Daily / Weekly Occupancy Trendline
        // -----------------------------------------------------
        var isWeekly = string.Equals(filter.TrendPeriod, "Weekly", StringComparison.OrdinalIgnoreCase);
        var occupancyTrendline = new List<DailyHotseatOccupancyTrendDto>();

        if (isWeekly)
        {
            // Weekly grouping
            var weeklyGroups = bookings
                .GroupBy(b =>
                {
                    var cal = System.Globalization.ISOWeek.GetWeekOfYear(b.BookingDate.ToDateTime(TimeOnly.MinValue));
                    var year = b.BookingDate.Year;
                    return $"{year}-W{cal:D2}";
                })
                .OrderBy(g => g.Key)
                .ToList();

            foreach (var g in weeklyGroups)
            {
                occupancyTrendline.Add(new DailyHotseatOccupancyTrendDto
                {
                    Date = g.Key,
                    Label = $"Week {g.Key.Split("-W").Last()}",
                    CheckInsCount = g.Count(b => b.CheckInTime.HasValue || b.BookingStatus == "CheckedIn" || b.BookingStatus == "Checked In"),
                    TotalBookingsCount = g.Count(b => b.BookingStatus == "Confirmed" || b.BookingStatus == "CheckedIn" || b.BookingStatus == "Checked In")
                });
            }
        }
        else
        {
            // Daily grouping
            DateOnly trendStart = startDate ?? (bookings.Any() ? bookings.Min(b => b.BookingDate) : today.AddDays(-6));
            DateOnly trendEnd = endDate ?? (bookings.Any() ? bookings.Max(b => b.BookingDate) : today);

            // Ensure window is at least 4-7 days for smooth chart visualization
            if (trendEnd.DayNumber - trendStart.DayNumber < 3)
            {
                trendStart = trendEnd.AddDays(-6);
            }

            for (var d = trendStart; d <= trendEnd; d = d.AddDays(1))
            {
                var targetDate = d;
                var dayBookings = bookings.Where(b => b.BookingDate == targetDate).ToList();
                int checkIns = dayBookings.Count(b => b.CheckInTime.HasValue || b.BookingStatus == "CheckedIn" || b.BookingStatus == "Checked In");
                int totalDay = dayBookings.Count(b => b.BookingStatus == "Confirmed" || b.BookingStatus == "CheckedIn" || b.BookingStatus == "Checked In");

                occupancyTrendline.Add(new DailyHotseatOccupancyTrendDto
                {
                    Date = targetDate.ToString("yyyy-MM-dd"),
                    Label = targetDate.ToString("yyyy-MM-dd"),
                    CheckInsCount = checkIns,
                    TotalBookingsCount = totalDay
                });
            }
        }

        // -----------------------------------------------------
        // Chart 4: Top In-Demand Workstation Desks (Horizontal Bar Chart)
        // -----------------------------------------------------
        var topDesks = bookings
            .Where(b => b.Seat != null)
            .GroupBy(b => new
            {
                b.SeatId,
                DeskNumber = b.Seat!.SeatNumber,
                Section = b.Seat.Section,
                ModuleName = b.Seat.Module != null ? b.Seat.Module.ModuleName : "",
                OfficeName = b.Seat.Module?.Office != null ? b.Seat.Module.Office.OfficeName : ""
            })
            .Select(g => new TopInDemandDeskDto
            {
                SeatId = g.Key.SeatId,
                DeskNumber = g.Key.DeskNumber,
                Section = g.Key.Section,
                ModuleName = g.Key.ModuleName,
                OfficeName = g.Key.OfficeName,
                ReservationCount = g.Count()
            })
            .OrderByDescending(d => d.ReservationCount)
            .ThenBy(d => d.DeskNumber)
            .Take(8)
            .ToList();

        // -----------------------------------------------------
        // Chart 5: Peak Hotseat Check-In Time Slots (Bar Chart)
        // -----------------------------------------------------
        var predefinedSlots = new List<(string Label, TimeSpan Start, TimeSpan End)>
        {
            ("09:30 - 10:30", new TimeSpan(9, 30, 0), new TimeSpan(10, 30, 0)),
            ("10:00 - 11:00", new TimeSpan(10, 0, 0), new TimeSpan(11, 0, 0)),
            ("10:30 - 11:30", new TimeSpan(10, 30, 0), new TimeSpan(11, 30, 0)),
            ("11:00 - 12:00", new TimeSpan(11, 0, 0), new TimeSpan(12, 0, 0)),
            ("13:00 - 14:00", new TimeSpan(13, 0, 0), new TimeSpan(14, 0, 0)),
            ("14:00 - 15:00", new TimeSpan(14, 0, 0), new TimeSpan(15, 0, 0)),
            ("15:00 - 16:00", new TimeSpan(15, 0, 0), new TimeSpan(16, 0, 0))
        };

        var peakSlots = new List<PeakCheckInSlotDto>();
        int maxSlotCount = 0;

        foreach (var slot in predefinedSlots)
        {
            int slotCount = bookings.Count(b =>
            {
                if (b.CheckInDeadline.HasValue)
                {
                    var istTime = TimeZoneInfo.ConvertTimeFromUtc(b.CheckInDeadline.Value, IndiaTimeZone);
                    var timeOfDay = istTime.TimeOfDay;
                    return timeOfDay >= slot.Start && timeOfDay < slot.End;
                }
                return false;
            });

            if (slotCount > maxSlotCount)
            {
                maxSlotCount = slotCount;
            }

            peakSlots.Add(new PeakCheckInSlotDto
            {
                TimeSlot = slot.Label,
                StartTime = slot.Start.ToString(@"hh\:mm"),
                EndTime = slot.End.ToString(@"hh\:mm"),
                CheckInSlotsCount = slotCount,
                Percentage = totalReservations > 0
                    ? Math.Round(slotCount * 100.0 / totalReservations, 1)
                    : 0.0,
                IsPeak = false
            });
        }

        // Mark the highest slot(s) as Peak
        if (maxSlotCount > 0)
        {
            foreach (var s in peakSlots.Where(s => s.CheckInSlotsCount == maxSlotCount))
            {
                s.IsPeak = true;
            }
        }

        return new HotseatManagementDashboardDto
        {
            TotalReservations = totalReservations,
            ActiveHotseatsCount = activeHotseatsCount,
            TotalVolumePercentage = 100.0,
            Utilization = utilization,
            ConfirmedBookings = confirmedBookings,
            ConfirmedRate = confirmedRate,
            CancelledBookings = cancelledBookings,
            CancelledRate = cancelledRate,
            TotalBookingsAnalyzed = totalReservations,
            VolumeByFacilityZone = volumeByFacilityZone,
            FloorSectionDemand = floorSectionDemand,
            DailyOccupancyTrendline = occupancyTrendline,
            TopInDemandDesks = topDesks,
            PeakCheckInSlots = peakSlots
        };
    }

    // =========================================================
    // 2. AUDIT RECORDS (Strictly Hotseat Bookings Only)
    // =========================================================

    public async Task<HotseatAuditPagedResultDto> GetHotseatAuditRecordsAsync(
        HotseatFilterDto filter)
    {
        await AutoExpireOverdueHotseatBookingsAsync();

        var today = GetIndiaToday();
        var (startDate, endDate) = ResolveDateRange(filter, today);

        var query = _context.HotseatBookings
            .AsNoTracking()
            .Include(h => h.Employee)
            .Include(h => h.Seat)
                .ThenInclude(s => s!.Module)
                    .ThenInclude(m => m!.Office)
                        .ThenInclude(o => o!.Location)
            .AsQueryable();

        // Apply Date filter
        if (startDate.HasValue)
        {
            query = query.Where(h => h.BookingDate >= startDate.Value);
        }
        if (endDate.HasValue)
        {
            query = query.Where(h => h.BookingDate <= endDate.Value);
        }

        // Apply Module filter
        if (!string.IsNullOrWhiteSpace(filter.Module) &&
            !filter.Module.Equals("All", StringComparison.OrdinalIgnoreCase) &&
            !filter.Module.Equals("All Modules", StringComparison.OrdinalIgnoreCase))
        {
            var targetModule = filter.Module.Trim().ToLowerInvariant();
            query = query.Where(h =>
                h.Seat != null &&
                h.Seat.Module != null &&
                (
                    h.Seat.Module.ModuleName.ToLower() == targetModule ||
                    targetModule.Contains(h.Seat.Module.ModuleName.ToLower()) ||
                    h.Seat.Module.ModuleName.ToLower().Contains(targetModule) ||
                    (h.Seat.Module.Office != null &&
                     (h.Seat.Module.ModuleName + " - " + h.Seat.Module.Office.OfficeName).ToLower() == targetModule) ||
                    (h.Seat.Module.Office != null &&
                     (h.Seat.Module.ModuleName + " - " + h.Seat.Module.Office.OfficeName + " - " + h.Seat.Module.Office.Location.LocationName).ToLower() == targetModule) ||
                    (h.Seat.Module.Office != null &&
                     targetModule.Contains(h.Seat.Module.ModuleName.ToLower()) &&
                     targetModule.Contains(h.Seat.Module.Office.OfficeName.ToLower()))
                ));
        }

        // Total count before status/search filters (Base population for Date & Module)
        int totalCount = await query.CountAsync();

        // Apply Status filter
        if (!string.IsNullOrWhiteSpace(filter.Status) &&
            !filter.Status.Equals("All", StringComparison.OrdinalIgnoreCase) &&
            !filter.Status.Equals("All Status", StringComparison.OrdinalIgnoreCase) &&
            !filter.Status.Equals("All Statuses", StringComparison.OrdinalIgnoreCase))
        {
            var targetStatus = filter.Status.Trim();
            if (string.Equals(targetStatus, "Checked In", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(targetStatus, "CheckedIn", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(h => h.BookingStatus == "CheckedIn" || h.BookingStatus == "Checked In");
            }
            else if (string.Equals(targetStatus, "Cancelled", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(targetStatus, "Canceled", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(h => h.BookingStatus == "Cancelled" || h.BookingStatus == "Canceled");
            }
            else if (string.Equals(targetStatus, "Confirmed", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(targetStatus, "Confirmed Bookings", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(h => h.BookingStatus == "Confirmed");
            }
            else if (string.Equals(targetStatus, "Released", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(h => h.BookingStatus == "Released");
            }
            else if (string.Equals(targetStatus, "Expired", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(h => h.BookingStatus == "Expired");
            }
            else
            {
                query = query.Where(h => h.BookingStatus.ToLower() == targetStatus.ToLower());
            }
        }

        // Apply Section filter
        if (!string.IsNullOrWhiteSpace(filter.Section) &&
            !filter.Section.Equals("All", StringComparison.OrdinalIgnoreCase) &&
            !filter.Section.Equals("All Sections", StringComparison.OrdinalIgnoreCase))
        {
            var targetSection = NormalizeSection(filter.Section);
            query = query.Where(h =>
                h.Seat != null &&
                h.Seat.Section != null &&
                (h.Seat.Section.ToUpper() == targetSection.ToUpper() ||
                 ("Section " + h.Seat.Section).ToLower() == filter.Section.Trim().ToLower()));
        }

        // Search Term
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.Trim().ToLower();
            query = query.Where(h =>
                (h.Employee != null && (h.Employee.Name.ToLower().Contains(term) || h.Employee.Email.ToLower().Contains(term))) ||
                (h.Seat != null && h.Seat.SeatNumber.ToLower().Contains(term)) ||
                (h.Seat != null && h.Seat.Section != null && h.Seat.Section.ToLower().Contains(term)) ||
                (h.Seat != null && h.Seat.Module != null && h.Seat.Module.ModuleName.ToLower().Contains(term)));
        }

        int filteredCount = await query.CountAsync();

        int page = filter.Page > 0 ? filter.Page : 1;
        int pageSize = filter.PageSize > 0 ? filter.PageSize : 20;

        var rawList = await query
            .OrderByDescending(h => h.BookingDate)
            .ThenByDescending(h => h.BookedOn)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = rawList.Select(h =>
        {
            DateTime? localDeadline = h.CheckInDeadline.HasValue
                ? TimeZoneInfo.ConvertTimeFromUtc(h.CheckInDeadline.Value, IndiaTimeZone)
                : null;

            DateTime? localBookedOn = TimeZoneInfo.ConvertTimeFromUtc(h.BookedOn, IndiaTimeZone);
            DateTime? localCheckInTime = h.CheckInTime.HasValue
                ? TimeZoneInfo.ConvertTimeFromUtc(h.CheckInTime.Value, IndiaTimeZone)
                : null;
            DateTime? localReleasedOn = h.ReleasedOn.HasValue
                ? TimeZoneInfo.ConvertTimeFromUtc(h.ReleasedOn.Value, IndiaTimeZone)
                : null;

            return new HotseatAuditRecordDto
            {
                HotseatBookingId = h.HotseatBookingId,
                SeatId = h.SeatId,
                SeatNumber = h.Seat?.SeatNumber ?? string.Empty,
                Section = h.Seat?.Section ?? string.Empty,
                RowNumber = h.Seat?.RowNumber ?? string.Empty,
                ColumnNumber = h.Seat?.ColumnNumber ?? 0,
                ModuleName = h.Seat?.Module?.ModuleName ?? string.Empty,
                OfficeName = h.Seat?.Module?.Office?.OfficeName ?? string.Empty,
                LocationName = h.Seat?.Module?.Office?.Location?.LocationName ?? string.Empty,
                EmployeeId = h.EmployeeId,
                EmployeeName = h.Employee?.Name ?? $"Employee #{h.EmployeeId}",
                EmployeeEmail = h.Employee?.Email ?? string.Empty,
                Department = h.Employee?.Department ?? string.Empty,
                BookingDate = h.BookingDate,
                BookingStatus = h.BookingStatus,
                ExpectedCheckInTime = localDeadline?.ToString("hh:mm tt"),
                CheckInDeadline = localDeadline?.ToString("yyyy-MM-dd HH:mm:ss"),
                CheckInTime = localCheckInTime?.ToString("yyyy-MM-dd HH:mm:ss"),
                BookedOn = localBookedOn?.ToString("yyyy-MM-dd HH:mm:ss"),
                ReleasedOn = localReleasedOn?.ToString("yyyy-MM-dd HH:mm:ss"),
                RecordIngestedBy = h.RecordIngestedBy,
                RecordIngestedOn = h.RecordIngestedOn,
                RecordModifiedBy = h.RecordModifiedBy,
                RecordModifiedOn = h.RecordModifiedOn
            };
        }).ToList();

        return new HotseatAuditPagedResultDto
        {
            TotalCount = totalCount,
            FilteredCount = filteredCount,
            Page = page,
            PageSize = pageSize,
            Items = items
        };
    }

    // =========================================================
    // 3. EXPORT CSV (Strictly Hotseat Bookings Only)
    // =========================================================

    public async Task<byte[]> ExportHotseatsCsvAsync(HotseatFilterDto filter)
    {
        await AutoExpireOverdueHotseatBookingsAsync();

        var today = GetIndiaToday();
        var (startDate, endDate) = ResolveDateRange(filter, today);

        var query = _context.HotseatBookings
            .AsNoTracking()
            .Include(h => h.Employee)
            .Include(h => h.Seat)
                .ThenInclude(s => s!.Module)
                    .ThenInclude(m => m!.Office)
                        .ThenInclude(o => o!.Location)
            .AsQueryable();

        // Apply Date filter
        if (startDate.HasValue)
        {
            query = query.Where(h => h.BookingDate >= startDate.Value);
        }
        if (endDate.HasValue)
        {
            query = query.Where(h => h.BookingDate <= endDate.Value);
        }

        // Apply Module filter
        if (!string.IsNullOrWhiteSpace(filter.Module) &&
            !filter.Module.Equals("All", StringComparison.OrdinalIgnoreCase) &&
            !filter.Module.Equals("All Modules", StringComparison.OrdinalIgnoreCase))
        {
            var targetModule = filter.Module.Trim().ToLowerInvariant();
            query = query.Where(h =>
                h.Seat != null &&
                h.Seat.Module != null &&
                (
                    h.Seat.Module.ModuleName.ToLower() == targetModule ||
                    targetModule.Contains(h.Seat.Module.ModuleName.ToLower()) ||
                    h.Seat.Module.ModuleName.ToLower().Contains(targetModule) ||
                    (h.Seat.Module.Office != null &&
                     (h.Seat.Module.ModuleName + " - " + h.Seat.Module.Office.OfficeName).ToLower() == targetModule) ||
                    (h.Seat.Module.Office != null &&
                     (h.Seat.Module.ModuleName + " - " + h.Seat.Module.Office.OfficeName + " - " + h.Seat.Module.Office.Location.LocationName).ToLower() == targetModule) ||
                    (h.Seat.Module.Office != null &&
                     targetModule.Contains(h.Seat.Module.ModuleName.ToLower()) &&
                     targetModule.Contains(h.Seat.Module.Office.OfficeName.ToLower()))
                ));
        }

        // Apply Status filter
        if (!string.IsNullOrWhiteSpace(filter.Status) &&
            !filter.Status.Equals("All", StringComparison.OrdinalIgnoreCase) &&
            !filter.Status.Equals("All Status", StringComparison.OrdinalIgnoreCase) &&
            !filter.Status.Equals("All Statuses", StringComparison.OrdinalIgnoreCase))
        {
            var targetStatus = filter.Status.Trim();
            if (string.Equals(targetStatus, "Checked In", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(targetStatus, "CheckedIn", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(h => h.BookingStatus == "CheckedIn" || h.BookingStatus == "Checked In");
            }
            else if (string.Equals(targetStatus, "Cancelled", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(targetStatus, "Canceled", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(h => h.BookingStatus == "Cancelled" || h.BookingStatus == "Canceled");
            }
            else if (string.Equals(targetStatus, "Confirmed", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(targetStatus, "Confirmed Bookings", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(h => h.BookingStatus == "Confirmed");
            }
            else if (string.Equals(targetStatus, "Released", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(h => h.BookingStatus == "Released");
            }
            else if (string.Equals(targetStatus, "Expired", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(h => h.BookingStatus == "Expired");
            }
            else
            {
                query = query.Where(h => h.BookingStatus.ToLower() == targetStatus.ToLower());
            }
        }

        // Apply Section filter
        if (!string.IsNullOrWhiteSpace(filter.Section) &&
            !filter.Section.Equals("All", StringComparison.OrdinalIgnoreCase) &&
            !filter.Section.Equals("All Sections", StringComparison.OrdinalIgnoreCase))
        {
            var targetSection = NormalizeSection(filter.Section);
            query = query.Where(h =>
                h.Seat != null &&
                h.Seat.Section != null &&
                (h.Seat.Section.ToUpper() == targetSection.ToUpper() ||
                 ("Section " + h.Seat.Section).ToLower() == filter.Section.Trim().ToLower()));
        }

        var list = await query
            .OrderByDescending(h => h.BookingDate)
            .ThenByDescending(h => h.BookedOn)
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Booking ID,Booking Date,Status,Seat Number,Section,Row,Module,Office / Facility,City,Employee ID,Employee Name,Employee Email,Department,Expected Check-In,Check-In Time,Booked On,Released On");

        foreach (var h in list)
        {
            DateTime? localDeadline = h.CheckInDeadline.HasValue
                ? TimeZoneInfo.ConvertTimeFromUtc(h.CheckInDeadline.Value, IndiaTimeZone)
                : null;
            DateTime? localBookedOn = TimeZoneInfo.ConvertTimeFromUtc(h.BookedOn, IndiaTimeZone);
            DateTime? localCheckInTime = h.CheckInTime.HasValue
                ? TimeZoneInfo.ConvertTimeFromUtc(h.CheckInTime.Value, IndiaTimeZone)
                : null;
            DateTime? localReleasedOn = h.ReleasedOn.HasValue
                ? TimeZoneInfo.ConvertTimeFromUtc(h.ReleasedOn.Value, IndiaTimeZone)
                : null;

            sb.AppendLine(string.Join(",",
                EscapeCsv(h.HotseatBookingId.ToString()),
                EscapeCsv(h.BookingDate.ToString("yyyy-MM-dd")),
                EscapeCsv(h.BookingStatus),
                EscapeCsv(h.Seat?.SeatNumber ?? ""),
                EscapeCsv(h.Seat?.Section ?? ""),
                EscapeCsv(h.Seat?.RowNumber ?? ""),
                EscapeCsv(h.Seat?.Module?.ModuleName ?? ""),
                EscapeCsv(h.Seat?.Module?.Office?.OfficeName ?? ""),
                EscapeCsv(h.Seat?.Module?.Office?.Location?.LocationName ?? ""),
                EscapeCsv(h.EmployeeId.ToString()),
                EscapeCsv(h.Employee?.Name ?? ""),
                EscapeCsv(h.Employee?.Email ?? ""),
                EscapeCsv(h.Employee?.Department ?? ""),
                EscapeCsv(localDeadline?.ToString("yyyy-MM-dd hh:mm tt") ?? "N/A"),
                EscapeCsv(localCheckInTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A"),
                EscapeCsv(localBookedOn?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A"),
                EscapeCsv(localReleasedOn?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A")
            ));
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    // =========================================================
    // 4. FILTER OPTIONS METADATA
    // =========================================================

    public async Task<HotseatFilterOptionsDto> GetFilterOptionsAsync()
    {
        var modules = await _context.Modules
            .AsNoTracking()
            .Include(m => m.Office)
                .ThenInclude(o => o!.Location)
            .OrderBy(m => m.Office != null ? m.Office.OfficeName : "")
            .ThenBy(m => m.ModuleName)
            .Select(m => new FilterOptionItemDto
            {
                Value = m.Office != null && m.Office.Location != null
                    ? $"{m.ModuleName} - {m.Office.OfficeName} - {m.Office.Location.LocationName}"
                    : (m.Office != null ? $"{m.ModuleName} - {m.Office.OfficeName}" : m.ModuleName),
                Label = m.Office != null && m.Office.Location != null
                    ? $"{m.ModuleName} - {m.Office.OfficeName} - {m.Office.Location.LocationName}"
                    : (m.Office != null ? $"{m.ModuleName} - {m.Office.OfficeName}" : m.ModuleName),
                Group = m.Office != null ? m.Office.OfficeName : "Main Office"
            })
            .Distinct()
            .ToListAsync();

        var distinctSections = await _context.Seats
            .AsNoTracking()
            .Where(s => !string.IsNullOrEmpty(s.Section))
            .Select(s => s.Section!)
            .Distinct()
            .ToListAsync();

        var normalizedSections = distinctSections
            .Select(NormalizeSection)
            .Distinct()
            .OrderBy(s => s)
            .ToList();

        if (!normalizedSections.Contains("Section A")) normalizedSections.Insert(0, "Section A");
        if (!normalizedSections.Contains("Section B")) normalizedSections.Insert(1, "Section B");
        if (!normalizedSections.Contains("Section C")) normalizedSections.Insert(2, "Section C");
        if (!normalizedSections.Contains("Section D")) normalizedSections.Insert(3, "Section D");

        return new HotseatFilterOptionsDto
        {
            Timeframes = new List<FilterOptionItemDto>
            {
                new() { Value = "All Time", Label = "All Time" },
                new() { Value = "Today", Label = "Today" },
                new() { Value = "Past 7 Days", Label = "Past 7 Days" },
                new() { Value = "Past 30 Days", Label = "Past 30 Days" },
                new() { Value = "Past Dates", Label = "Past Dates" },
                new() { Value = "Upcoming", Label = "Upcoming" }
            },
            Modules = modules,
            Statuses = new List<FilterOptionItemDto>
            {
                new() { Value = "All Status", Label = "All Status" },
                new() { Value = "Confirmed", Label = "Confirmed" },
                new() { Value = "Checked In", Label = "Checked In" },
                new() { Value = "Cancelled", Label = "Cancelled" },
                new() { Value = "Released", Label = "Released" },
                new() { Value = "Expired", Label = "Expired" }
            },
            Sections = normalizedSections.Distinct().ToList()
        };
    }

    // =========================================================
    // HELPER METHODS
    // =========================================================

    private static (DateOnly? Start, DateOnly? End) ResolveDateRange(HotseatFilterDto filter, DateOnly today)
    {
        if (filter.StartDate.HasValue || filter.EndDate.HasValue)
        {
            return (filter.StartDate, filter.EndDate);
        }

        var tf = filter.Timeframe?.Trim().ToLowerInvariant() ?? "all time";

        return tf switch
        {
            "today" => (today, today),
            "yesterday" => (today.AddDays(-1), today.AddDays(-1)),
            "this week" or "week" => (today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday), today.AddDays(7 - (int)today.DayOfWeek)),
            "past 7 days" or "last 7 days" or "7 days" => (today.AddDays(-6), today),
            "past 30 days" or "last 30 days" or "30 days" or "this month" or "month" => (today.AddDays(-29), today),
            "past dates" or "past" => (null, today.AddDays(-1)),
            "upcoming" or "future" => (today.AddDays(1), null),
            _ => (null, null)
        };
    }

    private static string NormalizeSection(string? section)
    {
        if (string.IsNullOrWhiteSpace(section)) return "Section A";
        var trimmed = section.Trim();
        if (trimmed.StartsWith("Section ", StringComparison.OrdinalIgnoreCase)) return trimmed;
        return $"Section {trimmed.ToUpper()}";
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value)) return "\"\"";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return $"\"{value}\"";
    }
}
