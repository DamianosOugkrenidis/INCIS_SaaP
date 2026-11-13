/* =====================================================================
   04_post_import_checks.sql
   ---------------------------------------------------------------------
   Σκοπός : Έλεγχοι ακεραιότητας μετά την εισαγωγή από ALL_INCIS_NEW_FILE.
            Read-only.
   ===================================================================== */
SET NOCOUNT ON;

PRINT '=== A) Πληρότητα/διπλοεγγραφές INCIs ===';
SELECT
    COUNT(*)                                              AS TotalRows,
    COUNT(DISTINCT Name)                                  AS DistinctNames,
    SUM(CASE WHEN DataSource = 'ALL_INCIS_NEW_FILE' THEN 1 ELSE 0 END) AS FromAllIncis,
    COUNT(*) - COUNT(DISTINCT Name)                       AS DuplicateNameCount
FROM INCIs;

PRINT '=== B) Κάλυψη πεδίων ===';
SELECT
    SUM(CASE WHEN CASNumber IS NOT NULL THEN 1 ELSE 0 END)            AS WithCas,
    SUM(CASE WHEN ECNumber IS NOT NULL THEN 1 ELSE 0 END)             AS WithEc,
    SUM(CASE WHEN [Function] IS NOT NULL THEN 1 ELSE 0 END)           AS WithFunction,
    SUM(CASE WHEN Restriction IS NOT NULL THEN 1 ELSE 0 END)          AS WithRestriction,
    SUM(CASE WHEN ToxicologicalProfile IS NOT NULL THEN 1 ELSE 0 END) AS WithTox,
    SUM(CASE WHEN Origin IS NOT NULL THEN 1 ELSE 0 END)               AS WithOrigin
FROM INCIs;

PRINT '=== C) Δείγμα εγγραφής (AQUA) ===';
SELECT Id, Name, CASNumber, ECNumber, [Function], Restriction, Origin, DataSource
FROM INCIs WHERE Name = 'AQUA';

PRINT '=== D) Λειτουργικές δοκιμές (αναζήτηση) ===';
SELECT
    (SELECT COUNT(*) FROM INCIs WHERE Name LIKE '%ALOE%')             AS AloeNameMatches,
    (SELECT COUNT(*) FROM INCIs WHERE CASNumber LIKE '%7732-18-5%')   AS AquaCasMatches;

PRINT '=== E) Ανεξαρτησία άλλων υποσυστημάτων (πρέπει ανέπαφα) ===';
SELECT
    (SELECT COUNT(*) FROM CosIngIngredients) AS CosIngIngredients,
    (SELECT COUNT(*) FROM CosIngEntries)     AS CosIngEntries,
    (SELECT COUNT(*) FROM Formulas)          AS Formulas,
    (SELECT COUNT(*) FROM RawMaterials)      AS RawMaterials,
    (SELECT COUNT(*) FROM Clients)           AS Clients,
    (SELECT COUNT(*) FROM Suppliers)         AS Suppliers;

PRINT '=== F) Απώλεια ονομάτων (truncation): Name<>FullName ===';
SELECT
    SUM(CASE WHEN Name <> FullName THEN 1 ELSE 0 END) AS RowsWithShortenedKey,
    MAX(LEN(FullName))              AS MaxFullNameLength,
    COUNT(DISTINCT FullName)        AS DistinctFullNames
FROM INCIs;
