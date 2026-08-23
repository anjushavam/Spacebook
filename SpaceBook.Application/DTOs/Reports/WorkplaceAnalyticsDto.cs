namespace SpaceBook.Application.DTOs.Reports;

public class WorkplaceAnalyticsDto
{
    // =========================================================
    // KPI METRICS
    // =========================================================

    public int TotalReservations { get; set; }

    public int ActiveRoomsCount { get; set; }

    public int ConfirmedBookings { get; set; }

    public double ConfirmedRate { get; set; }

    public int CancelledBookings { get; set; }

    public double CancelledRate { get; set; }

    public int ActiveTeamMembersCount { get; set; }

    public double AvgBookingsPerPerson { get; set; }

    // =========================================================
    // CHARTS DATA
    // =========================================================

    public List<EmployeeBookingRatioDto> EmployeeRatios { get; set; } = new();

    public List<OutcomeBreakdownDto> OutcomeBreakdown { get; set; } = new();

    public List<TrendlinePointDto> Trendline { get; set; } = new();

    public List<PopularWorkspaceDto> MostReservedWorkspaces { get; set; } = new();

    public List<CancellationDriverDto> TopCancellationDrivers { get; set; } = new();

    public List<HourlyDemandDto> PeakDemandByHour { get; set; } = new();
}

public class EmployeeBookingRatioDto
{
    public string EmployeeName { get; set; } = string.Empty;

    public int ConfirmedCount { get; set; }

    public int CancelledCount { get; set; }

    public int TotalCount => ConfirmedCount + CancelledCount;
}

public class OutcomeBreakdownDto
{
    public string Status { get; set; } = string.Empty;

    public int Count { get; set; }

    public double Percentage { get; set; }
}

public class TrendlinePointDto
{
    public string Label { get; set; } = string.Empty;

    public int Count { get; set; }
}

public class PopularWorkspaceDto
{
    public string RoomName { get; set; } = string.Empty;

    public string ModuleName { get; set; } = string.Empty;

    public int BookingCount { get; set; }
}

public class CancellationDriverDto
{
    public string Reason { get; set; } = string.Empty;

    public int Count { get; set; }

    public double Percentage { get; set; }
}

public class HourlyDemandDto
{
    public string Hour { get; set; } = string.Empty;

    public int HourNumber { get; set; }

    public int Count { get; set; }
}
