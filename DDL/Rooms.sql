CREATE TABLE Rooms
(
    RoomNumber VARCHAR(100) PRIMARY KEY,

    RoomTypeId INT NOT NULL,

    RoomName VARCHAR(100) UNIQUE,

    Capacity INT CHECK (Capacity > 0),

    Module VARCHAR(100),

    Status VARCHAR(20)
        DEFAULT 'Available'
        CHECK (Status IN ('Available', 'Booked', 'Maintenance')),

    RecordIngestedBy VARCHAR(100),
    RecordIngestedOn TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    RecordModifiedBy VARCHAR(100),
    RecordModifiedOn TIMESTAMPTZ,

    CONSTRAINT FK_Rooms_RoomTypes
        FOREIGN KEY (RoomTypeId)
        REFERENCES RoomTypes(RoomTypeId)
        ON UPDATE CASCADE
        ON DELETE RESTRICT
);

SET TIME ZONE 'Asia/Kolkata';
SET datestyle = 'SQL, MDY';