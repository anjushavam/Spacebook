INSERT INTO Rooms
(
    RoomNumber,
    RoomTypeId,
    RoomName,
    Capacity,
    Module,
    Status
)
VALUES
('CB-05-E01-001', 1, 'Training Room', 50, 'Module 1', 'Available'),

('CB-05-E01-002', 2, 'Conference Room', 20, 'Module 1', 'Available'),

('CB-05-E02-001', 3, 'Discussion Room 1', 8, 'Module 2', 'Available'),

('CB-05-E02-002', 3, 'Discussion Room 2', 8, 'Module 2', 'Available'),

('CB-05-E02-003', 3, 'Discussion Room 3', 8, 'Module 2', 'Available'),

('CB-05-E02-004', 3, 'Discussion Room 4', 8, 'Module 2', 'Available'),

('CB-05-E02-005', 3, 'Discussion Room 5', 8, 'Module 2', 'Available'),

('CB-05-E02-006', 3, 'Discussion Room 6', 8, 'Module 2', 'Available');

SELECT * FROM Rooms;