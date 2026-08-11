INSERT INTO Notifications
(
    NotificationId,
    EmployeeId,
    BookingId,
    Message,
    IsRead,
    RecordIngestedBy
)
VALUES
(
    1,
    105523,
    1,
    'Your booking request for Employee Training is pending approval.',
    FALSE,
    '105523'
),
(
    2,
    105508,
    1,
    'A new booking request from Shofia for Employee Training requires your approval.',
    FALSE,
    '105523'
),
(
    3,
    105554,
    2,
    'Your booking for Project Review has been approved.',
    TRUE,
    '105508'
),
(
    4,
    105514,
    3,
    'Your booking for Team Discussion has been approved.',
    FALSE,
    '105508'
),
(
    5,
    105489,
    4,
    'Your booking request for Sprint Planning is pending approval.',
    FALSE,
    '105489'
),
(
    6,
    105508,
    4,
    'A new booking request from Amirtha for Sprint Planning requires your approval.',
    FALSE,
    '105489'
),
(
    7,
    105528,
    5,
    'Your booking for Project Discussion has been rejected.',
    TRUE,
    '105508'
),
(
    8,
    105533,
    6,
    'Your booking for Team Meeting has been approved.',
    FALSE,
    '105508'
),
(
    9,
    105517,
    7,
    'Your booking for Requirement Discussion has been cancelled.',
    TRUE,
    '105517'
),
(
    10,
    103659,
    8,
    'Your booking request for Project Planning is pending approval.',
    FALSE,
    '103659'
),
(
    11,
    105508,
    8,
    'A new booking request from Anitha for Project Planning requires your approval.',
    FALSE,
    '103659'
);

SELECT * FROM Notifications;