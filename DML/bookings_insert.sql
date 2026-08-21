INSERT INTO public.bookings(
	bookingid, roomid, employeeid, purpose, participantcount, bookingdate, starttime, endtime, bookedon, status, meetingtitle)
	VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?);