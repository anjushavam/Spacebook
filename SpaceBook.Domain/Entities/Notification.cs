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
    // Every notification belongs to an employee.

    public int EmployeeId { get; set; }

    public Employee? Employee { get; set; }


    // =========================================================
    // NORMAL ROOM BOOKING
    // =========================================================
    // Used for room-booking notifications.
    //
    // Example:
    // BookingId = 115
    // HotseatBookingId = null
    // =========================================================

    public int? BookingId { get; set; }

    public Booking? Booking { get; set; }


    // =========================================================
    // HOTSEAT BOOKING
    // =========================================================
    // Used for hotseat-booking notifications.
    //
    // Example:
    // BookingId = null
    // HotseatBookingId = 10
    // =========================================================

    public int? HotseatBookingId { get; set; }

    public HotseatBooking? HotseatBooking { get; set; }


    // =========================================================
    // NOTIFICATION MESSAGE
    // =========================================================

    public string Message { get; set; } = string.Empty;


    // =========================================================
    // READ STATUS
    // =========================================================

    public bool IsRead { get; set; } = false;


    // =========================================================
    // CREATED DATE
    // =========================================================
    // PostgreSQL column:
    // timestamp with time zone
    //
    // Always use UTC.
    // =========================================================

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}