namespace SpaceBook.Application.DTOs.Hotseat;

public class HotseatManagementDashboardDto
{
    // =========================================================
    // KPI METRICS (Top Summary Cards)
    // =========================================================

    public int TotalReservations { get; set; }

    public int ActiveHotseatsCount { get; set; }

    public double TotalVolumePercentage { get; set; } = 100.0;

    public double Utilization { get; set; }

    public int ConfirmedBookings { get; set; }

    public double ConfirmedRate { get; set; }

    public int CheckedInBookings { get; set; }

    public double CheckedInRate { get; set; }

    public int ReleasedBookings { get; set; }

    public double ReleasedRate { get; set; }

    public int ExpiredBookings { get; set; }

    public double ExpiredRate { get; set; }

    public int CancelledBookings { get; set; }

    public double CancelledRate { get; set; }

    public int TotalBookingsAnalyzed { get; set; }

    // =========================================================
    // CHARTS DATA
    // =========================================================

    /// <summary>
    /// Chart 1: Donut chart showing workstation reservation share across modules/facilities
    /// </summary>
    public List<HotseatVolumeByFacilityZoneDto> VolumeByFacilityZone { get; set; } = new();

    /// <summary>
    /// Chart 2: Bar chart showing hotseat bookings distributed across floor sections A, B, C, and D
    /// </summary>
    public List<FloorSectionDemandDto> FloorSectionDemand { get; set; } = new();

    /// <summary>
    /// Chart 3: Area/Line chart tracking workstation check-ins/velocity across days/weeks
    /// </summary>
    public List<DailyHotseatOccupancyTrendDto> DailyOccupancyTrendline { get; set; } = new();

    /// <summary>
    /// Chart 4: Horizontal bar chart showing highest-occupied individual desk units ranked by frequency
    /// </summary>
    public List<TopInDemandDeskDto> TopInDemandDesks { get; set; } = new();

    /// <summary>
    /// Chart 5: Bar chart showing distribution of expected check-in slots throughout office hours
    /// </summary>
    public List<PeakCheckInSlotDto> PeakCheckInSlots { get; set; } = new();
}

public class HotseatVolumeByFacilityZoneDto
{
    public string Label { get; set; } = string.Empty;

    public string ModuleName { get; set; } = string.Empty;

    public string FacilityName { get; set; } = string.Empty;

    public int BookingCount { get; set; }

    public double Percentage { get; set; }
}

public class FloorSectionDemandDto
{
    public string Section { get; set; } = string.Empty;

    public int BookingCount { get; set; }

    public double Percentage { get; set; }
}

public class DailyHotseatOccupancyTrendDto
{
    public string Date { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public int CheckInsCount { get; set; }

    public int TotalBookingsCount { get; set; }
}

public class TopInDemandDeskDto
{
    public int SeatId { get; set; }

    public string DeskNumber { get; set; } = string.Empty;

    public string? Section { get; set; }

    public string ModuleName { get; set; } = string.Empty;

    public string OfficeName { get; set; } = string.Empty;

    public int ReservationCount { get; set; }
}

public class PeakCheckInSlotDto
{
    public string TimeSlot { get; set; } = string.Empty;

    public string StartTime { get; set; } = string.Empty;

    public string EndTime { get; set; } = string.Empty;

    public int CheckInSlotsCount { get; set; }

    public double Percentage { get; set; }

    public bool IsPeak { get; set; }
}
