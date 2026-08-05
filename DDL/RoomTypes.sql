CREATE TABLE RoomTypes
(
    RoomTypeId SERIAL PRIMARY KEY,

    TypeName VARCHAR(100)
        NOT NULL
        UNIQUE
);

