INSERT INTO Rooms
(
    RoomNumber,
    RoomTypeId,
    RoomName,
    Capacity,
    Module,
    Status,
    RecordIngestedBy
)
VALUES
('CB-05-E02-012', 3, 'Training Room ', 50, 'Module 2 - Elcot Park - CMB', 'Available', '105508'),

('CB-05-E01-001', 1, 'Conference Room ', 20, 'Module 1 - Elcot Park - CMB', 'Available', '105508'),

('CB-05-E01-003', 2, 'Discussion Room 1', 8, 'Module 1 - Elcot Park - CMB', 'Available', '105508'),

('CB-05-E01-005', 2, 'Discussion Room 2', 8, 'Module 1 - Elcot Park - CMB', 'Available', '105508'),

('CB-05-E02-001', 2, 'Discussion Room 3', 8, 'Module 2 - Elcot Park - CMB', 'Available', '105508'),

('CB-05-E02-002', 2, 'Discussion Room 4', 8, 'Module 2 - Elcot Park - CMB', 'Available', '105508'),

('CB-05-E02-007', 2, 'Discussion Room 5', 8, 'Module 2 - Elcot Park - CMB', 'Available', '105508'),

('CB-05-E02-010', 2, 'Discussion Room 6', 8, 'Module 2 - Elcot Park - CMB', 'Available', '105508');

SELECT * FROM Rooms;

