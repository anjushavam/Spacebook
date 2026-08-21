-- Table: public.roles

-- DROP TABLE IF EXISTS public.roles;

CREATE TABLE IF NOT EXISTS public.roles
(
    roleid integer NOT NULL DEFAULT nextval('roles_roleid_seq'::regclass),
    rolename character varying(50) COLLATE pg_catalog."default" NOT NULL,
    CONSTRAINT roles_pkey PRIMARY KEY (roleid),
    CONSTRAINT roles_rolename_key UNIQUE (rolename)
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.roles
    OWNER to spacebook_user;