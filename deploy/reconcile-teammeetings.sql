/*
  Reconcile a hand-created TeamMeetings table with the EF model/migration.
  The EF migration (AddTeamMeetings) is the single source of truth. Because the
  feature is brand new with no real data, the clean fix is to drop the manual
  table and let the app's Database.Migrate() create it correctly on next start.

  RUN THIS BEFORE DEPLOYING/STARTING THE NEW BUILD — otherwise Migrate() will
  fail with "There is already an object named 'TeamMeetings'".
*/

-- 1. INSPECT: what does the hand-created table currently look like?
SELECT c.column_id, c.name AS column_name, t.name AS data_type,
       c.max_length, c.is_nullable
FROM sys.columns c
JOIN sys.types t ON c.user_type_id = t.user_type_id
WHERE c.object_id = OBJECT_ID('dbo.TeamMeetings')
ORDER BY c.column_id;
-- max_length: nvarchar(max) shows as -1; nvarchar(450) shows as 900 (bytes).

-- 2. Has EF already recorded the migration? This should return NO rows.
SELECT * FROM __EFMigrationsHistory WHERE MigrationId LIKE '%AddTeamMeetings%';

-- 3. RECONCILE (safe: no data to preserve).
--    Drop the manual table so the EF migration owns the schema.
DROP TABLE IF EXISTS dbo.TeamMeetings;

-- 4. ONLY if step 2 returned a row (someone manually stamped the migration),
--    remove it so Migrate() will actually recreate the table on startup:
-- DELETE FROM __EFMigrationsHistory WHERE MigrationId LIKE '%AddTeamMeetings%';

/*
  Now start the app. Database.Migrate() will run AddTeamMeetings, create the
  table with the correct columns/types/index, and stamp __EFMigrationsHistory.
  From here the migration is the single source of truth and cannot drift.
*/
