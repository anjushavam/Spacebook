namespace SpaceBook.Application.DTOs.Reports;

public class BookingTrendDto
{
    public int TotalBookings { get; set; }

    public int TotalReservations { get; set; }

    public int UniqueRooms { get; set; }

    public int ActiveRoomsCount { get; set; }

    public int ConfirmedBookings { get; set; }

    public double ConfirmedRate { get; set; }

    public int CancelledBookings { get; set; }

    public double CancelledRate { get; set; }

    public double Utilization { get; set; }

    public double OccupancyRate { get; set; }

    public double UtilizationRate { get; set; }

    public double UtilizationPercentage { get; set; }

    public double Occupancy { get; set; }

    public string AverageDuration { get; set; } = string.Empty;

    public List<BookingTrendChartDto> Chart { get; set; } = new();
}

public class BookingTrendChartDto
{
    public string Label { get; set; } = string.Empty;

    public int Count { get; set; }
}