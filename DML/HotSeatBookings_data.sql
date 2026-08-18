INSERT INTO HotseatBookings
(
    HotseatBookingId,
    SeatId,
    EmployeeId,
    BookingDate,
    BookingStatus,
    BookedOn,
    CheckInDeadline,
    CheckInTime,
    ReleasedOn,
    RecordIngestedBy
)
VALUES

-- Confirmed: employee has booked but has not checked in yet
(
    1,
    1,
    105523,
    '2026-08-18',
    'Confirmed',
    '2026-08-17 19:35:00+05:30',
    '2026-08-18 09:30:00+05:30',
    NULL,
    NULL,
    '105508'
),

-- Checked in at 09:00, seat will be released at 10:00
(
    2,
    2,
    105554,
    '2026-08-18',
    'CheckedIn',
    '2026-08-17 19:40:00+05:30',
    '2026-08-18 09:30:00+05:30',
    '2026-08-18 09:00:00+05:30',
    NULL,
    '105508'
),

-- Confirmed
(
    3,
    3,
    105514,
    '2026-08-18',
    'Confirmed',
    '2026-08-17 19:45:00+05:30',
    '2026-08-18 09:30:00+05:30',
    NULL,
    NULL,
    '105508'
),

-- Released after one hour of check-in
(
    4,
    7,
    105489,
    '2026-08-18',
    'Released',
    '2026-08-17 20:00:00+05:30',
    '2026-08-18 09:30:00+05:30',
    '2026-08-18 09:15:00+05:30',
    '2026-08-18 10:15:00+05:30',
    '105508'
),

-- Cancelled before use
(
    5,
    8,
    105528,
    '2026-08-18',
    'Cancelled',
    '2026-08-17 20:10:00+05:30',
    '2026-08-18 09:30:00+05:30',
    NULL,
    NULL,
    '105508'
),

-- Expired because employee did not check in
(
    6,
    9,
    105533,
    '2026-08-18',
    'Expired',
    '2026-08-17 20:20:00+05:30',
    '2026-08-18 09:30:00+05:30',
    NULL,
    '2026-08-18 09:30:00+05:30',
    '105508'
),

-- Checked in and currently using the seat
(
    7,
    21,
    105517,
    '2026-08-18',
    'CheckedIn',
    '2026-08-17 20:30:00+05:30',
    '2026-08-18 09:30:00+05:30',
    '2026-08-18 10:00:00+05:30',
    NULL,
    '105508'
),

-- Checked in and already released after one hour
(
    8,
    22,
    103659,
    '2026-08-18',
    'Released',
    '2026-08-17 21:00:00+05:30',
    '2026-08-18 09:30:00+05:30',
    '2026-08-18 10:30:00+05:30',
    '2026-08-18 11:30:00+05:30',
    '105508'
);

SELECT * 
FROM HotseatBookings
ORDER BY HotseatBookingId;
