INSERT INTO Employees
(
    Email,
    RoleId,
    PasswordHash,
    IsActive
)
VALUES
('aarav.sharma@company.com', 1, 'Admin@123', TRUE),

('diya.nair@company.com', 2, 'Diya@123', TRUE),

('rahul.verma@company.com', 2, 'Rahul@123', TRUE),

('sneha.iyer@company.com', 2, 'Sneha@123', TRUE),

('arjun.patel@company.com', 2, 'Arjun@123', TRUE);

SELECT * FROM Employees;