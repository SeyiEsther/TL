/*
  OPTIONAL data cleanup — run only if you want EXISTING (historical) HoD audit
  records re-labelled to the new sheetmetal grouping.

  Nothing structural changes: no new tables/columns. Shift records store the
  zone label (e.g. "Zone 19"), not the group, so they are unaffected and need
  no update. Only HodDailyAudits carries a Department (group) string, and only
  its label is being modernised — the audit content is untouched.

  The old "Sheet Metal" group split across BOTH new groups, so records are
  remapped by their zone (Area), not by their old department text.

  Safe to re-run. Review the SELECT at the bottom before/after.
*/

-- Phase 1 Weld: Zones 7–15
UPDATE dbo.HodDailyAudits
SET Department = N'Phase 1 Weld'
WHERE Area IN (N'Zone 7', N'Zone 8', N'Zone 9', N'Zone 10', N'Zone 11',
               N'Zone 12', N'Zone 13', N'Zone 14', N'Zone 15')
  AND Department IN (N'Sheet Metal', N'Phase 1 Sheetmetal', N'Phase 3 Sheetmetal');

-- Phase 3 Pierce and Fold: Zones 1,2,3,16,17,20,22,4,5,6,23,18,19,21
UPDATE dbo.HodDailyAudits
SET Department = N'Phase 3 Pierce and Fold'
WHERE Area IN (N'Zone 1', N'Zone 2', N'Zone 3', N'Zone 16', N'Zone 17',
               N'Zone 20', N'Zone 22', N'Zone 4', N'Zone 5', N'Zone 6',
               N'Zone 23', N'Zone 18', N'Zone 19', N'Zone 21')
  AND Department IN (N'Sheet Metal', N'Phase 1 Sheetmetal', N'Phase 3 Sheetmetal');

-- Verify
SELECT Department, COUNT(*) AS Records
FROM dbo.HodDailyAudits
WHERE Area LIKE N'Zone %'
GROUP BY Department
ORDER BY Department;
