CREATE TABLE Bookings
(
    BookingId SERIAL PRIMARY KEY,

    RoomNumber INT NOT NULL,

    Email VARCHAR(150) NOT NULL,

    MeetingTitle VARCHAR(150)
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

    BookedOn TIMESTAMP
        NOT NULL
        DEFAULT CURRENT_TIMESTAMP,

    Status VARCHAR(20)
        NOT NULL
        DEFAULT 'Booked'
        CHECK (Status IN ('Booked','Cancelled','Rescheduled')),

    CONSTRAINT CK_Booking_Time
        CHECK (StartTime < EndTime),

    CONSTRAINT FK_Booking_Room
        FOREIGN KEY (RoomNumber)
        REFERENCES Rooms(RoomNumber)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,

    CONSTRAINT FK_Booking_Employee
        FOREIGN KEY (Email)
        REFERENCES Employees(Email)
        ON UPDATE CASCADE
        ON DELETE RESTRICT
);

SET datestyle = 'SQL, MDY'; 



