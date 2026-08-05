CREATE TABLE RoomFacilities
(
    RoomNumber INT NOT NULL,

    FacilityId INT NOT NULL,

    CONSTRAINT PK_RoomFacilities
        PRIMARY KEY(RoomNumber, FacilityId),

    CONSTRAINT FK_RoomFacilities_Room
        FOREIGN KEY(RoomNumber)
        REFERENCES Rooms(RoomNumber)
        ON UPDATE CASCADE
        ON DELETE CASCADE,

    CONSTRAINT FK_RoomFacilities_Facility
        FOREIGN KEY(FacilityId)
        REFERENCES Facilities(FacilityId)
        ON UPDATE CASCADE
        ON DELETE CASCADE
);

