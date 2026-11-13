BEGIN TRANSACTION;
DECLARE @var0 sysname;
SELECT @var0 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[INCIs]') AND [c].[name] = N'FullName');
IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [INCIs] DROP CONSTRAINT [' + @var0 + '];');
ALTER TABLE [INCIs] DROP COLUMN [FullName];

DELETE FROM [__EFMigrationsHistory]
WHERE [MigrationId] = N'20261020141235_AddInciFullName';

DROP INDEX [IX_INCIs_Name] ON [INCIs];
DECLARE @var1 sysname;
SELECT @var1 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[INCIs]') AND [c].[name] = N'Name');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [INCIs] DROP CONSTRAINT [' + @var1 + '];');
ALTER TABLE [INCIs] ALTER COLUMN [Name] nvarchar(200) NOT NULL;
CREATE INDEX [IX_INCIs_Name] ON [INCIs] ([Name]);

DROP INDEX [IX_INCIs_ECNumber] ON [INCIs];
DECLARE @var2 sysname;
SELECT @var2 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[INCIs]') AND [c].[name] = N'ECNumber');
IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [INCIs] DROP CONSTRAINT [' + @var2 + '];');
ALTER TABLE [INCIs] ALTER COLUMN [ECNumber] nvarchar(100) NULL;
CREATE INDEX [IX_INCIs_ECNumber] ON [INCIs] ([ECNumber]);

DROP INDEX [IX_INCIs_CASNumber] ON [INCIs];
DECLARE @var3 sysname;
SELECT @var3 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[INCIs]') AND [c].[name] = N'CASNumber');
IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [INCIs] DROP CONSTRAINT [' + @var3 + '];');
ALTER TABLE [INCIs] ALTER COLUMN [CASNumber] nvarchar(100) NULL;
CREATE INDEX [IX_INCIs_CASNumber] ON [INCIs] ([CASNumber]);

DELETE FROM [__EFMigrationsHistory]
WHERE [MigrationId] = N'20261020133742_EnlargeInciCatalogFields';

DECLARE @var4 sysname;
SELECT @var4 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[INCIs]') AND [c].[name] = N'DataSource');
IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [INCIs] DROP CONSTRAINT [' + @var4 + '];');
ALTER TABLE [INCIs] DROP COLUMN [DataSource];

DECLARE @var5 sysname;
SELECT @var5 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[INCIs]') AND [c].[name] = N'ImportedAt');
IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [INCIs] DROP CONSTRAINT [' + @var5 + '];');
ALTER TABLE [INCIs] DROP COLUMN [ImportedAt];

DECLARE @var6 sysname;
SELECT @var6 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[INCIs]') AND [c].[name] = N'Origin');
IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [INCIs] DROP CONSTRAINT [' + @var6 + '];');
ALTER TABLE [INCIs] DROP COLUMN [Origin];

DECLARE @var7 sysname;
SELECT @var7 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[INCIs]') AND [c].[name] = N'Restriction');
IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [INCIs] DROP CONSTRAINT [' + @var7 + '];');
ALTER TABLE [INCIs] DROP COLUMN [Restriction];

DECLARE @var8 sysname;
SELECT @var8 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[INCIs]') AND [c].[name] = N'ToxicologicalProfile');
IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [INCIs] DROP CONSTRAINT [' + @var8 + '];');
ALTER TABLE [INCIs] DROP COLUMN [ToxicologicalProfile];

DELETE FROM [__EFMigrationsHistory]
WHERE [MigrationId] = N'20261020132411_AddInciCatalogFields';

COMMIT;
GO

