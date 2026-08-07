INSERT INTO Employees
(
    RoleId,
    Email,
    PasswordHash,
    IsActive
)
VALUES
(1, 'admin@company.com', 'hashed_admin_password', TRUE),

(2, 'diya.nair@company.com', 'hashed_password_1', TRUE),

(2, 'rahul.verma@company.com', 'hashed_password_2', TRUE),

(2, 'sneha.iyer@company.com', 'hashed_password_3', TRUE),

(2, 'arjun.patel@company.com', 'hashed_password_4', TRUE);

SELECT * FROM Employees;

