BEGIN TRANSACTION;
GO

CREATE TABLE [TeamMeetings] (
    [Id] int NOT NULL IDENTITY,
    [MeetingDate] date NOT NULL,
    [Area] nvarchar(450) NOT NULL,
    [Shift] nvarchar(450) NOT NULL,
    [Supervisor] nvarchar(max) NULL,
    [CostCentre] nvarchar(max) NULL,
    [Team] nvarchar(max) NULL,
    [CompiledOn] date NULL,
    [Location] nvarchar(max) NULL,
    [MeetingDateTime] nvarchar(max) NULL,
    [Actions] nvarchar(max) NULL,
    [HealthSafety] nvarchar(max) NULL,
    [CustomerSatisfaction] nvarchar(max) NULL,
    [Kpis] nvarchar(max) NULL,
    [EmployeeSatisfaction] nvarchar(max) NULL,
    [Aob] nvarchar(max) NULL,
    [MeetingResults] nvarchar(max) NULL,
    [MinutesKeeper] nvarchar(max) NULL,
    [ProblemsJson] nvarchar(max) NULL,
    [TeamMembersJson] nvarchar(max) NULL,
    [GroupMembersJson] nvarchar(max) NULL,
    [GuestsJson] nvarchar(max) NULL,
    [SubmittedBy] nvarchar(max) NOT NULL,
    [SubmittedAt] datetime2 NOT NULL,
    [LastEditedBy] nvarchar(max) NULL,
    [LastEditedAt] datetime2 NULL,
    CONSTRAINT [PK_TeamMeetings] PRIMARY KEY ([Id])
);
GO

CREATE INDEX [IX_TeamMeetings_MeetingDate_Area_Shift] ON [TeamMeetings] ([MeetingDate], [Area], [Shift]);
GO

-- Only if history does NOT already have the row (check first):
-- INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
-- VALUES (N'20260729132027_AddTeamMeetings', N'8.0.0');
GO

COMMIT;
GO

