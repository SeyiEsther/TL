/*
  Grant "Steven White" full access to all areas (HoD, Senior, Management, all
  dashboards and history) — everything EXCEPT Admin.

  Full access is the FullAccess picker list. The app seeds this list only when
  it is empty, so on the live database (already seeded) the new default in
  ShiftManagerList.cs will NOT appear on its own — run this once in SSMS.

  Safe to re-run: it inserts only if the name isn't already present.
*/
IF NOT EXISTS (
    SELECT 1 FROM dbo.PickerPersons
    WHERE ListKind = N'FullAccess' AND LOWER(Name) = LOWER(N'Steven White'))
BEGIN
    INSERT INTO dbo.PickerPersons (ListKind, Name, SortOrder)
    SELECT N'FullAccess', N'Steven White',
           ISNULL(MAX(SortOrder), 0) + 1
    FROM dbo.PickerPersons
    WHERE ListKind = N'FullAccess';
END

SELECT Id, ListKind, Name, SortOrder
FROM dbo.PickerPersons
WHERE ListKind = N'FullAccess'
ORDER BY SortOrder;
