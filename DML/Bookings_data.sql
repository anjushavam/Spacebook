INSERT INTO Bookings
(
    RoomNumber,
    Email,
    MeetingTitle,
    ParticipantCount,
    BookingDate,
    StartTime,
    EndTime,
    Status
)
VALUES

(101, 'diya.nair@company.com', 'Employee Training', 30, '2026-08-10', '09:00', '11:00', 'Booked'),

(102, 'rahul.verma@company.com', 'Quarterly Review', 15, '2026-08-10', '11:30', '12:30', 'Booked'),

(201, 'sneha.iyer@company.com', 'Project Discussion', 6, '2026-08-11', '10:00', '11:00', 'Rescheduled'),

(203, 'arjun.patel@company.com', 'Client Meeting', 8, '2026-08-11', '14:00', '15:00', 'Booked'),

(205, 'diya.nair@company.com', 'Sprint Planning', 7, '2026-08-12', '15:30', '16:30', 'Cancelled');

SELECT * FROM Bookings;