UPDATE public.hotseatbookings
	SET hotseatbookingid=?, seatid=?, employeeid=?, bookingdate=?, bookingstatus=?, bookedon=?, checkindeadline=?, checkintime=?, releasedon=?, recordingestedby=?, recordingestedon=?, recordmodifiedby=?, recordmodifiedon=?
	WHERE <condition>;

BEGIN;

DELETE FROM public.hotseatbookings;

ALTER SEQUENCE public.hotseatbookings_hotseatbookingid_seq
RESTART WITH 1;

COMMIT;


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

UPDATE public.hotseatbookings
SET
    seatid = 8,
    employeeid = 105528,
    bookingdate = '2026-08-18',
    bookingstatus = 'Cancelled',
    bookedon = '2026-08-17 20:10:00+05:30',
    checkindeadline = '2026-08-18 09:30:00+05:30',
    checkintime = NULL,
    releasedon = NULL,
    recordingestedby = '105508'
WHERE hotseatbookingid = 4;


UPDATE public.hotseatbookings
SET
    seatid = 9,
    employeeid = 105533,
    bookingdate = '2026-08-18',
    bookingstatus = 'Expired',
    bookedon = '2026-08-17 20:20:00+05:30',
    checkindeadline = '2026-08-18 09:30:00+05:30',
    checkintime = NULL,
    releasedon = '2026-08-18 09:30:00+05:30',
    recordingestedby = '105508'
WHERE hotseatbookingid = 5;


UPDATE public.hotseatbookings
SET
    seatid = 21,
    employeeid = 105517,
    bookingdate = '2026-08-18',
    bookingstatus = 'CheckedIn',
    bookedon = '2026-08-17 20:30:00+05:30',
    checkindeadline = '2026-08-18 09:30:00+05:30',
    checkintime = '2026-08-18 10:00:00+05:30',
    releasedon = NULL,
    recordingestedby = '105508'
WHERE hotseatbookingid = 6;

SELECT * 
FROM HotseatBookings
ORDER BY HotseatBookingId;



