using Microsoft.EntityFrameworkCore;
using SpaceBook.Application.DTOs.Reports;
using SpaceBook.Application.Interfaces;
using SpaceBook.Infrastructure.Data;

namespace SpaceBook.Infrastructure.Repositories;

public class ReportRepository : IReportRepository
{
    private readonly ApplicationDbContext _context;

    public ReportRepository(ApplicationDbContext context)
    {
        _context = context;
    }


    // =========================================================
    // BOOKING TREND
    // =========================================================

    public async Task<BookingTrendDto> GetBookingTrendAsync(
        ReportFilterDto filter)
    {
        var isHotseatsOnly = string.Equals(filter.ReportType, "Hotseats", StringComparison.OrdinalIgnoreCase);
        var isAll = string.Equals(filter.ReportType, "All", StringComparison.OrdinalIgnoreCase);

        // Hotseats Query
        var hotseatQuery = _context.HotseatBookings
            .AsNoTracking()
            .Include(h => h.Seat)
                .ThenInclude(s => s!.Module)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Module))
        {
            var moduleName = filter.Module.Trim();
            hotseatQuery = hotseatQuery.Where(h =>
                h.Seat != null &&
                h.Seat.Module != null &&
                h.Seat.Module.ModuleName == moduleName);
        }

        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            hotseatQuery = hotseatQuery.Where(h => h.BookingStatus == filter.Status);
        }

        // Rooms Query
        var roomQuery = _context.Bookings
            .AsNoTracking()
            .Include(b => b.Room)
                .ThenInclude(r => r!.RoomType)
            .Include(b => b.Room)
                .ThenInclude(r => r!.Module)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Module))
        {
            var moduleName = filter.Module.Trim();
            roomQuery = roomQuery.Where(b =>
                b.Room != null &&
                b.Room.Module != null &&
                b.Room.Module.ModuleName == moduleName);
        }

        if (filter.RoomTypeId.HasValue)
        {
            roomQuery = roomQuery.Where(b =>
                b.Room != null &&
                b.Room.RoomTypeId == filter.RoomTypeId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            roomQuery = roomQuery.Where(b => b.Status == filter.Status);
        }

        if (isHotseatsOnly)
        {
            var hotseats = await hotseatQuery.ToListAsync();
            int total = hotseats.Count;
            int unique = hotseats.Select(h => h.SeatId).Distinct().Count();
            double confirmedRate = total == 0 ? 0 : Math.Round(hotseats.Count(h => h.BookingStatus == "Confirmed" || h.BookingStatus == "CheckedIn") * 100.0 / total, 2);

            return new BookingTrendDto
            {
                TotalBookings = total,
                UniqueRooms = unique,
                ConfirmedRate = confirmedRate,
                AverageDuration = "Full Day",
                Chart = hotseats.GroupBy(h => h.BookingStatus)
                    .Select(g => new BookingTrendChartDto { Label = g.Key, Count = g.Count() })
                    .ToList()
            };
        }
        else if (isAll)
        {
            var rooms = await roomQuery.ToListAsync();
            var hotseats = await hotseatQuery.ToListAsync();
            int total = rooms.Count + hotseats.Count;
            int unique = rooms.Select(r => r.RoomId).Distinct().Count() + hotseats.Select(h => h.SeatId).Distinct().Count();
            int confirmedCount = rooms.Count(r => r.Status == "Approved") + hotseats.Count(h => h.BookingStatus == "Confirmed" || h.BookingStatus == "CheckedIn");
            double confirmedRate = total == 0 ? 0 : Math.Round(confirmedCount * 100.0 / total, 2);

            var chartList = new List<BookingTrendChartDto>
            {
                new BookingTrendChartDto { Label = "Room Bookings", Count = rooms.Count },
                new BookingTrendChartDto { Label = "Hotseat Bookings", Count = hotseats.Count }
            };

            return new BookingTrendDto
            {
                TotalBookings = total,
                UniqueRooms = unique,
                ConfirmedRate = confirmedRate,
                AverageDuration = "Mixed",
                Chart = chartList
            };
        }
        else
        {
            var bookings = await roomQuery.ToListAsync();
            int total = bookings.Count;
            int unique = bookings.Select(b => b.RoomId).Distinct().Count();
            double confirmedRate = total == 0 ? 0 : Math.Round(bookings.Count(b => b.Status == "Approved") * 100.0 / total, 2);

            return new BookingTrendDto
            {
                TotalBookings = total,
                UniqueRooms = unique,
                ConfirmedRate = confirmedRate,
                AverageDuration = "0h 0m",
                Chart = bookings.GroupBy(b => b.Status)
                    .Select(g => new BookingTrendChartDto { Label = g.Key, Count = g.Count() })
                    .ToList()
            };
        }
    }


    // =========================================================
    // BOOKING STATUS
    // =========================================================

    public async Task<List<BookingStatusDto>>
        GetBookingStatusAsync(
            ReportFilterDto filter)
    {
        var isHotseatsOnly = string.Equals(filter.ReportType, "Hotseats", StringComparison.OrdinalIgnoreCase);
        var isAll = string.Equals(filter.ReportType, "All", StringComparison.OrdinalIgnoreCase);

        if (isHotseatsOnly)
        {
            var query = _context.HotseatBookings
                .AsNoTracking()
                .Include(h => h.Seat)
                    .ThenInclude(s => s!.Module)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Module))
            {
                var moduleName = filter.Module.Trim();
                query = query.Where(h =>
                    h.Seat != null &&
                    h.Seat.Module != null &&
                    h.Seat.Module.ModuleName == moduleName);
            }

            return await query
                .GroupBy(h => h.BookingStatus)
                .Select(g => new BookingStatusDto
                {
                    Status = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();
        }
        else if (isAll)
        {
            var roomStatuses = await _context.Bookings
                .AsNoTracking()
                .GroupBy(b => b.Status)
                .Select(g => new BookingStatusDto { Status = $"Room: {g.Key}", Count = g.Count() })
                .ToListAsync();

            var hotseatStatuses = await _context.HotseatBookings
                .AsNoTracking()
                .GroupBy(h => h.BookingStatus)
                .Select(g => new BookingStatusDto { Status = $"Hotseat: {g.Key}", Count = g.Count() })
                .ToListAsync();

            return roomStatuses.Concat(hotseatStatuses).ToList();
        }
        else
        {
            var query = _context.Bookings
                .AsNoTracking()
                .Include(b => b.Room)
                    .ThenInclude(r => r!.Module)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Module))
            {
                var moduleName = filter.Module.Trim();
                query = query.Where(b =>
                    b.Room != null &&
                    b.Room.Module != null &&
                    b.Room.Module.ModuleName == moduleName);
            }

            if (filter.RoomTypeId.HasValue)
            {
                query = query.Where(b =>
                    b.Room != null &&
                    b.Room.RoomTypeId ==
                        filter.RoomTypeId.Value);
            }

            return await query
                .GroupBy(b => b.Status)
                .Select(g => new BookingStatusDto
                {
                    Status = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();
        }
    }


    // =========================================================
    // ROOM USAGE
    // =========================================================

    public async Task<List<RoomUsageDto>>
        GetRoomUsageAsync(
            ReportFilterDto filter)
    {
        var isHotseatsOnly = string.Equals(filter.ReportType, "Hotseats", StringComparison.OrdinalIgnoreCase);
        var isAll = string.Equals(filter.ReportType, "All", StringComparison.OrdinalIgnoreCase);

        if (isHotseatsOnly)
        {
            var query = _context.HotseatBookings
                .AsNoTracking()
                .Include(h => h.Seat)
                    .ThenInclude(s => s!.Module)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Module))
            {
                var moduleName = filter.Module.Trim();
                query = query.Where(h =>
                    h.Seat != null &&
                    h.Seat.Module != null &&
                    h.Seat.Module.ModuleName == moduleName);
            }

            return await query
                .GroupBy(h => h.Seat != null && h.Seat.Module != null ? $"Hotseat ({h.Seat.Module.ModuleName})" : "Hotseat Desks")
                .Select(g => new RoomUsageDto
                {
                    RoomType = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();
        }
        else if (isAll)
        {
            var roomUsage = await _context.Bookings
                .AsNoTracking()
                .Where(b => b.Room != null && b.Room.RoomType != null)
                .GroupBy(b => b.Room!.RoomType!.TypeName)
                .Select(g => new RoomUsageDto { RoomType = g.Key, Count = g.Count() })
                .ToListAsync();

            var hotseatCount = await _context.HotseatBookings.CountAsync();
            if (hotseatCount > 0)
            {
                roomUsage.Add(new RoomUsageDto { RoomType = "Hotseat Desks", Count = hotseatCount });
            }

            return roomUsage;
        }
        else
        {
            var query = _context.Bookings
                .AsNoTracking()
                .Include(b => b.Room)
                    .ThenInclude(r => r!.RoomType)
                .Include(b => b.Room)
                    .ThenInclude(r => r!.Module)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Module))
            {
                var moduleName = filter.Module.Trim();
                query = query.Where(b =>
                    b.Room != null &&
                    b.Room.Module != null &&
                    b.Room.Module.ModuleName == moduleName);
            }

            if (filter.RoomTypeId.HasValue)
            {
                query = query.Where(b =>
                    b.Room != null &&
                    b.Room.RoomTypeId ==
                        filter.RoomTypeId.Value);
            }

            return await query
                .Where(b =>
                    b.Room != null &&
                    b.Room.RoomType != null)
                .GroupBy(b =>
                    b.Room!.RoomType!.TypeName)
                .Select(g =>
                    new RoomUsageDto
                    {
                        RoomType = g.Key,
                        Count = g.Count()
                    })
                .ToListAsync();
        }
    }

    // =========================================================
    // WORKPLACE ANALYTICS & INTELLIGENCE
    // =========================================================

    public async Task<WorkplaceAnalyticsDto> GetWorkplaceAnalyticsAsync(
        ReportFilterDto filter)
    {
        var now = DateTime.Now;
        var today = DateOnly.FromDateTime(now);

        // Determine Date Range from Timeframe
        DateOnly? startDate = filter.StartDate;
        DateOnly? endDate = filter.EndDate;

        if (!string.IsNullOrWhiteSpace(filter.Timeframe))
        {
            var tf = filter.Timeframe.Trim().ToLowerInvariant();
            if (tf.Contains("7") || tf.Contains("week"))
            {
                startDate = today.AddDays(-7);
                endDate = today;
            }
            else if (tf.Contains("30"))
            {
                startDate = today.AddDays(-30);
                endDate = today;
            }
            else if (tf.Contains("month"))
            {
                startDate = new DateOnly(today.Year, today.Month, 1);
                endDate = today;
            }
        }

        // Room Bookings Query
        var roomQuery = _context.Bookings
            .AsNoTracking()
            .Include(b => b.Employee)
            .Include(b => b.Room).ThenInclude(r => r!.Module)
            .Include(b => b.Room).ThenInclude(r => r!.RoomType)
            .AsQueryable();

        if (startDate.HasValue)
        {
            roomQuery = roomQuery.Where(b => b.BookingDate >= startDate.Value);
        }
        if (endDate.HasValue)
        {
            roomQuery = roomQuery.Where(b => b.BookingDate <= endDate.Value);
        }
        if (!string.IsNullOrWhiteSpace(filter.Module) && !filter.Module.Equals("All", StringComparison.OrdinalIgnoreCase) && !filter.Module.Equals("All Modules", StringComparison.OrdinalIgnoreCase))
        {
            var moduleName = filter.Module.Trim();
            roomQuery = roomQuery.Where(b =>
                b.Room != null &&
                b.Room.Module != null &&
                (b.Room.Module.ModuleName == moduleName || b.Room.Module.ModuleName.Contains(moduleName)));
        }
        if (filter.RoomTypeId.HasValue)
        {
            roomQuery = roomQuery.Where(b => b.Room != null && b.Room.RoomTypeId == filter.RoomTypeId.Value);
        }
        if (!string.IsNullOrWhiteSpace(filter.Status) && !filter.Status.Equals("All", StringComparison.OrdinalIgnoreCase) && !filter.Status.Equals("All Statuses", StringComparison.OrdinalIgnoreCase))
        {
            roomQuery = roomQuery.Where(b => b.Status == filter.Status);
        }

        var bookings = await roomQuery.ToListAsync();

        // 1. Total Reservations
        int totalReservations = bookings.Count;

        // 2. Active Rooms Count
        int activeRoomsCount = bookings.Select(b => b.RoomId).Distinct().Count();

        // 3. Confirmed Bookings (Approved)
        int confirmedBookings = bookings.Count(b => b.Status == "Approved" || b.Status == "Confirmed");
        double confirmedRate = totalReservations > 0
            ? Math.Round(confirmedBookings * 100.0 / totalReservations, 1)
            : 0;

        // 4. Cancelled Bookings
        int cancelledBookings = bookings.Count(b => b.Status == "Cancelled");
        double cancelledRate = totalReservations > 0
            ? Math.Round(cancelledBookings * 100.0 / totalReservations, 1)
            : 0;

        // 5. Workforce Engagement
        int activeTeamMembers = bookings.Select(b => b.EmployeeId).Distinct().Count();
        double avgPerPerson = activeTeamMembers > 0
            ? Math.Round((double)totalReservations / activeTeamMembers, 1)
            : 0;

        // 6. Employee Booking vs Cancellation Ratio
        var employeeRatios = bookings
            .GroupBy(b => new { b.EmployeeId, Name = b.Employee?.Name ?? $"Employee {b.EmployeeId}" })
            .Select(g => new EmployeeBookingRatioDto
            {
                EmployeeName = g.Key.Name,
                ConfirmedCount = g.Count(b => b.Status == "Approved" || b.Status == "Confirmed"),
                CancelledCount = g.Count(b => b.Status == "Cancelled")
            })
            .OrderByDescending(e => e.ConfirmedCount + e.CancelledCount)
            .Take(10)
            .ToList();

        // 7. Reservation Outcome Breakdown (Donut)
        var outcomeBreakdown = new List<OutcomeBreakdownDto>();
        if (confirmedBookings > 0 || totalReservations == 0)
        {
            outcomeBreakdown.Add(new OutcomeBreakdownDto
            {
                Status = "Confirmed",
                Count = confirmedBookings,
                Percentage = confirmedRate
            });
        }
        if (cancelledBookings > 0)
        {
            outcomeBreakdown.Add(new OutcomeBreakdownDto
            {
                Status = "Cancelled",
                Count = cancelledBookings,
                Percentage = cancelledRate
            });
        }
        var otherCount = totalReservations - confirmedBookings - cancelledBookings;
        if (otherCount > 0)
        {
            outcomeBreakdown.Add(new OutcomeBreakdownDto
            {
                Status = "Pending / Other",
                Count = otherCount,
                Percentage = Math.Round(otherCount * 100.0 / totalReservations, 1)
            });
        }

        // 8. Reservation Volume Trendline
        var trendline = bookings
            .GroupBy(b => b.Status)
            .Select(g => new TrendlinePointDto
            {
                Label = g.Key,
                Count = g.Count()
            })
            .ToList();

        // Ensure key labels exist for smooth curve
        var requiredLabels = new[] { "Approved", "Pending", "Cancelled", "Rejected" };
        foreach (var label in requiredLabels)
        {
            if (!trendline.Any(t => t.Label.Equals(label, StringComparison.OrdinalIgnoreCase)))
            {
                trendline.Add(new TrendlinePointDto { Label = label, Count = 0 });
            }
        }
        trendline = trendline.OrderBy(t => Array.IndexOf(requiredLabels, t.Label) >= 0 ? Array.IndexOf(requiredLabels, t.Label) : 99).ToList();

        // 9. Most Reserved Rooms & Workspaces
        var mostReserved = bookings
            .Where(b => b.Room != null)
            .GroupBy(b => new { b.RoomId, RoomName = b.Room!.RoomName, ModuleName = b.Room.Module?.ModuleName ?? string.Empty })
            .Select(g => new PopularWorkspaceDto
            {
                RoomName = g.Key.RoomName,
                ModuleName = g.Key.ModuleName,
                BookingCount = g.Count()
            })
            .OrderByDescending(r => r.BookingCount)
            .Take(6)
            .ToList();

        // 10. Top Cancellation Reasons & Drivers
        var cancellationDrivers = bookings
            .Where(b => b.Status == "Cancelled")
            .GroupBy(b => !string.IsNullOrWhiteSpace(b.CancellationReason) ? b.CancellationReason.Trim() : "General Schedule Conflict")
            .Select(g => new CancellationDriverDto
            {
                Reason = g.Key,
                Count = g.Count(),
                Percentage = cancelledBookings > 0 ? Math.Round(g.Count() * 100.0 / cancelledBookings, 1) : 0
            })
            .OrderByDescending(c => c.Count)
            .Take(5)
            .ToList();

        // 11. Peak Workspace Demand by Hour (10:00 to 22:00)
        var peakDemand = new List<HourlyDemandDto>();
        for (int h = 10; h <= 22; h++)
        {
            var countAtHour = bookings.Count(b => b.StartTime.Hour == h);

            peakDemand.Add(new HourlyDemandDto
            {
                Hour = $"{h:D2}:00",
                HourNumber = h,
                Count = countAtHour
            });
        }

        return new WorkplaceAnalyticsDto
        {
            TotalReservations = totalReservations,
            ActiveRoomsCount = activeRoomsCount,
            ConfirmedBookings = confirmedBookings,
            ConfirmedRate = confirmedRate,
            CancelledBookings = cancelledBookings,
            CancelledRate = cancelledRate,
            ActiveTeamMembersCount = activeTeamMembers,
            AvgBookingsPerPerson = avgPerPerson,
            EmployeeRatios = employeeRatios,
            OutcomeBreakdown = outcomeBreakdown,
            Trendline = trendline,
            MostReservedWorkspaces = mostReserved,
            TopCancellationDrivers = cancellationDrivers,
            PeakDemandByHour = peakDemand
        };
    }

    // =========================================================
    // EXPORT ELABORATE BOOKINGS CSV
    // =========================================================

    public async Task<byte[]> ExportBookingsCsvAsync(
        ReportFilterDto filter)
    {
        var isHotseatsOnly = string.Equals(filter.ReportType, "Hotseats", StringComparison.OrdinalIgnoreCase);
        var isAll = string.Equals(filter.ReportType, "All", StringComparison.OrdinalIgnoreCase);

        var sb = new System.Text.StringBuilder();

        if (isHotseatsOnly)
        {
            var query = _context.HotseatBookings
                .AsNoTracking()
                .Include(h => h.Employee)
                .Include(h => h.Seat)
                    .ThenInclude(s => s!.Module)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Module))
            {
                var moduleName = filter.Module.Trim();
                query = query.Where(h =>
                    h.Seat != null &&
                    h.Seat.Module != null &&
                    h.Seat.Module.ModuleName == moduleName);
            }

            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                query = query.Where(h => h.BookingStatus == filter.Status);
            }

            var hotseats = await query
                .OrderByDescending(h => h.BookingDate)
                .ThenByDescending(h => h.BookedOn)
                .ToListAsync();

            sb.AppendLine("Reservation Type,Hotseat Booking ID,Employee Name,Employee Email,Department,Seat Number,Module,Section,Row,Column,Booking Date,Status,Booked On,Check-In Deadline,Check-In Time,Released On");

            foreach (var h in hotseats)
            {
                var empName = EscapeCsv(h.Employee?.Name);
                var empEmail = EscapeCsv(h.Employee?.Email);
                var empDept = EscapeCsv(h.Employee?.Department);
                var seatNum = EscapeCsv(h.Seat?.SeatNumber);
                var moduleName = EscapeCsv(h.Seat?.Module?.ModuleName);
                var section = EscapeCsv(h.Seat?.Section);
                var row = EscapeCsv(h.Seat?.RowNumber);
                var col = h.Seat?.ColumnNumber.ToString() ?? "";
                var bookingDate = h.BookingDate.ToString("yyyy-MM-dd");
                var status = EscapeCsv(h.BookingStatus);
                var bookedOn = h.BookedOn.ToString("yyyy-MM-dd HH:mm:ss");
                var checkInDeadline = h.CheckInDeadline?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
                var checkInTime = h.CheckInTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
                var releasedOn = h.ReleasedOn?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";

                sb.AppendLine($"\"Hotseat Booking\",{h.HotseatBookingId},{empName},{empEmail},{empDept},{seatNum},{moduleName},{section},{row},{col},{bookingDate},{status},{bookedOn},{checkInDeadline},{checkInTime},{releasedOn}");
            }
        }
        else if (isAll)
        {
            // Unified Combined CSV
            sb.AppendLine("Reservation Type,Booking ID,Employee Name,Employee Email,Department,Resource Name,Module,Type / Category,Booking Date,Time Window,Status,Booked On,Details / Reason");

            // 1. Room Bookings
            var roomQuery = _context.Bookings
                .AsNoTracking()
                .Include(b => b.Employee)
                .Include(b => b.Room).ThenInclude(r => r!.RoomType)
                .Include(b => b.Room).ThenInclude(r => r!.Module)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Module))
            {
                var moduleName = filter.Module.Trim();
                roomQuery = roomQuery.Where(b => b.Room != null && b.Room.Module != null && b.Room.Module.ModuleName == moduleName);
            }
            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                roomQuery = roomQuery.Where(b => b.Status == filter.Status);
            }

            var roomBookings = await roomQuery.OrderByDescending(b => b.BookingDate).ToListAsync();
            foreach (var b in roomBookings)
            {
                var empName = EscapeCsv(b.Employee?.Name);
                var empEmail = EscapeCsv(b.Employee?.Email);
                var empDept = EscapeCsv(b.Employee?.Department);
                var resourceName = EscapeCsv(b.Room?.RoomName ?? $"Room {b.RoomId}");
                var moduleName = EscapeCsv(b.Room?.Module?.ModuleName);
                var type = EscapeCsv(b.Room?.RoomType?.TypeName ?? "Meeting Room");
                var bookingDate = b.BookingDate.ToString("yyyy-MM-dd");
                var timeWindow = $"\"{b.StartTime:HH:mm} - {b.EndTime:HH:mm}\"";
                var status = EscapeCsv(b.Status);
                var bookedOn = b.BookedOn.ToString("yyyy-MM-dd HH:mm:ss");
                var details = EscapeCsv(b.MeetingTitle);

                sb.AppendLine($"\"Room Booking\",{b.BookingId},{empName},{empEmail},{empDept},{resourceName},{moduleName},{type},{bookingDate},{timeWindow},{status},{bookedOn},{details}");
            }

            // 2. Hotseat Bookings
            var hotseatQuery = _context.HotseatBookings
                .AsNoTracking()
                .Include(h => h.Employee)
                .Include(h => h.Seat).ThenInclude(s => s!.Module)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Module))
            {
                var moduleName = filter.Module.Trim();
                hotseatQuery = hotseatQuery.Where(h => h.Seat != null && h.Seat.Module != null && h.Seat.Module.ModuleName == moduleName);
            }
            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                hotseatQuery = hotseatQuery.Where(h => h.BookingStatus == filter.Status);
            }

            var hotseats = await hotseatQuery.OrderByDescending(h => h.BookingDate).ToListAsync();
            foreach (var h in hotseats)
            {
                var empName = EscapeCsv(h.Employee?.Name);
                var empEmail = EscapeCsv(h.Employee?.Email);
                var empDept = EscapeCsv(h.Employee?.Department);
                var resourceName = EscapeCsv(h.Seat?.SeatNumber ?? $"Seat {h.SeatId}");
                var moduleName = EscapeCsv(h.Seat?.Module?.ModuleName);
                var type = "\"Hotseat Desk\"";
                var bookingDate = h.BookingDate.ToString("yyyy-MM-dd");
                var timeWindow = "\"Full Day\"";
                var status = EscapeCsv(h.BookingStatus);
                var bookedOn = h.BookedOn.ToString("yyyy-MM-dd HH:mm:ss");
                var details = EscapeCsv($"Section: {h.Seat?.Section}, Row: {h.Seat?.RowNumber}");

                sb.AppendLine($"\"Hotseat Booking\",{h.HotseatBookingId},{empName},{empEmail},{empDept},{resourceName},{moduleName},{type},{bookingDate},{timeWindow},{status},{bookedOn},{details}");
            }
        }
        else
        {
            // Default: Rooms
            var query = _context.Bookings
                .AsNoTracking()
                .Include(b => b.Employee)
                .Include(b => b.Room)
                    .ThenInclude(r => r!.RoomType)
                .Include(b => b.Room)
                    .ThenInclude(r => r!.Module)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Module))
            {
                var moduleName = filter.Module.Trim();
                query = query.Where(b =>
                    b.Room != null &&
                    b.Room.Module != null &&
                    b.Room.Module.ModuleName == moduleName);
            }

            if (filter.RoomTypeId.HasValue)
            {
                query = query.Where(b =>
                    b.Room != null &&
                    b.Room.RoomTypeId == filter.RoomTypeId.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                query = query.Where(b => b.Status == filter.Status);
            }

            var bookings = await query
                .OrderByDescending(b => b.BookingDate)
                .ThenByDescending(b => b.StartTime)
                .ToListAsync();

            // CSV Header with full Employee and Room details (Purpose removed)
            sb.AppendLine("Booking ID,Meeting Title,Employee Name,Employee Email,Department,Room Name,Room Number,Module,Room Type,Capacity,Participant Count,Booking Date,Start Time,End Time,Status,Booked On,Cancellation Reason");

            foreach (var b in bookings)
            {
                var employeeName = EscapeCsv(b.Employee?.Name);
                var employeeEmail = EscapeCsv(b.Employee?.Email);
                var department = EscapeCsv(b.Employee?.Department);
                var meetingTitle = EscapeCsv(b.MeetingTitle);
                var roomName = EscapeCsv(b.Room?.RoomName);
                var roomNumber = EscapeCsv(b.Room?.RoomNumber);
                var moduleName = EscapeCsv(b.Room?.Module?.ModuleName);
                var roomType = EscapeCsv(b.Room?.RoomType?.TypeName);
                var capacity = b.Room?.Capacity.ToString() ?? "";
                var participantCount = b.ParticipantCount.ToString();
                var bookingDate = b.BookingDate.ToString("yyyy-MM-dd");
                var startTime = b.StartTime.ToString("HH:mm:ss");
                var endTime = b.EndTime.ToString("HH:mm:ss");
                var status = EscapeCsv(b.Status);
                var bookedOn = b.BookedOn.ToString("yyyy-MM-dd HH:mm:ss");
                var cancellationReason = EscapeCsv(b.CancellationReason);

                sb.AppendLine($"{b.BookingId},{meetingTitle},{employeeName},{employeeEmail},{department},{roomName},{roomNumber},{moduleName},{roomType},{capacity},{participantCount},{bookingDate},{startTime},{endTime},{status},{bookedOn},{cancellationReason}");
            }
        }

        // Return UTF-8 bytes with BOM for Excel compatibility
        var utf8WithBom = new System.Text.UTF8Encoding(true);
        return utf8WithBom.GetBytes(sb.ToString());
    }

    private static string EscapeCsv(string? field)
    {
        if (string.IsNullOrEmpty(field))
        {
            return "\"\"";
        }
        return $"\"{field.Replace("\"", "\"\"")}\"";
    }
}