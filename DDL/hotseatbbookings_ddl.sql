-- Table: public.hotseatbookings

-- DROP TABLE IF EXISTS public.hotseatbookings;

CREATE TABLE IF NOT EXISTS public.hotseatbookings
(
    hotseatbookingid integer NOT NULL DEFAULT nextval('hotseatbookings_hotseatbookingid_seq'::regclass),
    seatid integer NOT NULL,
    employeeid integer NOT NULL,
    bookingdate date NOT NULL,
    bookingstatus character varying(30) COLLATE pg_catalog."default" DEFAULT 'Confirmed'::character varying,
    bookedon timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    checkindeadline timestamp with time zone,
    checkintime timestamp with time zone,
    releasedon timestamp with time zone,
    recordingestedby character varying(100) COLLATE pg_catalog."default",
    recordingestedon timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    recordmodifiedby character varying(100) COLLATE pg_catalog."default",
    recordmodifiedon timestamp with time zone,
    CONSTRAINT hotseatbookings_pkey PRIMARY KEY (hotseatbookingid),
    CONSTRAINT uq_seat_booking_date UNIQUE (seatid, bookingdate),
    CONSTRAINT fk_hotseatbookings_employees FOREIGN KEY (employeeid)
        REFERENCES public.employees (employeeid) MATCH SIMPLE
        ON UPDATE CASCADE
        ON DELETE RESTRICT,
    CONSTRAINT fk_hotseatbookings_seats FOREIGN KEY (seatid)
        REFERENCES public.seats (seatid) MATCH SIMPLE
        ON UPDATE CASCADE
        ON DELETE RESTRICT,
    CONSTRAINT chk_hotseatbooking_status CHECK (bookingstatus::text = ANY (ARRAY['Confirmed'::character varying, 'Cancelled'::character varying, 'CheckedIn'::character varying, 'Released'::character varying, 'Expired'::character varying]::text[]))
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.hotseatbookings
    OWNER to spacebook_user;