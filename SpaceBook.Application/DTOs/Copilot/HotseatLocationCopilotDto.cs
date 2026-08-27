namespace SpaceBook.Application.DTOs.Copilot;

public class HotseatLocationCopilotDto
{
    public string LocationName { get; set; } = string.Empty;

    public List<HotseatOfficeSummaryDto> Offices { get; set; } = new();

    public int TotalSeats { get; set; }

    public int AvailableSeats { get; set; }

    public int BookedSeats { get; set; }

    public int CancelledBookings { get; set; }

    public int ExpiredBookings { get; set; }
}

public class HotseatOfficeSummaryDto
{
    public int OfficeId { get; set; }

    public string OfficeName { get; set; } = string.Empty;

    public List<HotseatModuleSummaryDto> Modules { get; set; } = new();

    public int TotalSeats { get; set; }

    public int AvailableSeats { get; set; }

    public int BookedSeats { get; set; }

    public int CancelledBookings { get; set; }

    public int ExpiredBookings { get; set; }
}

public class HotseatModuleSummaryDto
{
    public int ModuleId { get; set; }

    public string ModuleName { get; set; } = string.Empty;

    public List<string> Sections { get; set; } = new();

    public int TotalSeats { get; set; }

    public int AvailableSeats { get; set; }

    public int BookedSeats { get; set; }

    public int CheckedInSeats { get; set; }

    public int CancelledBookings { get; set; }

    public int ExpiredBookings { get; set; }
}
