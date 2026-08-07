CREATE TABLE RoomFacilities
(
    RoomNumber VARCHAR(100) NOT NULL,

    FacilityId INT NOT NULL,

    CONSTRAINT PK_RoomFacilities
        PRIMARY KEY (RoomNumber, FacilityId),

    CONSTRAINT FK_RoomFacilities_Rooms
        FOREIGN KEY (RoomNumber)
        REFERENCES Rooms(RoomNumber)
        ON UPDATE CASCADE
        ON DELETE CASCADE,

    CONSTRAINT FK_RoomFacilities_Facilities
        FOREIGN KEY (FacilityId)
        REFERENCES Facilities(FacilityId)
        ON UPDATE CASCADE
        ON DELETE CASCADE
);

