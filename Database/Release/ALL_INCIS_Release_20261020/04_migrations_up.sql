BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020132411_AddInciCatalogFields'
)
BEGIN
    ALTER TABLE [INCIs] ADD [DataSource] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020132411_AddInciCatalogFields'
)
BEGIN
    ALTER TABLE [INCIs] ADD [ImportedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020132411_AddInciCatalogFields'
)
BEGIN
    ALTER TABLE [INCIs] ADD [Origin] nvarchar(200) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020132411_AddInciCatalogFields'
)
BEGIN
    ALTER TABLE [INCIs] ADD [Restriction] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020132411_AddInciCatalogFields'
)
BEGIN
    ALTER TABLE [INCIs] ADD [ToxicologicalProfile] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020132411_AddInciCatalogFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261020132411_AddInciCatalogFields', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020133742_EnlargeInciCatalogFields'
)
BEGIN
    DROP INDEX [IX_INCIs_Name] ON [INCIs];
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[INCIs]') AND [c].[name] = N'Name');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [INCIs] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [INCIs] ALTER COLUMN [Name] nvarchar(500) NOT NULL;
    CREATE INDEX [IX_INCIs_Name] ON [INCIs] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020133742_EnlargeInciCatalogFields'
)
BEGIN
    DROP INDEX [IX_INCIs_ECNumber] ON [INCIs];
    DECLARE @var1 sysname;
    SELECT @var1 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[INCIs]') AND [c].[name] = N'ECNumber');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [INCIs] DROP CONSTRAINT [' + @var1 + '];');
    ALTER TABLE [INCIs] ALTER COLUMN [ECNumber] nvarchar(200) NULL;
    CREATE INDEX [IX_INCIs_ECNumber] ON [INCIs] ([ECNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020133742_EnlargeInciCatalogFields'
)
BEGIN
    DROP INDEX [IX_INCIs_CASNumber] ON [INCIs];
    DECLARE @var2 sysname;
    SELECT @var2 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[INCIs]') AND [c].[name] = N'CASNumber');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [INCIs] DROP CONSTRAINT [' + @var2 + '];');
    ALTER TABLE [INCIs] ALTER COLUMN [CASNumber] nvarchar(200) NULL;
    CREATE INDEX [IX_INCIs_CASNumber] ON [INCIs] ([CASNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020133742_EnlargeInciCatalogFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261020133742_EnlargeInciCatalogFields', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020141235_AddInciFullName'
)
BEGIN
    ALTER TABLE [INCIs] ADD [FullName] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020141235_AddInciFullName'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261020141235_AddInciFullName', N'9.0.0');
END;

COMMIT;
GO

