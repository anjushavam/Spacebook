UPDATE public.bookings
	SET bookingid=?, roomid=?, employeeid=?, purpose=?, participantcount=?, bookingdate=?, starttime=?, endtime=?, bookedon=?, status=?, meetingtitle=?
	WHERE <condition>;

ALTER TABLE bookings
ADD COLUMN IF NOT EXISTS cancellationreason TEXT;

BEGIN;

DELETE FROM public.bookings;

ALTER SEQUENCE public.bookings_bookingid_seq
RESTART WITH 1;

COMMIT;

SELECT nextval('public.bookings_bookingid_seq');

ALTER SEQUENCE public.bookings_bookingid_seq
RESTART WITH 1;

INSERT INTO public.bookings
    (roomid, employeeid, purpose, participantcount, bookingdate,
     starttime, endtime, status, meetingtitle, cancellationreason)
VALUES
    (1, 105523, 'Training Session', 25, '2026-08-24',
     '10:00:00', '12:00:00', 'Approved', 'Project Training', NULL),

    (2, 105489, 'Client Meeting', 12, '2026-08-24',
     '11:00:00', '12:30:00', 'Approved', 'Client Discussion', NULL),

    (3, 105508, 'Team Discussion', 6, '2026-08-24',
     '09:30:00', '10:30:00', 'Approved', 'Sprint Planning', NULL),

    (4, 105514, 'Team Meeting', 7, '2026-08-24',
     '14:00:00', '15:00:00', 'Pending', 'Team Sync-up', NULL),

    (5, 105517, 'Project Discussion', 8, '2026-08-25',
     '10:00:00', '11:30:00', 'Approved', 'Project Discussion', NULL),

    (6, 105528, 'Team Discussion', 5, '2026-08-25',
     '11:00:00', '12:00:00', 'Pending', 'Requirement Discussion', NULL),

    (7, 105533, 'Knowledge Sharing', 8, '2026-08-25',
     '14:00:00', '15:30:00', 'Approved', 'Knowledge Sharing Session', NULL),

    (8, 105554, 'Team Meeting', 6, '2026-08-26',
     '10:30:00', '11:30:00', 'Cancelled', 'Weekly Team Meeting',
     'Meeting rescheduled');

ALTER TABLE public.bookings
ADD COLUMN cancelledby INTEGER;

ALTER TABLE public.bookings
ADD CONSTRAINT fk_bookings_cancelledby
FOREIGN KEY (cancelledby)
REFERENCES public.employees(employeeid)
ON UPDATE CASCADE
ON DELETE RESTRICT;

UPDATE public.bookings
SET
    status = 'Rejected',
    cancellationreason = 'Room is under maintenance.',
    cancelledby = 105508
WHERE bookingid = 4;

UPDATE public.bookings
SET
    status = 'Cancelled',
    cancellationreason = 'Meeting has been rescheduled.',
    cancelledby = 105554
WHERE bookingid = 8;

UPDATE public.bookings
SET
    cancellationreason = 'Employee cancelled the booking because the room is not required right now.',
    cancelledby = employeeid
WHERE status = 'Cancelled'
  AND bookingid = 1;

UPDATE public.bookings
SET
    cancellationreason = 'Employee cancelled the meeting because the meeting has been rescheduled.',
    cancelledby = employeeid
WHERE status = 'Cancelled'
  AND bookingid = 8;

UPDATE public.bookings
SET
    cancellationreason = 'Employee cancelled the meeting because the meeting was rescheduled.',
    cancelledby = employeeid
WHERE status = 'Cancelled'
  AND bookingid = 9;

UPDATE public.bookings
SET
    status = 'Rejected',
    cancellationreason = 'Room is required for higher priority meeting.',
    cancelledby = 105508
WHERE bookingid = 4;

UPDATE public.bookings
SET
    employeeid = 105514,
    status = 'Rejected',
    cancellationreason = 'Room is under maintenance.',
    cancelledby = 105508
WHERE bookingid = 4;

UPDATE public.bookings
SET cancellationreason = 'Room is not required right now.'
WHERE bookingid = 1;

UPDATE public.bookings
SET cancellationreason = 'Meeting has been rescheduled.'
WHERE bookingid = 8;

UPDATE public.bookings
SET cancellationreason = 'Meeting was rescheduled.'
WHERE bookingid = 9;

UPDATE public.bookings
SET cancellationreason = 'Room is under maintenance.'
WHERE bookingid = 4;

SELECT *
FROM public.bookings
ORDER BY bookingid;




