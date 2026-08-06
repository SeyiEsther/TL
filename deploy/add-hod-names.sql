/*
  Add Piotr Pelka, Alison Gilley and Michael Tregillis to the HOD name list
  (the Hod picker used on the audit start form and HOD access).

  The app seeds this list only when empty, so on the already-seeded live
  database these new defaults won't appear on their own — run this once in SSMS.
  Safe to re-run: each name is inserted only if not already present.
*/
DECLARE @names TABLE (Name nvarchar(200));
INSERT INTO @names (Name) VALUES (N'Piotr Pelka'), (N'Alison Gilley'), (N'Michael Tregillis');

INSERT INTO dbo.PickerPersons (ListKind, Name, SortOrder)
SELECT N'Hod', n.Name,
       (SELECT ISNULL(MAX(SortOrder), 0) FROM dbo.PickerPersons WHERE ListKind = N'Hod')
         + ROW_NUMBER() OVER (ORDER BY n.Name)
FROM @names n
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.PickerPersons p
    WHERE p.ListKind = N'Hod' AND LOWER(p.Name) = LOWER(n.Name));

SELECT Id, ListKind, Name, SortOrder
FROM dbo.PickerPersons
WHERE ListKind = N'Hod'
ORDER BY SortOrder;
