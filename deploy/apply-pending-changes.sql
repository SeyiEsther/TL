/*
  Targeted, safe-to-re-run script for the two schema changes not yet applied:
    1. HourlyChecks.PreKitBreakInFollowed  (Assembly pre-kitting/break-in check)
    2. TeamMeetings key columns shrunk under the 1700-byte index limit

  Each block checks the ACTUAL schema (not just migration history), so it works
  no matter what state the DB drifted into, and it stamps __EFMigrationsHistory
  so EF stays consistent going forward. Run the whole thing in SSMS.
*/

-------------------------------------------------------------------------------
-- 0. OPTIONAL: see current state first
-------------------------------------------------------------------------------
SELECT MigrationId FROM [__EFMigrationsHistory] ORDER BY MigrationId;
-- HourlyChecks column present?
SELECT name, system_type_id FROM sys.columns
WHERE object_id = OBJECT_ID('dbo.HourlyChecks') AND name = 'PreKitBreakInFollowed';
-- TeamMeetings Area/Shift sizes (max_length: 900 = nvarchar(450); 400 = nvarchar(200); 60 = nvarchar(30))
SELECT name, max_length FROM sys.columns
WHERE object_id = OBJECT_ID('dbo.TeamMeetings') AND name IN ('Area','Shift');
GO

-------------------------------------------------------------------------------
-- 1. Pre-kitting / break-in check column on HourlyChecks
-------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('dbo.HourlyChecks') AND name = 'PreKitBreakInFollowed')
BEGIN
    ALTER TABLE dbo.HourlyChecks ADD [PreKitBreakInFollowed] bit NULL;
END
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory]
               WHERE MigrationId = N'20260730072445_AddPreKitBreakInCheck')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260730072445_AddPreKitBreakInCheck', N'8.0.0');
END
GO

-------------------------------------------------------------------------------
-- 2. Shrink TeamMeetings Area/Shift so the composite index fits (<1700 bytes).
--    Only runs if Area is still nvarchar(450) (max_length 900).
-------------------------------------------------------------------------------
IF EXISTS (SELECT 1 FROM sys.columns
           WHERE object_id = OBJECT_ID('dbo.TeamMeetings') AND name = 'Area' AND max_length = 900)
BEGIN
    IF EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'IX_TeamMeetings_MeetingDate_Area_Shift' AND object_id = OBJECT_ID('dbo.TeamMeetings'))
        DROP INDEX [IX_TeamMeetings_MeetingDate_Area_Shift] ON [dbo].[TeamMeetings];

    ALTER TABLE [dbo].[TeamMeetings] ALTER COLUMN [Area]  nvarchar(200) NOT NULL;
    ALTER TABLE [dbo].[TeamMeetings] ALTER COLUMN [Shift] nvarchar(30)  NOT NULL;

    CREATE INDEX [IX_TeamMeetings_MeetingDate_Area_Shift]
        ON [dbo].[TeamMeetings] ([MeetingDate], [Area], [Shift]);
END
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory]
               WHERE MigrationId = N'20260730021721_ShrinkTeamMeetingKeyColumns')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260730021721_ShrinkTeamMeetingKeyColumns', N'8.0.0');
END
GO
