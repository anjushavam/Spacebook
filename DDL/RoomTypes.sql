CREATE TABLE RoomTypes
(
    RoomTypeId SERIAL PRIMARY KEY,

    TypeName VARCHAR(100) UNIQUE,

    RecordIngestedBy VARCHAR(100),
    RecordIngestedOn TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    RecordModifiedBy VARCHAR(100),
    RecordModifiedOn TIMESTAMPTZ
);

SET TIME ZONE 'Asia/Kolkata';
SET datestyle = 'SQL, MDY';