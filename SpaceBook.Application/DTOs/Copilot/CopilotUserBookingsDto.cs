namespace SpaceBook.Application.DTOs.Copilot;

public class CopilotUserBookingsDto
{
    public int EmployeeId { get; set; }

    public string EmployeeName { get; set; } = string.Empty;

    public string EmployeeEmail { get; set; } = string.Empty;

    public List<CopilotUserRoomBookingDto> RoomBookings { get; set; } = new();

    public List<CopilotUserHotseatBookingDto> HotseatBookings { get; set; } = new();
}

public class CopilotUserRoomBookingDto
{
    public int BookingId { get; set; }

    public int RoomId { get; set; }

    public string RoomName { get; set; } = string.Empty;

    public string ModuleName { get; set; } = string.Empty;

    public string OfficeName { get; set; } = string.Empty;

    public string LocationName { get; set; } = string.Empty;

    public string MeetingTitle { get; set; } = string.Empty;

    public DateOnly BookingDate { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public string Status { get; set; } = string.Empty;
}

public class CopilotUserHotseatBookingDto
{
    public int HotseatBookingId { get; set; }

    public int SeatId { get; set; }

    public string SeatNumber { get; set; } = string.Empty;

    public string Section { get; set; } = string.Empty;

    public string ModuleName { get; set; } = string.Empty;

    public string OfficeName { get; set; } = string.Empty;

    public string LocationName { get; set; } = string.Empty;

    public DateOnly BookingDate { get; set; }

    public string? ExpectedCheckInTime { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime? CheckInTime { get; set; }
}
