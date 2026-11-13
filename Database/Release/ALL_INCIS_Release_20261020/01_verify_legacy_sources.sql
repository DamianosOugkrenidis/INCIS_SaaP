/* =====================================================================
   01_verify_legacy_sources.sql
   ---------------------------------------------------------------------
   Σκοπός : Read-only αποτύπωση της ΠΡΟΕΛΕΥΣΗΣ των δεδομένων.
            ΔΕΝ τροποποιεί κανένα δεδομένο.
   Χρήση  : sqlcmd -S <server> -d INCIS_SaaP -E -i 01_verify_legacy_sources.sql
   ---------------------------------------------------------------------
   Συμπέρασμα ελέγχου (βλ. αναφορά):
     * Η βάση περιέχει μόνο δεδομένα CosIng (CSV/JSON) και sample/demo γραμμές.
     * Δεν υπάρχει καμία εγγραφή προερχόμενη από τα XLS dumps
       (FILES\*.xls, root\*.xls, wwwroot\*.xls, παλαιά INCIS-*.xls).
     * Ως εκ τούτου δεν υπάρχουν δεδομένα προς εκκαθάριση από αυτές τις πηγές.
   ===================================================================== */
SET NOCOUNT ON;

PRINT '=== 1) Προέλευση CosIng (CSV/JSON) ===';
SELECT 'CosIngIngredients' AS TableName, ISNULL(DataSource, '(null)') AS DataSource, COUNT(*) AS Cnt
FROM CosIngIngredients GROUP BY DataSource
UNION ALL
SELECT 'CosIngEntries', ISNULL(DataSource, '(null)'), COUNT(*)
FROM CosIngEntries GROUP BY DataSource;

PRINT '=== 2) INCIs: προέλευση (αναμένεται ALL_INCIS_NEW_FILE μετά την εισαγωγή) ===';
SELECT ISNULL(DataSource, '(null)') AS DataSource, COUNT(*) AS Cnt
FROM INCIs GROUP BY DataSource;

PRINT '=== 3) Γραμμές που θα μπορούσαν να προέρχονται από XLS dumps (αναμένεται 0) ===';
SELECT
    (SELECT COUNT(*) FROM INCIs         WHERE DataSource IN ('XLS_DUMP','FILES_XLS','INCIS_20251218','MASTER_FORMULA_XLS')) AS IncisFromXls,
    (SELECT COUNT(*) FROM CosIngIngredients WHERE DataSource IN ('XLS_DUMP','FILES_XLS','INCIS_20251218','MASTER_FORMULA_XLS')) AS CosIngIngFromXls;

PRINT '=== 4) Μέγεθος βασικών πινάκων (για σύγκριση πριν/μετά) ===';
SELECT 'INCIs' AS TableName, COUNT(*) AS [Rows] FROM INCIs
UNION ALL SELECT 'RawMaterials', COUNT(*) FROM RawMaterials
UNION ALL SELECT 'Formulas',     COUNT(*) FROM Formulas
UNION ALL SELECT 'Clients',      COUNT(*) FROM Clients
UNION ALL SELECT 'Suppliers',    COUNT(*) FROM Suppliers
UNION ALL SELECT 'CosIngIngredients', COUNT(*) FROM CosIngIngredients
UNION ALL SELECT 'CosIngEntries',     COUNT(*) FROM CosIngEntries;
