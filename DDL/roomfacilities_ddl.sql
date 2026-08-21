-- Table: public.roomfacilities

-- DROP TABLE IF EXISTS public.roomfacilities;

CREATE TABLE IF NOT EXISTS public.roomfacilities
(
    roomfacilityid integer NOT NULL DEFAULT nextval('roomfacilities_roomfacilityid_seq'::regclass),
    roomid integer NOT NULL,
    facilityid integer NOT NULL,
    CONSTRAINT roomfacilities_pkey PRIMARY KEY (roomfacilityid),
    CONSTRAINT uq_roomfacilities UNIQUE (roomid, facilityid),
    CONSTRAINT fk_roomfacilities_facilities FOREIGN KEY (facilityid)
        REFERENCES public.facilities (facilityid) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE CASCADE,
    CONSTRAINT fk_roomfacilities_rooms FOREIGN KEY (roomid)
        REFERENCES public.rooms (roomid) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE CASCADE
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.roomfacilities
    OWNER to spacebook_user;