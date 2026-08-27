namespace SpaceBook.Application.DTOs.Copilot;

public class HotseatCopilotDto
{
    public int SeatId { get; set; }

    public string SeatNumber { get; set; } = string.Empty;

    public string Section { get; set; } = string.Empty;

    public string RowNumber { get; set; } = string.Empty;

    public int ColumnNumber { get; set; }

    public int ModuleId { get; set; }

    public string ModuleName { get; set; } = string.Empty;

    public string OfficeName { get; set; } = string.Empty;

    public string LocationName { get; set; } = string.Empty;

    public string Status { get; set; } = "Available";

    public string? BookingStatus { get; set; }

    public int? CurrentBookingId { get; set; }

    public string? BookedByEmployeeName { get; set; }

    public string? ExpectedCheckInTime { get; set; }
}
