CREATE TABLE Offices
(
    OfficeId SERIAL PRIMARY KEY,
    LocationId INT NOT NULL,
    OfficeName VARCHAR(100) NOT NULL,

    CONSTRAINT FK_Offices_Locations
        FOREIGN KEY (LocationId)
        REFERENCES Locations(LocationId),

    CONSTRAINT UQ_Office_Location
        UNIQUE (LocationId, OfficeName),

    RecordIngestedBy VARCHAR(100),
    RecordIngestedOn TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    RecordModifiedBy VARCHAR(100),
    RecordModifiedOn TIMESTAMPTZ
);