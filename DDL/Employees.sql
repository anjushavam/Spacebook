CREATE TABLE Employees
(
    EmployeeId VARCHAR(20) PRIMARY KEY,

    RoleId INT NOT NULL,

    Name VARCHAR(100)
        NOT NULL,

    Email VARCHAR(150)
        NOT NULL
        UNIQUE,

    PasswordHash TEXT
        NOT NULL,

    Department VARCHAR(100),

    IsActive BOOLEAN
        NOT NULL
        DEFAULT TRUE,

    CreatedOn TIMESTAMP
        NOT NULL
        DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT FK_Employee_Role
        FOREIGN KEY (RoleId)
        REFERENCES Roles(RoleId)
        ON UPDATE CASCADE
        ON DELETE RESTRICT
);

