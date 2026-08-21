UPDATE public.employees
	SET employeeid=?, roleid=?, name=?, email=?, passwordhash=?, department=?, isactive=?, createdon=?
	WHERE <condition>;

BEGIN;

UPDATE public.employees
SET department = 'Admin'
WHERE employeeid = 105517;

DELETE FROM public.employees
WHERE employeeid IN (1, 2);

COMMIT;


SELECT
    employeeid,
    roleid,
    name,
    email,
    department,
    isactive
FROM public.employees
ORDER BY employeeid;
