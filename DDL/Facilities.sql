CREATE TABLE Facilities
(
    FacilityId SERIAL PRIMARY KEY,

    FacilityName VARCHAR(100)
        NOT NULL
        UNIQUE
);
