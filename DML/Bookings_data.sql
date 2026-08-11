INSERT INTO Bookings
(
    BookingId,
    RoomNumber,
    EmployeeId,
    MeetingTitle,
    ParticipantCount,
    BookingDate,
    StartTime,
    EndTime,
    Status,
    RecordIngestedBy
)
VALUES
(
    1,
    'CB-05-E01-001',
    105523,
    'Employee Training',
    25,
    '2026-08-12',
    '09:00',
    '11:00',
    'Pending',
    '105523'
),

(
    2,
    'CB-05-E01-002',
    105554,
    'Project Review',
    10,
    '2026-08-12',
    '11:30',
    '12:30',
    'Approved',
    '105554'
),

(
    3,
    'CB-05-E02-001',
    105514,
    'Team Discussion',
    6,
    '2026-08-12',
    '10:00',
    '11:00',
    'Approved',
    '105514'
),

(
    4,
    'CB-05-E02-002',
    105489,
    'Sprint Planning',
    7,
    '2026-08-12',
    '14:00',
    '15:00',
    'Pending',
    '105489'
),

(
    5,
    'CB-05-E02-003',
    105528,
    'Project Discussion',
    5,
    '2026-08-13',
    '10:00',
    '11:00',
    'Rejected',
    '105528'
),

(
    6,
    'CB-05-E02-004',
    105533,
    'Team Meeting',
    8,
    '2026-08-13',
    '11:30',
    '12:30',
    'Approved',
    '105533'
),

(
    7,
    'CB-05-E02-005',
    105517,
    'Requirement Discussion',
    6,
    '2026-08-13',
    '14:00',
    '15:00',
    'Cancelled',
    '105517'
),

(
    8,
    'CB-05-E02-006',
    103659,
    'Project Planning',
    7,
    '2026-08-14',
    '15:00',
    '16:00',
    'Pending',
    '103659'
);

SELECT * FROM Bookings;
