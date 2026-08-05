INSERT INTO Employees
(
    EmployeeId,
    RoleId,
    Name,
    Email,
    PasswordHash,
    Department,
    IsActive
)
VALUES
('105524', 1, 'Aarav Sharma', 'aarav.sharma@company.com', 'Admin@123', 'Administration', TRUE),

('105525', 2, 'Diya Nair', 'diya.nair@company.com', 'Employee@123', 'Human Resources', TRUE),

('105526', 2, 'Rahul Verma', 'rahul.verma@company.com', 'Employee@123', 'Information Technology', TRUE),

('105527', 2, 'Sneha Iyer', 'sneha.iyer@company.com', 'Employee@123', 'Finance', TRUE),

('105528', 2, 'Arjun Patel', 'arjun.patel@company.com', 'Employee@123', 'Operations', TRUE);

SELECT * FROM Employees;