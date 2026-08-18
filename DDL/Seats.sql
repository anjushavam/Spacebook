CREATE TABLE Seats
(
    SeatId SERIAL PRIMARY KEY,
    ModuleId INT NOT NULL,
    Section VARCHAR(50),
    SeatNumber VARCHAR(50) NOT NULL,
    RowNumber VARCHAR (10) NOT NULL,
    ColumnNumber INT NOT NULL,
    IsActive BOOLEAN DEFAULT TRUE,

    CONSTRAINT FK_Seats_Modules
        FOREIGN KEY (ModuleId)
        REFERENCES Modules(ModuleId),

    CONSTRAINT UQ_Seat_Module
        UNIQUE (ModuleId, SeatNumber),

    CONSTRAINT UQ_Seat_Position
        UNIQUE (ModuleId, Section, RowNumber, ColumnNumber),

    CONSTRAINT CHK_Seat_Column
        CHECK (ColumnNumber > 0),

    RecordIngestedBy VARCHAR(100),
    RecordIngestedOn TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    RecordModifiedBy VARCHAR(100),
    RecordModifiedOn TIMESTAMPTZ
);