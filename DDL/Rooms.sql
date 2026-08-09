CREATE TABLE Rooms
(
    RoomNumber VARCHAR(100) PRIMARY KEY,

    RoomTypeId INT NOT NULL,

    RoomName VARCHAR(100)
        NOT NULL
        UNIQUE,

    Capacity INT
        NOT NULL
        CHECK (Capacity > 0),

    Module VARCHAR(100)
        NOT NULL,

    Status VARCHAR(100)
        NOT NULL
        DEFAULT 'Available'
        CHECK (Status IN ('Available', 'Booked', 'Maintenance')),

    CONSTRAINT FK_Rooms_RoomTypes
        FOREIGN KEY (RoomTypeId)
        REFERENCES RoomTypes(RoomTypeId)
        ON UPDATE CASCADE
        ON DELETE RESTRICT
);