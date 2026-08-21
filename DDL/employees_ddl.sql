-- Table: public.employees

-- DROP TABLE IF EXISTS public.employees;

CREATE TABLE IF NOT EXISTS public.employees
(
    employeeid integer NOT NULL DEFAULT nextval('employees_employeeid_seq'::regclass),
    roleid integer NOT NULL,
    name character varying(100) COLLATE pg_catalog."default" NOT NULL,
    email character varying(100) COLLATE pg_catalog."default" NOT NULL,
    passwordhash character varying(255) COLLATE pg_catalog."default" NOT NULL,
    department character varying(100) COLLATE pg_catalog."default",
    isactive boolean NOT NULL DEFAULT true,
    createdon timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT employees_pkey PRIMARY KEY (employeeid),
    CONSTRAINT employees_email_key UNIQUE (email),
    CONSTRAINT fk_employees_roles FOREIGN KEY (roleid)
        REFERENCES public.roles (roleid) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.employees
    OWNER to spacebook_user;