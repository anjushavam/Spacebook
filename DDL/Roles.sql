CREATE TABLE Roles
(
    RoleId SERIAL PRIMARY KEY,

    RoleName VARCHAR(50)
        NOT NULL
        UNIQUE
);



