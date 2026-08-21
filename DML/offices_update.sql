UPDATE public.offices
	SET officeid=?, locationid=?, officename=?, recordingestedby=?, recordingestedon=?, recordmodifiedby=?, recordmodifiedon=?
	WHERE <condition>;

UPDATE public.offices
SET officename = 'Elcot Park'
WHERE officeid = 1;

UPDATE public.offices
SET officename = 'Tidel Park'
WHERE officeid = 2;

SELECT *
FROM public.offices
ORDER BY officeid;

