CREATE TABLE HotseatBookings
(
    HotseatBookingId SERIAL PRIMARY KEY,
    SeatId INT NOT NULL,
    EmployeeId INT NOT NULL,
    BookingDate DATE NOT NULL,
    BookingStatus VARCHAR(30)
        DEFAULT 'Confirmed',
    BookedOn TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    CheckInDeadline TIMESTAMPTZ,
    CheckInTime TIMESTAMPTZ,
    ReleasedOn TIMESTAMPTZ,

    CONSTRAINT FK_HotseatBookings_Seats
        FOREIGN KEY (SeatId)
        REFERENCES Seats(SeatId)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,

    CONSTRAINT FK_HotseatBookings_Employees
        FOREIGN KEY (EmployeeId)
        REFERENCES Employees(EmployeeId)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,

    CONSTRAINT UQ_Seat_Booking_Date
        UNIQUE (SeatId, BookingDate),

    CONSTRAINT CHK_HotseatBooking_Status
        CHECK
        (
            BookingStatus IN
            ('Confirmed', 'Cancelled', 'CheckedIn', 'Released', 'Expired')
        ),

    RecordIngestedBy VARCHAR(100),
    RecordIngestedOn TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    RecordModifiedBy VARCHAR(100),
    RecordModifiedOn TIMESTAMPTZ
);