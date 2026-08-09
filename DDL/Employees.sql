CREATE TABLE Employees
(
    EmployeeId SERIAL PRIMARY KEY,

    RoleId INT NOT NULL,

    Email VARCHAR(100)
        NOT NULL
        UNIQUE,

    PasswordHash VARCHAR(255)
        NOT NULL,

    IsActive BOOLEAN
        NOT NULL
        DEFAULT TRUE,

    CreatedOn TIMESTAMP
        NOT NULL
        DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT FK_Employees_Roles
        FOREIGN KEY (RoleId)
        REFERENCES Roles(RoleId)
);





