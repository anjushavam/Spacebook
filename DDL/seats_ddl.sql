-- Table: public.seats

-- DROP TABLE IF EXISTS public.seats;

CREATE TABLE IF NOT EXISTS public.seats
(
    seatid integer NOT NULL DEFAULT nextval('seats_seatid_seq'::regclass),
    moduleid integer NOT NULL,
    section character varying(50) COLLATE pg_catalog."default",
    seatnumber character varying(50) COLLATE pg_catalog."default" NOT NULL,
    rownumber character varying(50) COLLATE pg_catalog."default" NOT NULL,
    columnnumber integer NOT NULL,
    isactive boolean DEFAULT true,
    recordingestedby character varying(100) COLLATE pg_catalog."default",
    recordingestedon timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    recordmodifiedby character varying(100) COLLATE pg_catalog."default",
    recordmodifiedon timestamp with time zone,
    CONSTRAINT seats_pkey PRIMARY KEY (seatid),
    CONSTRAINT uq_seat_module UNIQUE (moduleid, seatnumber),
    CONSTRAINT uq_seat_position UNIQUE (moduleid, section, rownumber, columnnumber),
    CONSTRAINT chk_seat_column CHECK (columnnumber > 0)
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.seats
    OWNER to spacebook_user;