ALTER TABLE Rooms
ADD COLUMN ModuleId INT;

UPDATE Rooms r
SET ModuleId = m.ModuleId
FROM Modules m
WHERE r.Module = m.ModuleName;

ALTER TABLE Rooms
ALTER COLUMN ModuleId SET NOT NULL;

ALTER TABLE Rooms
ADD CONSTRAINT FK_Rooms_Modules
FOREIGN KEY (ModuleId)
REFERENCES Modules(ModuleId)
ON UPDATE CASCADE
ON DELETE RESTRICT;

ALTER TABLE Rooms
ADD CONSTRAINT UQ_Room_Module_Name
UNIQUE (ModuleId, RoomName);

ALTER TABLE Rooms
DROP COLUMN Module;

ALTER TABLE Notifications
ADD COLUMN HotseatBookingId INT;

ALTER TABLE Notifications
ALTER COLUMN BookingId DROP NOT NULL;

ALTER TABLE Notifications
ADD CONSTRAINT FK_Notifications_HotseatBookings
    FOREIGN KEY (HotseatBookingId)
    REFERENCES HotseatBookings(HotseatBookingId)
    ON UPDATE CASCADE
    ON DELETE RESTRICT;

ALTER TABLE Notifications
ADD CONSTRAINT CHK_Notification_Booking
    CHECK
    (
        BookingId IS NOT NULL
        OR HotseatBookingId IS NOT NULL
    );

UPDATE Notifications
SET
    EmployeeId = 105523,
    BookingId = 1,
    HotseatBookingId = NULL,
    Message = 'Your booking request for Project Review has been approved.',
    IsRead = FALSE,
    RecordIngestedBy = '105508'
WHERE NotificationId = 1;

UPDATE Notifications
SET
    EmployeeId = 105508,
    BookingId = 3,
    HotseatBookingId = NULL,
    Message = 'A new booking request from Amirtha for Sprint Planning requires your approval.',
    IsRead = FALSE,
    RecordIngestedBy = '105489'
WHERE NotificationId = 2;

UPDATE Notifications
SET
    EmployeeId = 105514,
    BookingId = 2,
    HotseatBookingId = NULL,
    Message = 'Your booking for Team Discussion has been approved.',
    IsRead = TRUE,
    RecordIngestedBy = '105508'
WHERE NotificationId = 3;

UPDATE Notifications
SET
    EmployeeId = 105528,
    BookingId = 4,
    HotseatBookingId = NULL,
    Message = 'Your booking request for Project Discussion has been rejected.',
    IsRead = TRUE,
    RecordIngestedBy = '105508'
WHERE NotificationId = 4;

UPDATE Notifications
SET
    EmployeeId = 105533,
    BookingId = 5,
    HotseatBookingId = NULL,
    Message = 'Your booking for Team Meeting has been approved.',
    IsRead = FALSE,
    RecordIngestedBy = '105508'
WHERE NotificationId = 5;

UPDATE Notifications
SET
    EmployeeId = 105517,
    BookingId = 6,
    HotseatBookingId = NULL,
    Message = 'Your booking for Requirement Discussion has been cancelled.',
    IsRead = TRUE,
    RecordIngestedBy = '105517'
WHERE NotificationId = 6;

UPDATE Notifications
SET
    EmployeeId = 103659,
    BookingId = 7,
    HotseatBookingId = NULL,
    Message = 'Your booking request for Project Planning is pending approval.',
    IsRead = FALSE,
    RecordIngestedBy = '103659'
WHERE NotificationId = 7;

UPDATE Notifications
SET
    EmployeeId = 105508,
    BookingId = 7,
    HotseatBookingId = NULL,
    Message = 'A new booking request from Anitha for Project Planning requires your approval.',
    IsRead = FALSE,
    RecordIngestedBy = '103659'
WHERE NotificationId = 8;


DELETE FROM Notifications
WHERE NotificationId BETWEEN 9 AND 11;

INSERT INTO Notifications
(
    NotificationId,
    EmployeeId,
    BookingId,
    HotseatBookingId,
    Message,
    IsRead,
    RecordIngestedBy
)
VALUES
(
    9,
    105523,
    NULL,
    1,
    'Your hotseat booking for August 18, 2026 has been confirmed. Seat 1 is assigned to you.',
    FALSE,
    '105508'
),
(
    10,
    105554,
    NULL,
    2,
    'You have successfully checked in to hotseat 2.',
    TRUE,
    '105508'
),
(
    11,
    105514,
    NULL,
    3,
    'Your hotseat booking for August 18, 2026 is confirmed. Seat 3 is assigned to you.',
    FALSE,
    '105508'
),
(
    12,
    105489,
    NULL,
    4,
    'Your hotseat booking for August 18, 2026 has been released.',
    TRUE,
    '105508'
),
(
    13,
    105528,
    NULL,
    5,
    'Your hotseat booking for August 18, 2026 has been cancelled.',
    TRUE,
    '105528'
),
(
    14,
    105533,
    NULL,
    6,
    'Your hotseat booking has expired because you did not check in within the required time.',
    TRUE,
    '105508'
),
(
    15,
    105517,
    NULL,
    7,
    'Your hotseat booking for August 18, 2026 is confirmed. Seat 33 is assigned to you.',
    FALSE,
    '105508'
),
(
    16,
    103659,
    NULL,
    8,
    'Your hotseat has been released after one hour from your check-in time.',
    TRUE,
    '105508'
);

SELECT * FROM Rooms;
SELECT * FROM Notifications;


