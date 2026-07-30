/*
  Fix the "maximum key length ... 1803 bytes > 1700" warning on the existing
  TeamMeetings table (created while Area/Shift were nvarchar(450)).

  Shrinks the two natural-key columns so the composite index fits comfortably:
    date(3) + Area nvarchar(200)=400 + Shift nvarchar(30)=60 = 463 bytes.
  All free-text/notes columns are untouched (still nvarchar(max)).

  This matches the ShrinkTeamMeetingKeyColumns migration, so it's safe whether
  you run this now for immediate relief OR let the migration apply on deploy —
  either way the end state is identical. Real area/shift names are tiny, so
  there is no truncation risk at these sizes.
*/

DROP INDEX [IX_TeamMeetings_MeetingDate_Area_Shift] ON [TeamMeetings];
GO
ALTER TABLE [TeamMeetings] ALTER COLUMN [Area]  nvarchar(200) NOT NULL;
GO
ALTER TABLE [TeamMeetings] ALTER COLUMN [Shift] nvarchar(30)  NOT NULL;
GO
CREATE INDEX [IX_TeamMeetings_MeetingDate_Area_Shift]
    ON [TeamMeetings] ([MeetingDate], [Area], [Shift]);
GO
