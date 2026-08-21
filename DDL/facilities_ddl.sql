-- Table: public.facilities

-- DROP TABLE IF EXISTS public.facilities;

CREATE TABLE IF NOT EXISTS public.facilities
(
    facilityid integer NOT NULL DEFAULT nextval('facilities_facilityid_seq'::regclass),
    facilityname character varying(100) COLLATE pg_catalog."default" NOT NULL,
    CONSTRAINT facilities_pkey PRIMARY KEY (facilityid),
    CONSTRAINT facilities_facilityname_key UNIQUE (facilityname)
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.facilities
    OWNER to spacebook_user;