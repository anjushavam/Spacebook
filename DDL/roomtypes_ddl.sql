-- Table: public.roomtypes

-- DROP TABLE IF EXISTS public.roomtypes;

CREATE TABLE IF NOT EXISTS public.roomtypes
(
    roomtypeid integer NOT NULL DEFAULT nextval('roomtypes_roomtypeid_seq'::regclass),
    typename character varying(100) COLLATE pg_catalog."default" NOT NULL,
    CONSTRAINT roomtypes_pkey PRIMARY KEY (roomtypeid),
    CONSTRAINT roomtypes_typename_key UNIQUE (typename)
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.roomtypes
    OWNER to spacebook_user;