CREATE TABLE Bookings
(
    BookingId SERIAL PRIMARY KEY,

    RoomNumber VARCHAR(100) NOT NULL,

    EmployeeId INT NOT NULL,

    MeetingTitle VARCHAR(200)
        NOT NULL,

    ParticipantCount INT
        NOT NULL
        CHECK (ParticipantCount > 0),

    BookingDate DATE
        NOT NULL,

    StartTime TIME
        NOT NULL,

    EndTime TIME
        NOT NULL,

    BookedOn TIMESTAMPTZ
        NOT NULL
        DEFAULT CURRENT_TIMESTAMP,

    Status VARCHAR(20)
        NOT NULL
        DEFAULT 'Pending'
        CHECK (Status IN ('Pending', 'Approved', 'Rejected')),

    CONSTRAINT FK_Bookings_Rooms
        FOREIGN KEY (RoomNumber)
        REFERENCES Rooms(RoomNumber)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,

    CONSTRAINT FK_Bookings_Employees
        FOREIGN KEY (EmployeeId)
        REFERENCES Employees(EmployeeId)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,

    CONSTRAINT CHK_Booking_Time
        CHECK (EndTime > StartTime)
);

SET TIME ZONE 'Asia/Kolkata';
SET datestyle = 'SQL, MDY'; 