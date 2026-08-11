CREATE TABLE Employees
(
    EmployeeId INT PRIMARY KEY,

    EmployeeName VARCHAR(100) NOT NULL,

    RoleId INT NOT NULL,

    Email VARCHAR(150) NOT NULL UNIQUE,

    PasswordHash VARCHAR(255) NOT NULL,

    IsActive BOOLEAN DEFAULT TRUE,

    RecordIngestedBy VARCHAR(100),
    RecordIngestedOn TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    RecordModifiedBy VARCHAR(100),
    RecordModifiedOn TIMESTAMPTZ,

    CONSTRAINT FK_Employees_Roles
        FOREIGN KEY (RoleId)
        REFERENCES Roles(RoleId)
        ON UPDATE CASCADE
        ON DELETE RESTRICT
);

SET TIME ZONE 'Asia/Kolkata';
SET datestyle = 'SQL, MDY';