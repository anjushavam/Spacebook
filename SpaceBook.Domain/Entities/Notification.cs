namespace SpaceBook.Domain.Entities;

public class Notification
{
    // =========================================================
    // PRIMARY KEY
    // =========================================================

    public int NotificationId { get; set; }


    // =========================================================
    // EMPLOYEE
    // =========================================================

    public int? EmployeeId { get; set; }

    public Employee? Employee { get; set; }


    // =========================================================
    // NORMAL ROOM BOOKING
    // =========================================================

    public int? BookingId { get; set; }

    public Booking? Booking { get; set; }


    // =========================================================
    // HOTSEAT BOOKING
    // =========================================================

    public int? HotseatBookingId { get; set; }

    public HotseatBooking? HotseatBooking { get; set; }


    // =========================================================
    // NOTIFICATION DETAILS
    // =========================================================

    public string Message { get; set; } = string.Empty;

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }
}