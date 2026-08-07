INSERT INTO Notifications
(EmployeeId,  BookingId, Message)
VALUES
(1, 1, 'A new booking request has been submitted for approval.'),

(2, 1, 'Your booking request has been approved.'),

(3, 2, 'Your booking request has been rejected.');

SELECT * FROM Notifications;