INSERT INTO Bookings
(
    RoomNumber,
    EmployeeId,
    MeetingTitle,
    ParticipantCount,
    BookingDate,
    StartTime,
    EndTime,
    Status
)
VALUES

(101, '105524', 'Employee Training', 30, '2026-08-10', '09:00', '11:00', 'Booked'),

(102, '105525', 'Quarterly Review', 15, '2026-08-10', '11:30', '12:30', 'Booked'),

(201, '105526', 'Project Discussion', 6, '2026-08-11', '10:00', '11:00', 'Rescheduled'),

(203, '105527', 'Client Meeting', 8, '2026-08-11', '02:00', '03:00', 'Booked'),

(205, '105528', 'Sprint Planning', 7, '2026-08-12', '03:30', '04:30', 'Cancelled');

SELECT * FROM Bookings;


