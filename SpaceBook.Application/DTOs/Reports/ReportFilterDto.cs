namespace SpaceBook.Application.DTOs.Reports;

public class ReportFilterDto
{
    public string? ReportType { get; set; }

    public string? Timeframe { get; set; } // "7days", "30days", "month", "all"

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public string? Module { get; set; }

    public int? RoomTypeId { get; set; }

    public string? Status { get; set; }
}