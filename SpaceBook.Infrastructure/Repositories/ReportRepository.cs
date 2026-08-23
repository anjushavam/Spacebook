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
                var details = EscapeCsv(!string.IsNullOrWhiteSpace(b.MeetingTitle) ? b.MeetingTitle : b.Purpose);

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

            // CSV Header with full Employee and Room details
            sb.AppendLine("Booking ID,Meeting Title,Purpose,Employee Name,Employee Email,Department,Room Name,Room Number,Module,Room Type,Capacity,Participant Count,Booking Date,Start Time,End Time,Status,Booked On,Cancellation Reason");

            foreach (var b in bookings)
            {
                var employeeName = EscapeCsv(b.Employee?.Name);
                var employeeEmail = EscapeCsv(b.Employee?.Email);
                var department = EscapeCsv(b.Employee?.Department);
                var meetingTitle = EscapeCsv(b.MeetingTitle);
                var purpose = EscapeCsv(b.Purpose);
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

                sb.AppendLine($"{b.BookingId},{meetingTitle},{purpose},{employeeName},{employeeEmail},{department},{roomName},{roomNumber},{moduleName},{roomType},{capacity},{participantCount},{bookingDate},{startTime},{endTime},{status},{bookedOn},{cancellationReason}");
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