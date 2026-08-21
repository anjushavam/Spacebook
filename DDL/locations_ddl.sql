-- Table: public.locations

-- DROP TABLE IF EXISTS public.locations;

CREATE TABLE IF NOT EXISTS public.locations
(
    locationid integer NOT NULL DEFAULT nextval('locations_locationid_seq'::regclass),
    locationname character varying(100) COLLATE pg_catalog."default" NOT NULL,
    recordingestedby character varying(100) COLLATE pg_catalog."default",
    recordingestedon timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    recordmodifiedby character varying(100) COLLATE pg_catalog."default",
    recordmodifiedon timestamp with time zone,
    CONSTRAINT locations_pkey PRIMARY KEY (locationid),
    CONSTRAINT locations_locationname_key UNIQUE (locationname)
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.locations
    OWNER to spacebook_user;