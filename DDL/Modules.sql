CREATE TABLE Modules
(
    ModuleId SERIAL PRIMARY KEY,
    OfficeId INT NOT NULL,
    ModuleName VARCHAR(150) NOT NULL,

    CONSTRAINT FK_Modules_Offices
        FOREIGN KEY (OfficeId)
        REFERENCES Offices(OfficeId),

    CONSTRAINT UQ_Module_Office
        UNIQUE (OfficeId, ModuleName),

    RecordIngestedBy VARCHAR(100),
    RecordIngestedOn TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    RecordModifiedBy VARCHAR(100),
    RecordModifiedOn TIMESTAMPTZ
);