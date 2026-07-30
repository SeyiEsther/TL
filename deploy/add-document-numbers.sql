BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730131755_AddDocumentNumbers'
)
BEGIN
    CREATE TABLE [DocumentNumbers] (
        [Id] int NOT NULL IDENTITY,
        [FormType] nvarchar(60) NOT NULL,
        [Label] nvarchar(120) NOT NULL,
        [Number] nvarchar(120) NOT NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_DocumentNumbers] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730131755_AddDocumentNumbers'
)
BEGIN
    CREATE UNIQUE INDEX [IX_DocumentNumbers_FormType] ON [DocumentNumbers] ([FormType]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730131755_AddDocumentNumbers'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260730131755_AddDocumentNumbers', N'8.0.0');
END;
GO

COMMIT;
GO

