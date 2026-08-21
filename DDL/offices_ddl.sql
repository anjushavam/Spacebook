-- Table: public.offices

-- DROP TABLE IF EXISTS public.offices;

CREATE TABLE IF NOT EXISTS public.offices
(
    officeid integer NOT NULL DEFAULT nextval('offices_officeid_seq'::regclass),
    locationid integer NOT NULL,
    officename character varying(100) COLLATE pg_catalog."default" NOT NULL,
    recordingestedby character varying(100) COLLATE pg_catalog."default",
    recordingestedon timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    recordmodifiedby character varying(100) COLLATE pg_catalog."default",
    recordmodifiedon timestamp with time zone,
    CONSTRAINT offices_pkey PRIMARY KEY (officeid),
    CONSTRAINT uq_office_location UNIQUE (locationid, officename),
    CONSTRAINT fk_offices_locations FOREIGN KEY (locationid)
        REFERENCES public.locations (locationid) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.offices
    OWNER to spacebook_user;