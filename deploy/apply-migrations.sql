IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260420105748_InitialCreate'
)
BEGIN
    CREATE TABLE [ShiftSubmissions] (
        [Id] int NOT NULL IDENTITY,
        [SubmittedAt] datetime2 NOT NULL,
        [SubmittedBy] nvarchar(max) NOT NULL,
        [TeamLeaderDisplay] nvarchar(max) NOT NULL,
        [ShiftDate] date NOT NULL,
        [Shift] nvarchar(max) NOT NULL,
        [Area] nvarchar(max) NULL,
        [HoursCompleted] tinyint NOT NULL,
        [Escalations] nvarchar(max) NULL,
        [KeyRisks] nvarchar(max) NULL,
        [Priorities] nvarchar(max) NULL,
        [OutgoingTLSignature] nvarchar(max) NULL,
        [IncomingTLSignature] nvarchar(max) NULL,
        [LastEditedBy] nvarchar(max) NULL,
        [LastEditedAt] datetime2 NULL,
        CONSTRAINT [PK_ShiftSubmissions] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260420105748_InitialCreate'
)
BEGIN
    CREATE TABLE [AuditLogs] (
        [Id] int NOT NULL IDENTITY,
        [SubmissionId] int NOT NULL,
        [ChangedBy] nvarchar(max) NOT NULL,
        [ChangedAt] datetime2 NOT NULL,
        [FieldName] nvarchar(max) NOT NULL,
        [OldValue] nvarchar(max) NULL,
        [NewValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AuditLogs_ShiftSubmissions_SubmissionId] FOREIGN KEY ([SubmissionId]) REFERENCES [ShiftSubmissions] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260420105748_InitialCreate'
)
BEGIN
    CREATE TABLE [HourlyChecks] (
        [Id] int NOT NULL IDENTITY,
        [ShiftSubmissionId] int NOT NULL,
        [HourNumber] tinyint NOT NULL,
        [HazardsObserved] bit NULL,
        [UnsafeBehaviours] bit NULL,
        [PositiveBehaviours] bit NULL,
        [SafetyNotes] nvarchar(max) NULL,
        [QualityChecksCompleted] bit NULL,
        [DeviationsEscalated] bit NULL,
        [NonComplianceAddressed] bit NULL,
        [QualityNotes] nvarchar(max) NULL,
        [HourlyTargetAchieved] bit NULL,
        [MaintenanceIssues] bit NULL,
        [MaterialsAvailable] bit NULL,
        [ToolsAvailable] bit NULL,
        [EscalationsNeeded] bit NULL,
        [PartsConfirmed] bit NULL,
        [PartsIdCorrect] bit NULL,
        [NCPartsStoredCorrectly] bit NULL,
        [SixSCompleted] bit NULL,
        [TPMCompleted] bit NULL,
        [PerformanceNotes] nvarchar(max) NULL,
        [WellbeingConfirmed] bit NULL,
        [SupportRequired] bit NULL,
        [MoraleNotes] nvarchar(max) NULL,
        [AccidentsReported] bit NULL,
        [OverallSafetyStatus] nvarchar(max) NULL,
        [OverallQualityStatus] nvarchar(max) NULL,
        [OverallPerfStatus] nvarchar(max) NULL,
        CONSTRAINT [PK_HourlyChecks] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_HourlyChecks_ShiftSubmissions_ShiftSubmissionId] FOREIGN KEY ([ShiftSubmissionId]) REFERENCES [ShiftSubmissions] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260420105748_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_SubmissionId] ON [AuditLogs] ([SubmissionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260420105748_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_HourlyChecks_ShiftSubmissionId] ON [HourlyChecks] ([ShiftSubmissionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260420105748_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260420105748_InitialCreate', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260522000000_AddIncomingTLSignedAt'
)
BEGIN
    ALTER TABLE [ShiftSubmissions] ADD [IncomingTLSignedAt] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260522000000_AddIncomingTLSignedAt'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260522000000_AddIncomingTLSignedAt', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722123653_AddSeniorAuditEditTracking'
)
BEGIN
    ALTER TABLE [SeniorWeeklyAudits] ADD [LastEditedAt] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722123653_AddSeniorAuditEditTracking'
)
BEGIN
    ALTER TABLE [SeniorWeeklyAudits] ADD [LastEditedBy] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722123653_AddSeniorAuditEditTracking'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260722123653_AddSeniorAuditEditTracking', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729132027_AddTeamMeetings'
)
BEGIN
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
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729132027_AddTeamMeetings'
)
BEGIN
    CREATE INDEX [IX_TeamMeetings_MeetingDate_Area_Shift] ON [TeamMeetings] ([MeetingDate], [Area], [Shift]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729132027_AddTeamMeetings'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260729132027_AddTeamMeetings', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730021721_ShrinkTeamMeetingKeyColumns'
)
BEGIN
    DROP INDEX [IX_TeamMeetings_MeetingDate_Area_Shift] ON [TeamMeetings];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730021721_ShrinkTeamMeetingKeyColumns'
)
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[TeamMeetings]') AND [c].[name] = N'Shift');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [TeamMeetings] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [TeamMeetings] ALTER COLUMN [Shift] nvarchar(30) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730021721_ShrinkTeamMeetingKeyColumns'
)
BEGIN
    DECLARE @var1 sysname;
    SELECT @var1 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[TeamMeetings]') AND [c].[name] = N'Area');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [TeamMeetings] DROP CONSTRAINT [' + @var1 + '];');
    ALTER TABLE [TeamMeetings] ALTER COLUMN [Area] nvarchar(200) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730021721_ShrinkTeamMeetingKeyColumns'
)
BEGIN
    CREATE INDEX [IX_TeamMeetings_MeetingDate_Area_Shift] ON [TeamMeetings] ([MeetingDate], [Area], [Shift]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730021721_ShrinkTeamMeetingKeyColumns'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260730021721_ShrinkTeamMeetingKeyColumns', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730072445_AddPreKitBreakInCheck'
)
BEGIN
    ALTER TABLE [HourlyChecks] ADD [PreKitBreakInFollowed] bit NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730072445_AddPreKitBreakInCheck'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260730072445_AddPreKitBreakInCheck', N'8.0.0');
END;
GO

COMMIT;
GO

