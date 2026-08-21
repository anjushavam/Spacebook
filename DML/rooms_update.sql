UPDATE public.rooms
	SET roomid=?, roomtypeid=?, roomname=?, capacity=?, status=?, isblocked=?, moduleid=?
	WHERE <condition>;

TRUNCATE TABLE rooms RESTART IDENTITY CASCADE;

INSERT INTO public.rooms
    (roomid, roomtypeid, roomname, capacity, status, isblocked, moduleid)
VALUES
    (1, 1, 'Training',          50, 'Available', false, 2),
    (2, 2, 'Conference Room',  20, 'Available', false, 1),
    (3, 3, 'Discussion Room 1', 8, 'Available', false, 1),
    (4, 3, 'Discussion Room 2', 8, 'Available', false, 1),
    (5, 3, 'Discussion Room 1', 10, 'Available', false, 2),
    (6, 3, 'Discussion Room 2', 8, 'Available', false, 2),
    (7, 3, 'Discussion Room 3', 8, 'Available', false, 2),
    (8, 3, 'Discussion Room 4', 8, 'Available', false, 2);

SELECT setval(
    'public.rooms_roomid_seq',
    (SELECT MAX(roomid) FROM public.rooms)
);

UPDATE public.rooms
SET roomname = 'Training Room'
WHERE roomid = 1;

ALTER TABLE public.rooms
ADD COLUMN roomnumber VARCHAR(100);

UPDATE public.rooms
SET
    roomnumber = CASE roomid
        WHEN 1 THEN 'CBE-05-EO2-012'
        WHEN 2 THEN 'CBE-05-EO1-001'
        WHEN 3 THEN 'CBE-05-EO1-003'
        WHEN 4 THEN 'CBE-05-EO1-005'
        WHEN 5 THEN 'CBE-05-EO2-001'
        WHEN 6 THEN 'CBE-05-EO2-002'
        WHEN 7 THEN 'CBE-05-EO2-007'
        WHEN 8 THEN 'CBE-05-EO2-010'
    END,
    roomname = CASE roomid
        WHEN 1 THEN 'Training Room'
        WHEN 2 THEN 'Conference Room'
        WHEN 3 THEN 'Discussion Room 1'
        WHEN 4 THEN 'Discussion Room 2'
        WHEN 5 THEN 'Discussion Room 1'
        WHEN 6 THEN 'Discussion Room 2'
        WHEN 7 THEN 'Discussion Room 3'
        WHEN 8 THEN 'Discussion Room 4'
    END
WHERE roomid IN (1,2,3,4,5,6,7,8);

SELECT *
FROM public.rooms
ORDER BY roomid;





