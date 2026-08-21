UPDATE public.notifications
	SET notificationid=?, employeeid=?, bookingid=?, message=?, isread=?, createdat=?, hotseatbookingid=?
	WHERE <condition>;

BEGIN;

DELETE FROM public.notifications;

ALTER SEQUENCE public.notifications_notificationid_seq
RESTART WITH 1;

COMMIT;

SELECT *
FROM public.notifications
ORDER BY notificationid;

SELECT nextval('public.notifications_notificationid_seq');

ALTER SEQUENCE public.notifications_notificationid_seq
RESTART WITH 1;