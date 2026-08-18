SET TIME ZONE 'Asia/Kolkata';
SET datestyle = 'SQL, MDY';

CREATE TABLE Locations
(
    LocationId SERIAL PRIMARY KEY,
    LocationName VARCHAR(100) NOT NULL UNIQUE,

    RecordIngestedBy VARCHAR(100),
    RecordIngestedOn TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    RecordModifiedBy VARCHAR(100),
    RecordModifiedOn TIMESTAMPTZ
);