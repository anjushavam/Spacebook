INSERT INTO Bookings
(
    RoomNumber,
    EmployeeId,
    MeetingTitle,
    ParticipantCount,
    BookingDate,
    StartTime,
    EndTime
)
VALUES

('CB-05-E01-001', 2, 'Employee Training', 30, '2026-08-10', '09:00', '11:00'),

('CB-05-E01-002', 2, 'Quarterly Review', 15, '2026-08-10', '11:30', '12:30'),

('CB-05-E02-001', 2, 'Project Discussion', 6, '2026-08-11', '10:00', '11:00'),

('CB-05-E02-003', 2, 'Client Meeting', 8, '2026-08-11', '14:00', '15:00'),

('CB-05-E02-005', 2, 'Sprint Planning', 7, '2026-08-12', '15:30', '16:30');

SELECT * FROM Bookings;