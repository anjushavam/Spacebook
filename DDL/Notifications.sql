CREATE TABLE Notifications
(
    NotificationId SERIAL PRIMARY KEY,

    EmployeeId INT NOT NULL,

    EmployeeName VARCHAR(100) NOT NULL,

    BookingId INT NOT NULL,

    Message VARCHAR(500) NOT NULL,

    IsRead BOOLEAN DEFAULT FALSE,

    CreatedAt TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,

    RecordIngestedBy VARCHAR(100),

    RecordIngestedOn TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,

    RecordModifiedBy VARCHAR(100),

    RecordModifiedOn TIMESTAMPTZ,

    CONSTRAINT FK_Notifications_Employees
        FOREIGN KEY (EmployeeId)
        REFERENCES Employees(EmployeeId)
        ON UPDATE CASCADE
        ON DELETE CASCADE,

    CONSTRAINT FK_Notifications_Bookings
        FOREIGN KEY (BookingId)
        REFERENCES Bookings(BookingId)
        ON UPDATE CASCADE
        ON DELETE CASCADE
);

SET TIME ZONE 'Asia/Kolkata';
SET datestyle = 'SQL, MDY';