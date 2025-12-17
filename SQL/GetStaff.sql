SELECT
	s.fname As [FName],
	s.lname AS [LName],
	s.email AS [Email],
	'SHN' AS [ACCOUNT NAME],
	'SHARENOTE' AS [PRODUCT],
	s.providerid AS [PLATFORM ID],
	s.position AS [Role],
	s.phone AS [Phone],
	CASE
		WHEN s.active = 1 THEN 'Y'
		ELSE 'N'
	END AS [Status]
FROM tbl_sn_staff s
join  tbl_sn_providers p
ON s.providerid = p.id;