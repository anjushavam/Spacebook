public class CreateHotseatBookingDto
{
    public int SeatId { get; set; }
 
    public DateOnly BookingDate { get; set; }
 
    public TimeOnly? ExpectedCheckInTime { get; set; }
}