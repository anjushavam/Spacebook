namespace SpaceBook.Application.DTOs.Hotseat;

public class HotseatAuditRecordDto
{
    public int HotseatBookingId { get; set; }

    public int SeatId { get; set; }

    public string SeatNumber { get; set; } = string.Empty;

    public string Section { get; set; } = string.Empty;

    public string RowNumber { get; set; } = string.Empty;

    public int ColumnNumber { get; set; }

    public string ModuleName { get; set; } = string.Empty;

    public string OfficeName { get; set; } = string.Empty;

    public string LocationName { get; set; } = string.Empty;

    public int EmployeeId { get; set; }

    public string EmployeeName { get; set; } = string.Empty;

    public string EmployeeEmail { get; set; } = string.Empty;

    public string Department { get; set; } = string.Empty;

    public DateOnly BookingDate { get; set; }

    public string BookingStatus { get; set; } = string.Empty;

    public string? ExpectedCheckInTime { get; set; }

    public string? CheckInDeadline { get; set; }

    public string? CheckInTime { get; set; }

    public string? BookedOn { get; set; }

    public string? ReleasedOn { get; set; }

    public string? RecordIngestedBy { get; set; }

    public DateTime? RecordIngestedOn { get; set; }

    public string? RecordModifiedBy { get; set; }

    public DateTime? RecordModifiedOn { get; set; }
}

public class HotseatAuditPagedResultDto
{
    public int TotalCount { get; set; }

    public int FilteredCount { get; set; }

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)FilteredCount / PageSize) : 0;

    public List<HotseatAuditRecordDto> Items { get; set; } = new();
}
