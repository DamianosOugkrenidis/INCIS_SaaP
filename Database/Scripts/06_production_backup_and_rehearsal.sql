/* =====================================================================
   06_production_backup_and_rehearsal.sql
   ---------------------------------------------------------------------
   Σκοπός : Πλήρης, επαληθευμένη διαδικασία backup της ΠΑΡΑΓΩΓΙΚΗΣ βάσης
            INCIS_SaaP με CHECKSUM + RESTORE VERIFYONLY + δοκιμαστική
            επαναφορά (rehearsal) σε ξεχωριστή βάση + έλεγχος ακεραιότητας
            + επιβεβαίωση rollback.
   Εκτέλεση: Από ΕΞΟΥΣΙΟΔΟΤΗΜΕΝΟ άτομο με πρόσβαση στην παραγωγή.
             ΔΕΝ αλλάζει την παραγωγή (μόνο read + backup).
   ---------------------------------------------------------------------
   ΟΔΗΓΙΕΣ: Εκτέλεσε τα βήματα ΞΕΧΩΡΙΣΤΑ και έλεγξε τα αποτελέσματα.
            Το reference backup (Database\INCIS_SaaP.bak) ΔΕΝ υποκαθιστά
            αυτή τη διαδικασία.
   ===================================================================== */
SET NOCOUNT ON;

/* ---------------------------------------------------------------------
   PREFLIGHT GUARDS (must pass before anything runs)
   - Production DB must exist on this instance.
   - Rehearsal DB name MUST differ from production and contain "rehearsal".
   - No statement in this script writes to INCIS_SaaP except BACKUP.
   --------------------------------------------------------------------- */
PRINT '=== PREFLIGHT: production/rehearsal separation ===';
IF DB_ID(N'INCIS_SaaP') IS NULL
    THROW 50001, N'Production database INCIS_SaaP was not found on this instance. Aborting.', 1;
IF N'INCIS_SaaP_rehearsal' = N'INCIS_SaaP'
    THROW 50002, N'SAFETY: rehearsal database must NOT be the production database. Aborting.', 1;
IF N'INCIS_SaaP_rehearsal' NOT LIKE N'%rehearsal%'
    THROW 50003, N'SAFETY: rehearsal database name must contain "rehearsal". Aborting.', 1;
PRINT 'Guard OK: production=INCIS_SaaP, rehearsal=INCIS_SaaP_rehearsal (no prod writes).';
PRINT 'Preconditions: approved backup path, enough free space, BACKUP/CREATE DATABASE rights.';

/* STEP 0 — IDENTITY VERIFICATION (paste output as evidence).
   Name alone is NOT sufficient: confirm database_id, state, create_date and owner. */
PRINT '=== STEP 0: database identity evidence ===';
SELECT name, database_id, state_desc, create_date, SUSER_SNAME(owner_sid) AS owner
FROM sys.databases
WHERE name IN (N'INCIS_SaaP', N'INCIS_SaaP_rehearsal')
ORDER BY name;

-- database_id must differ if rehearsal already exists
DECLARE @ProdId int = DB_ID(N'INCIS_SaaP');
DECLARE @RehId  int = DB_ID(N'INCIS_SaaP_rehearsal');
IF @RehId IS NOT NULL AND @ProdId IS NOT NULL AND @RehId = @ProdId
    THROW 50004, N'SAFETY: rehearsal and production resolve to the SAME database_id. Aborting.', 1;
PRINT 'Guard OK: database_id separation verified (or rehearsal not yet created).';

/* ---------------------------------------------------------------------
   ΒΗΜΑ 1 — BACKUP με CHECKSUM
   Όρισέ τα paths πριν την εκτέλεση.
   --------------------------------------------------------------------- */
DECLARE @BackupDir  nvarchar(400) = N'C:\Backups\INCIS';
DECLARE @Stamp      nvarchar(20)  = CONVERT(nvarchar(20), GETDATE(), 112) + N'_' + REPLACE(CONVERT(nvarchar(8), GETDATE(), 108), N':', N'');
DECLARE @BackupFile nvarchar(500) = @BackupDir + N'\INCIS_SaaP_PRE_ALLINCIS_' + @Stamp + N'.bak';

PRINT '=== ΒΗΜΑ 1: BACKUP (CHECKSUM) ===';
-- ΣΗΜΑΝΤΙΚΟ: Το WITH COMPRESSION ΔΕΝ υποστηρίζεται σε SQL Server **Express** (σφάλμα 1844).
-- Αν η παραγωγή είναι Express, αφαίρεσε το COMPRESSION. Εδώ χρησιμοποιείται χωρίς compression.
BACKUP DATABASE [INCIS_SaaP]
    TO DISK = @BackupFile
    WITH INIT, CHECKSUM, STATS = 10;
GO

/* ---------------------------------------------------------------------
   ΒΗΜΑ 2 — RESTORE VERIFYONLY (με CHECKSUM)
   --------------------------------------------------------------------- */
