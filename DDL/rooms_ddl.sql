-- Table: public.rooms

-- DROP TABLE IF EXISTS public.rooms;

CREATE TABLE IF NOT EXISTS public.rooms
(
    roomid integer NOT NULL DEFAULT nextval('rooms_roomid_seq'::regclass),
    roomtypeid integer NOT NULL,
    roomname character varying(100) COLLATE pg_catalog."default" NOT NULL,
    capacity integer NOT NULL,
    status character varying(20) COLLATE pg_catalog."default" NOT NULL,
    isblocked boolean DEFAULT false,
    moduleid integer,
    CONSTRAINT rooms_pkey PRIMARY KEY (roomid),
    CONSTRAINT fk_rooms_modules FOREIGN KEY (moduleid)
        REFERENCES public.modules (moduleid) MATCH SIMPLE
        ON UPDATE CASCADE
        ON DELETE RESTRICT,
    CONSTRAINT fk_rooms_roomtypes FOREIGN KEY (roomtypeid)
        REFERENCES public.roomtypes (roomtypeid) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION,
    CONSTRAINT rooms_capacity_check CHECK (capacity > 0)
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.rooms
    OWNER to spacebook_user;