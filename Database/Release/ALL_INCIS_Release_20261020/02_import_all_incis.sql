/* =====================================================================
   03_import_all_incis.sql
   ---------------------------------------------------------------------
   Σκοπός : Εισαγωγή/ενημέρωση του καταλόγου INCI από το ALL_INCIS_NEW_FILE.
   ---------------------------------------------------------------------
   ΣΗΜΑΝΤΙΚΟ: Ο ΚΑΝΟΝΙΚΟΣ τρόπος εισαγωγής είναι ο C# importer
              (INCIS_SaaP/Services/InciCatalogImportService.cs) που διαβάζει
              απευθείας το .xls/.xlsx. Ενεργοποιείται μέσω:
                 POST /Import/InciCatalog            (upload .xls/.xlsx)
                 POST /Import/SeedInciCatalog        (από InciCatalog:SourcePath)
   Το παρόν script παρέχει SQL-only εναλλακτική (BULK INSERT + MERGE) για
   περιβάλλοντα χωρίς τη δυνατότητα εκτέλεσης του importer.
   ---------------------------------------------------------------------
   Προαπαιτούμενο: ένα κανονικοποιημένο αρχείο (tab-delimited) του φύλλου
   "printing" με στήλες: a/n, Name, Origin, Cas, Einecs, Tox. Profile,
   Restriction, Functions  (η γραμμή κεφαλίδας στην γραμμή 3, δεδομένα από 4).
   ===================================================================== */
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Execute bit = 0;                    -- <<< 1 για εκτέλεση
DECLARE @FilePath nvarchar(400) = N'C:\path\to\ALL_INCIS_NEW_FILE_export.txt';
DECLARE @RowTerminator nvarchar(10) = N'\r\n';
DECLARE @FieldTerminator nvarchar(10) = N'\t';

IF OBJECT_ID('tempdb..#Stage') IS NOT NULL DROP TABLE #Stage;
CREATE TABLE #Stage
(
    [a_n]         nvarchar(50)   NULL,
    [Name]        nvarchar(500)  NULL,
    [Origin]      nvarchar(200)  NULL,
    [Cas]         nvarchar(400)  NULL,
    [Einecs]      nvarchar(400)  NULL,
    [ToxProfile]  nvarchar(max)  NULL,
    [Restriction] nvarchar(500)  NULL,
    [Functions]   nvarchar(400)  NULL
);

PRINT '=== BULK INSERT στο staging ===';
BULK INSERT #Stage
FROM @FilePath
WITH (
    FIRSTROW = 4,                 -- παράκαμψη τίτλου/κενής/κεφαλίδας
    FIELDTERMINATOR = @FieldTerminator,
    ROWTERMINATOR = @RowTerminator,
    CODEPAGE = '65001'            -- UTF-8
);

DECLARE @Staged int = (SELECT COUNT(*) FROM #Stage);
PRINT CONCAT('Staged rows: ', @Staged);

IF (@Execute = 1)
BEGIN
    BEGIN TRAN;

    ;WITH Src AS (
        SELECT
            LTRIM(RTRIM(Name)) AS FullName,
            NULLIF(NULLIF(NULLIF(LTRIM(RTRIM(Origin)), N'None'), N'-'), N'')        AS Origin,
            NULLIF(NULLIF(NULLIF(LTRIM(RTRIM(Cas)),   N'0'), N'-'), N'None')        AS CASNumber,
            NULLIF(NULLIF(NULLIF(LTRIM(RTRIM(Einecs)),N'0'), N'-'), N'None')        AS ECNumber,
            NULLIF(LTRIM(RTRIM(ToxProfile)), N'')                                   AS ToxicologicalProfile,
            NULLIF(NULLIF(NULLIF(LTRIM(RTRIM(Restriction)), N'-'), N''), N'None')   AS Restriction,
            NULLIF(NULLIF(LTRIM(RTRIM([Functions])), N'-'), N'')                    AS [Function]
        FROM #Stage
        WHERE Name IS NOT NULL AND LTRIM(RTRIM(Name)) <> ''
    )
    MERGE INCIs AS tgt
    USING Src ON tgt.FullName = Src.FullName
                 OR (tgt.FullName IS NULL AND tgt.Name = LEFT(Src.FullName, 500))
    WHEN MATCHED THEN UPDATE SET
        tgt.Name               = LEFT(Src.FullName, 500),
        tgt.FullName           = Src.FullName,
        tgt.CASNumber          = LEFT(Src.CASNumber, 200),
        tgt.ECNumber           = LEFT(Src.ECNumber, 200),
        tgt.[Function]         = LEFT(Src.[Function], 200),
        tgt.Origin             = LEFT(Src.Origin, 200),
        tgt.Restriction        = LEFT(Src.Restriction, 500),
        tgt.ToxicologicalProfile = Src.ToxicologicalProfile,
        tgt.DataSource         = 'ALL_INCIS_NEW_FILE',
        tgt.ImportedAt         = SYSUTCDATETIME(),
        tgt.IsActive           = 1
    WHEN NOT MATCHED BY TARGET THEN INSERT
        (Name, FullName, CASNumber, ECNumber, [Function], Origin, Restriction, ToxicologicalProfile, DataSource, ImportedAt, CreatedAt, IsActive)
        VALUES
        (LEFT(Src.FullName,500), Src.FullName, LEFT(Src.CASNumber,200), LEFT(Src.ECNumber,200), LEFT(Src.[Function],200),
         LEFT(Src.Origin,200), LEFT(Src.Restriction,500), Src.ToxicologicalProfile, 'ALL_INCIS_NEW_FILE', SYSUTCDATETIME(), SYSDATETIME(), 1);

    PRINT CONCAT('MERGE ολοκληρώθηκε. INCIs συνολικά: ', (SELECT COUNT(*) FROM INCIs));
    COMMIT;
END
ELSE
BEGIN
    PRINT 'Dry-run (μόνο staging). Θέσε @Execute = 1 για πραγματικό MERGE.';
END

DROP TABLE #Stage;
