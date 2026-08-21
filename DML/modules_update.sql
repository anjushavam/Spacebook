UPDATE public.modules
	SET moduleid=?, officeid=?, modulename=?, recordingestedby=?, recordingestedon=?, recordmodifiedby=?, recordmodifiedon=?
	WHERE <condition>;

UPDATE public.modules
SET modulename = 'Module 1'
WHERE moduleid = 1;

UPDATE public.modules
SET modulename = 'Module 2'
WHERE moduleid = 2;

UPDATE public.modules
SET modulename = 'Module 1'
WHERE moduleid = 3;


UPDATE public.modules
SET modulename = 'Module 1 - Elcot Park - CMB'
WHERE moduleid = 1;

UPDATE public.modules
SET modulename = 'Module 2 - Elcot Park - CMB'
WHERE moduleid = 2;

UPDATE public.modules
SET modulename = 'Module 1 - Tidel Park - CMB'
WHERE moduleid = 3;

SELECT *
FROM public.modules
ORDER BY moduleid;

