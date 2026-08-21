ALTER TABLE public.bookings
ALTER COLUMN bookedon TYPE timestamp with time zone
USING bookedon AT TIME ZONE 'Asia/Kolkata';

ALTER TABLE public.checkins
ALTER COLUMN checkedinat TYPE timestamp with time zone
USING checkedinat AT TIME ZONE 'Asia/Kolkata';

ALTER TABLE public.employees
ALTER COLUMN createdon TYPE timestamp with time zone
USING createdon AT TIME ZONE 'Asia/Kolkata';

ALTER TABLE public.notifications
ALTER COLUMN createdat TYPE timestamp with time zone
USING createdat AT TIME ZONE 'Asia/Kolkata';

SET TIME ZONE 'Asia/Kolkata';

ALTER DATABASE spacebookdb
SET timezone TO 'Asia/Kolkata';

SELECT
    table_name,
    column_name,
    data_type
FROM information_schema.columns
WHERE data_type LIKE 'timestamp%'
ORDER BY table_name, column_name;