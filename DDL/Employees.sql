CREATE TABLE Employees
(
    Email VARCHAR(150) PRIMARY KEY,

    RoleId INT NOT NULL,

    PasswordHash TEXT
        NOT NULL,

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

SET datestyle = 'SQL, MDY'; 

