-- Table: public.modules

-- DROP TABLE IF EXISTS public.modules;

CREATE TABLE IF NOT EXISTS public.modules
(
    moduleid integer NOT NULL DEFAULT nextval('modules_moduleid_seq'::regclass),
    officeid integer NOT NULL,
    modulename character varying(150) COLLATE pg_catalog."default" NOT NULL,
    recordingestedby character varying(100) COLLATE pg_catalog."default",
    recordingestedon timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    recordmodifiedby character varying(100) COLLATE pg_catalog."default",
    recordmodifiedon timestamp with time zone,
    CONSTRAINT modules_pkey PRIMARY KEY (moduleid),
    CONSTRAINT uq_module_office UNIQUE (officeid, modulename)
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.modules
    OWNER to spacebook_user;