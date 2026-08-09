CREATE TABLE Notifications
(
    NotificationId SERIAL PRIMARY KEY,

    EmployeeId INT NOT NULL,

    BookingId INT NOT NULL,

    Message VARCHAR(500)
        NOT NULL,

    IsRead BOOLEAN
        NOT NULL
        DEFAULT FALSE,

    CreatedAt TIMESTAMPTZ
        NOT NULL
        DEFAULT CURRENT_TIMESTAMP,

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