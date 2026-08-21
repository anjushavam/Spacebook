INSERT INTO public.notifications(
	notificationid, employeeid, bookingid, message, isread, createdat, hotseatbookingid)
	VALUES (?, ?, ?, ?, ?, ?, ?);