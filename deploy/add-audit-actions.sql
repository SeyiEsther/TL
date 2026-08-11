BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811101748_AddAuditActions'
)
BEGIN
    CREATE TABLE [AuditActions] (
        [Id] int NOT NULL IDENTITY,
        [SourceType] nvarchar(40) NOT NULL,
        [SourceId] int NULL,
        [SourceLabel] nvarchar(max) NULL,
        [AuditType] nvarchar(120) NULL,
        [Area] nvarchar(200) NULL,
        [AuditDate] date NULL,
        [Text] nvarchar(max) NOT NULL,
        [RaisedByName] nvarchar(256) NOT NULL,
        [RaisedByUsername] nvarchar(256) NOT NULL,
        [RaisedAt] datetime2 NOT NULL,
        [OwnerName] nvarchar(256) NOT NULL,
        [OwnerKey] nvarchar(256) NOT NULL,
        [OwnerIsExternal] bit NOT NULL,
        [DueDate] date NULL,
        [Status] nvarchar(20) NOT NULL,
        [CompletedAt] datetime2 NULL,
        [CompletedByName] nvarchar(256) NULL,
        [CompletionNote] nvarchar(max) NULL,
        CONSTRAINT [PK_AuditActions] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811101748_AddAuditActions'
)
BEGIN
    CREATE INDEX [IX_AuditActions_SourceType_SourceId] ON [AuditActions] ([SourceType], [SourceId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811101748_AddAuditActions'
)
BEGIN
    CREATE INDEX [IX_AuditActions_Status_OwnerKey] ON [AuditActions] ([Status], [OwnerKey]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811101748_AddAuditActions'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260811101748_AddAuditActions', N'8.0.0');
END;
GO

COMMIT;
GO

