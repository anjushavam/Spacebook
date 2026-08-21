UPDATE public.roomfacilities
	SET roomfacilityid=?, roomid=?, facilityid=?
	WHERE <condition>;

DELETE FROM public.roomfacilities;

ALTER SEQUENCE public.roomfacilities_roomfacilityid_seq
RESTART WITH 1;

INSERT INTO public.roomfacilities
    (roomid, facilityid)
VALUES
    -- Module 2 - Training
    (1, 1),

    -- Module 1 - Conference Room
    (2, 3),

    -- Module 1 - Discussion Rooms
    (3, 3),
    (3, 2),
    (4, 3),
    (4, 2),

    -- Module 2 - Discussion Rooms
    (5, 3),
    (5, 2),
    (6, 3),
    (6, 2),
    (7, 3),
    (7, 2),
    (8, 3),
    (8, 2);

SELECT * FROM public.roomfacilities
ORDER BY roomfacilityid;