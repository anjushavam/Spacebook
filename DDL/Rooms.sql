CREATE TABLE Rooms
(
    RoomNumber INT PRIMARY KEY,

    RoomTypeId INT NOT NULL,

    RoomName VARCHAR(100) NOT NULL UNIQUE,

    Capacity INT NOT NULL
        CHECK (Capacity > 0),

    Module VARCHAR(20) NOT NULL,

    Status VARCHAR(20) NOT NULL
        DEFAULT 'Available'
        CHECK (Status IN ('Available','Unavailable','Maintenance')),

    CONSTRAINT FK_Room_RoomType
        FOREIGN KEY (RoomTypeId)
        REFERENCES RoomTypes(RoomTypeId)
        ON UPDATE CASCADE
        ON DELETE RESTRICT
);