-- Αντικατέστησε το path με το πραγματικό backup file του ΒΗΜΑΤΟΣ 1.
DECLARE @BackupFile nvarchar(500) = N'C:\Backups\INCIS\INCIS_SaaP_PRE_ALLINCIS_<STAMP>.bak'; -- <<< UPDATE
PRINT '=== ΒΗΜΑ 2: RESTORE VERIFYONLY ===';
RESTORE VERIFYONLY FROM DISK = @BackupFile WITH CHECKSUM;
PRINT 'Αναμενόμενο: "The backup set on file 1 is valid."';
GO

/* ---------------------------------------------------------------------
   ΒΗΜΑ 3 — Δοκιμαστική επαναφορά (rehearsal) σε ΞΕΧΩΡΙΣΤΗ βάση
   --------------------------------------------------------------------- */
-- Λίστα logical files (για τα MOVE):
RESTORE FILELISTONLY FROM DISK = N'C:\Backups\INCIS\INCIS_SaaP_PRE_ALLINCIS_<STAMP>.bak'; -- <<< UPDATE
GO

-- Επαναφορά σε rehearsal DB (προσάρμοσε τα MOVE paths):
PRINT '=== GUARD: restore target identity ===';
IF DB_ID(N'INCIS_SaaP_rehearsal') IS NOT NULL
   AND DB_ID(N'INCIS_SaaP') IS NOT NULL
   AND DB_ID(N'INCIS_SaaP_rehearsal') = DB_ID(N'INCIS_SaaP')
    THROW 50004, N'SAFETY: restore target resolves to the production database_id. Aborting.', 1;
PRINT 'Guard OK: restore target is NOT the production database.';
RESTORE DATABASE [INCIS_SaaP_rehearsal]
    FROM DISK = N'C:\Backups\INCIS\INCIS_SaaP_PRE_ALLINCIS_<STAMP>.bak'          -- <<< UPDATE
    WITH MOVE N'INCIS_SaaP'     TO N'C:\Backups\INCIS\INCIS_SaaP_rehearsal.mdf',
         MOVE N'INCIS_SaaP_log' TO N'C:\Backups\INCIS\INCIS_SaaP_rehearsal_log.ldf',
         RECOVERY, STATS = 10;
GO

/* ---------------------------------------------------------------------
   ΒΗΜΑ 4 — Έλεγχος ακεραιότητας & κρίσιμων μετρητών (στη rehearsal)
   Συμπλήρωσε τα αναμενόμενα (πριν) πλήθη σε σχόλια.
   --------------------------------------------------------------------- */
USE [INCIS_SaaP_rehearsal];
GO
IF DB_NAME() = N'INCIS_SaaP'
    THROW 50005, N'SAFETY: connected to the PRODUCTION database. Aborting integrity checks.', 1;
IF DB_NAME() <> N'INCIS_SaaP_rehearsal'
    THROW 50006, N'SAFETY: connected to an unexpected database. Aborting.', 1;
PRINT 'Guard OK: connected to rehearsal database.';
GO
PRINT '=== ΒΗΜΑ 4: Μετρητές κρίσιμων πινάκων ===';
SELECT 'CosIngIngredients' AS TableName, COUNT(*) AS [Rows] FROM CosIngIngredients
UNION ALL SELECT 'CosIngEntries',     COUNT(*) FROM CosIngEntries
UNION ALL SELECT 'INCIs',             COUNT(*) FROM INCIs
UNION ALL SELECT 'RawMaterials',      COUNT(*) FROM RawMaterials
UNION ALL SELECT 'Formulas',          COUNT(*) FROM Formulas
UNION ALL SELECT 'Clients',           COUNT(*) FROM Clients
UNION ALL SELECT 'Suppliers',         COUNT(*) FROM Suppliers
UNION ALL SELECT 'AspNetUsers',       COUNT(*) FROM AspNetUsers
UNION ALL SELECT 'AspNetRoles',       COUNT(*) FROM AspNetRoles;

PRINT '=== ΒΗΜΑ 4β: Fingerprints (περιεχόμενο, χωρίς ευαίσθητα πεδία) ===';
SELECT 'CosIngIngredients' AS TableName, CONCAT(COUNT(*), '|', ISNULL(CHECKSUM_AGG(BINARY_CHECKSUM(*)),0)) AS Fingerprint FROM CosIngIngredients
UNION ALL SELECT 'CosIngEntries', CONCAT(COUNT(*), '|', ISNULL(CHECKSUM_AGG(BINARY_CHECKSUM(*)),0)) FROM CosIngEntries
UNION ALL SELECT 'INCIs',         CONCAT(COUNT(*), '|', ISNULL(CHECKSUM_AGG(BINARY_CHECKSUM(*)),0)) FROM INCIs
UNION ALL SELECT 'RawMaterials',  CONCAT(COUNT(*), '|', ISNULL(CHECKSUM_AGG(BINARY_CHECKSUM(*)),0)) FROM RawMaterials
UNION ALL SELECT 'Formulas',      CONCAT(COUNT(*), '|', ISNULL(CHECKSUM_AGG(BINARY_CHECKSUM(*)),0)) FROM Formulas
UNION ALL SELECT 'Clients',       CONCAT(COUNT(*), '|', ISNULL(CHECKSUM_AGG(BINARY_CHECKSUM(*)),0)) FROM Clients
UNION ALL SELECT 'Suppliers',     CONCAT(COUNT(*), '|', ISNULL(CHECKSUM_AGG(BINARY_CHECKSUM(*)),0)) FROM Suppliers
UNION ALL SELECT 'AspNetUsers',   CONCAT(COUNT(*), '|', ISNULL(CHECKSUM_AGG(BINARY_CHECKSUM(Id, UserName, Email, EmailConfirmed, AccessFailedCount)),0)) FROM AspNetUsers
UNION ALL SELECT 'AspNetRoles',   CONCAT(COUNT(*), '|', ISNULL(CHECKSUM_AGG(BINARY_CHECKSUM(Id, Name, NormalizedName)),0)) FROM AspNetRoles;
GO

