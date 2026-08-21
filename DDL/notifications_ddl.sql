-- Table: public.notifications

-- DROP TABLE IF EXISTS public.notifications;

CREATE TABLE IF NOT EXISTS public.notifications
(
    notificationid integer NOT NULL DEFAULT nextval('notifications_notificationid_seq'::regclass),
    employeeid integer,
    bookingid integer,
    message character varying(500) COLLATE pg_catalog."default" NOT NULL,
    isread boolean DEFAULT false,
    createdat timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    hotseatbookingid integer,
    CONSTRAINT notifications_pkey PRIMARY KEY (notificationid),
    CONSTRAINT fk_notification_booking FOREIGN KEY (bookingid)
        REFERENCES public.bookings (bookingid) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION,
    CONSTRAINT fk_notifications_hotseatbookings FOREIGN KEY (hotseatbookingid)
        REFERENCES public.hotseatbookings (hotseatbookingid) MATCH SIMPLE
        ON UPDATE CASCADE
        ON DELETE RESTRICT,
    CONSTRAINT chk_notification_booking CHECK (bookingid IS NOT NULL OR hotseatbookingid IS NOT NULL)
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.notifications
    OWNER to spacebook_user;