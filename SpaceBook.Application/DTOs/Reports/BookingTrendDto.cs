namespace SpaceBook.Application.DTOs.Reports;

public class BookingTrendDto
{
    public int TotalBookings { get; set; }

    public int UniqueRooms { get; set; }

    public double ConfirmedRate { get; set; }

    public string AverageDuration { get; set; } = string.Empty;

    public List<BookingTrendChartDto> Chart { get; set; } = new();
}

public class BookingTrendChartDto
{
    public string Label { get; set; } = string.Empty;

    public int Count { get; set; }
}