PRINT '=== ΒΗΜΑ 4γ: DBCC CHECKDB στη rehearsal (προαιρετικό, χρονοβόρο) ===';
DBCC CHECKDB ([INCIS_SaaP_rehearsal]) WITH NO_INFOMSGS;
GO

/* ---------------------------------------------------------------------
   ΒΗΜΑ 5 — Επιβεβαίωση rollback για την υποψήφια έκδοση
   Στόχος: να αποδειχθεί ότι ΜΠΟΡΟΥΜΕ να επαναφέρουμε την παραγωγή.
   ΔΕΝ εκτελείται στην παραγωγή κατά τη διάρκεια των δοκιμών.
   ---------------------------------------------------------------------
   5α. Migration down rehearsal (σε rehearsal DB):
       dotnet ef database update 20260702214036_AddFormulaApprovalWorkflow
           --project INCIS_SaaP --startup-project INCIS_SaaP
       (με connection string -> INCIS_SaaP_rehearsal)
       ΕΝΗΜΕΡΩΣΗ: το Down του EnlargeInciCatalogFields είναι πλέον ΜΗ-ΚΑΤΑΣΤΡΟΦΙΚΟ
       (δεν συρρικνώνει Name/EC/CAS) — επαληθεύτηκε με MigrationRollbackTests.
       Το down αφαιρεί τα additive πεδία (FullName κ.λπ.)· για πλήρη διατήρηση -> restore (5β).
   5β. Πλήρες restore rehearsal (se ΔΕΙΓΜΑ, όχι prod):
       ALTER DATABASE [INCIS_SaaP_rehearsal] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
       RESTORE DATABASE [INCIS_SaaP_rehearsal] FROM DISK = <backup> WITH REPLACE, RECOVERY;
       ALTER DATABASE [INCIS_SaaP_rehearsal] SET MULTI_USER;
   5γ. Επανέλεγξε τους μετρητές/ fingerprints του ΒΗΜΑΤΟΣ 4.
   --------------------------------------------------------------------- */

PRINT '=== ΒΗΜΑ 6 — Καθαρισμός rehearsal (προαιρετικό) ===';
/* SAFETY: DROP μόνο της rehearsal DB, μετά από έλεγχο ταυτότητας.
   Το EXEC είναι σκόπιμα σχολιασμένο: ενεργοποιήστε το ΜΟΝΟ αφού επιβεβαιώσετε
   ότι @RehearsalDb είναι η προσωρινή βάση και ΟΧΙ η παραγωγή. */
DECLARE @RehearsalDb sysname = N'INCIS_SaaP_rehearsal';
DECLARE @ConfirmDrop bit = 0;  -- set to 1 ONLY after identity verification (STEP 0) and approval
IF @RehearsalDb = N'INCIS_SaaP'
    THROW 50002, N'SAFETY: refusing to DROP the production database. Aborting.', 1;
IF @RehearsalDb NOT LIKE N'%rehearsal%'
    THROW 50003, N'SAFETY: refusing to DROP a database whose name is not explicitly a rehearsal DB.', 1;
IF DB_ID(@RehearsalDb) IS NOT NULL AND DB_ID(@RehearsalDb) = DB_ID(N'INCIS_SaaP')
    THROW 50004, N'SAFETY: DROP target resolves to the production database_id. Aborting.', 1;

IF DB_ID(@RehearsalDb) IS NOT NULL
BEGIN
    PRINT 'Guard OK: DROP target verified as ' + @RehearsalDb + N' (not production).';
    IF @ConfirmDrop = 1
    BEGIN
        -- Ενεργοποίηση μετά την επιβεβαίωση ταυτότητας:
        -- DECLARE @sql nvarchar(max) =
        --   N'ALTER DATABASE ' + QUOTENAME(@RehearsalDb) + N' SET SINGLE_USER WITH ROLLBACK IMMEDIATE; ' +
        --   N'DROP DATABASE ' + QUOTENAME(@RehearsalDb) + N';';
        -- EXEC(@sql);
        PRINT 'DROP skipped: EXEC is intentionally commented (enable only with written approval).';
    END
    ELSE
        PRINT 'DROP disabled (ConfirmDrop=0). No destructive action performed.';
END
ELSE
    PRINT 'Rehearsal DB not present; nothing to drop.';
GO
