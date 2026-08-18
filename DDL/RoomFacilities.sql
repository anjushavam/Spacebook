CREATE TABLE RoomFacilities
(
    RoomNumber VARCHAR(100) NOT NULL,

    FacilityId INT NOT NULL,

    RecordIngestedBy VARCHAR(100),
    RecordIngestedOn TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    RecordModifiedBy VARCHAR(100),
    RecordModifiedOn TIMESTAMPTZ,

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

