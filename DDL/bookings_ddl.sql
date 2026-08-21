-- Table: public.bookings

-- DROP TABLE IF EXISTS public.bookings;

CREATE TABLE IF NOT EXISTS public.bookings
(
    bookingid integer NOT NULL DEFAULT nextval('bookings_bookingid_seq'::regclass),
    roomid integer NOT NULL,
    employeeid integer NOT NULL,
    purpose character varying(255) COLLATE pg_catalog."default" NOT NULL,
    participantcount integer NOT NULL,
    bookingdate date NOT NULL,
    starttime time without time zone NOT NULL,
    endtime time without time zone NOT NULL,
    bookedon timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    status character varying(20) COLLATE pg_catalog."default" NOT NULL,
    meetingtitle character varying(200) COLLATE pg_catalog."default" NOT NULL DEFAULT ''::character varying,
    CONSTRAINT bookings_pkey PRIMARY KEY (bookingid),
    CONSTRAINT fk_bookings_employees FOREIGN KEY (employeeid)
        REFERENCES public.employees (employeeid) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION,
    CONSTRAINT fk_bookings_rooms FOREIGN KEY (roomid)
        REFERENCES public.rooms (roomid) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION,
    CONSTRAINT bookings_participantcount_check CHECK (participantcount > 0),
    CONSTRAINT chk_booking_time CHECK (endtime > starttime)
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.bookings
    OWNER to spacebook_user;