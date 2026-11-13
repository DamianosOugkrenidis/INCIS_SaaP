# ALL_INCIS_NEW_FILE — Πλάνο Deployment & Rollback

Ενότητα: ενοποίηση καταλόγου INCI με μοναδική πηγή το `ALL_INCIS_NEW_FILE.xls`.

## 1. Σύνοψη

- **Πηγή καταλόγου INCI:** `ALL_INCIS_NEW_FILE.xls` (φύλλο `printing`, 19.773 γραμμές → 19.724 μοναδικά ονόματα).
- **CosIng (CSV/JSON):** **διατηρείται** (Annex II–VI κ.λπ. δεν καλύπτονται από το νέο αρχείο και χρησιμοποιούνται από ενεργές λειτουργίες).
- **XLS dumps:** δεν υπάρχουν δεδομένα τους στη βάση (επιβεβαιώθηκε) — δεν γίνεται καμία διαγραφή.
- **Καμία αλλαγή στην παραγωγή** μέχρι έγγραφη έγκριση.

## 2. Παραδοτέα αρχεία

**Κώδικας**
- `INCIS_SaaP/Models/INCI.cs` — νέα πεδία: `Origin`, `Restriction`, `ToxicologicalProfile`, `FullName`, `DataSource`, `ImportedAt`· `Name`→500, `CAS/EC`→200.
- `INCIS_SaaP/Services/InciCatalogImportService.cs` — importer (lossless, idempotent upsert).
- `INCIS_SaaP/Dtos/InciCatalogImportResult.cs`
- `INCIS_SaaP/Controllers/ImportController.cs` — `POST /Import/InciCatalog`, `POST /Import/SeedInciCatalog`
- `INCIS_SaaP/Controllers/INCIsController.cs` — αναζήτηση και στο `FullName`
- `INCIS_SaaP/Data/INCISDbContext.cs`, `INCIS_SaaP/Program.cs` (DI)

**Migrations (EF Core)**
- `20261020132411_AddInciCatalogFields`
- `20261020133742_EnlargeInciCatalogFields`
- `20261020141235_AddInciFullName`

**Tests**
- `INCIS_SaaP.Tests/Services/InciCatalogImportServiceTests.cs` (9 tests)
- `INCIS_SaaP.Tests/Controllers/INCIsControllerTests.cs` (search στο FullName)

**SQL scripts** (`Database/Scripts/`)
- `01_verify_legacy_sources.sql` (read-only προέλευση)
- `02_import_all_incis.sql` (SQL-only εναλλακτική: BULK INSERT + MERGE, dry-run default)
- `03_post_import_checks.sql` (έλεγχοι μετά την εισαγωγή)

## 3. Προϋποθέσεις (πριν από οτιδήποτε)

1. **Πλήρες backup παραγωγής** (full backup + verify):
   ```sql
   BACKUP DATABASE [INCIS_SaaP]
     TO DISK = N'...\INCIS_SaaP_PRE_ALLINCIS_yyyymmdd_hhmm.bak'
     WITH INIT, COMPRESSION, CHECKSUM, STATS = 10;
   RESTORE VERIFYONLY FROM DISK = N'...\INCIS_SaaP_PRE_ALLINCIS_yyyymmdd_hhmm.bak';
   ```
2. Καταγραφή μεγεθών πριν (`03_post_import_checks.sql`, ενότητα E).
3. Παράθυρο συντήρησης & έγγραφη έγκριση.

## 4. Deployment (παραγωγή)

1. **Backup** (βήμα 3.1) και αποθήκευση εκτός server.
2. **Deploy κώδικα** στην έκδοση που περιέχει τα 3 migrations.
3. **Εφαρμογή migrations** (γίνεται αυτόματα στο startup μέσω `MigrateAsync`, ή χειροκίνητα):
   ```
   dotnet ef database update --project INCIS_SaaP --startup-project INCIS_SaaP
   ```
   *Μόνο schema (add columns / widen)· δεν διαγράφει δεδομένα.*
4. **Εισαγωγή δεδομένων** (μία από τις δύο):
   - Endpoint: `POST /Import/SeedInciCatalog` (admin/superuser), ή `POST /Import/InciCatalog` με upload του `.xls`.
   - CLI: το `InciCatalogImportService.ImportFromFileAsync(path)`.
   Αναμενόμενο αποτέλεσμα 1ης εκτέλεσης: `Inserted≈19724`, `DuplicateRows=49`, `ErrorRows=0`.
   Επανάληψη: `Inserted=0`, `Updated≈19724` (idempotent).
5. **Post-checks:** `Database/Scripts/03_post_import_checks.sql`.
   - `DuplicateNameCount = 0`
   - `FromAllIncis = TotalRows`
   - Ενότητα E ανέπαφη (CosIngIngredients=24094, CosIngEntries=2353, κ.λπ.)
   - Ενότητα F: `RowsWithShortenedKey = 7` (Name≤500, αλλά `FullName` πλήρες, χωρίς απώλεια).

## 5. Rollback

**Επίπεδο A — μόνο δεδομένα (χωρίς αλλαγή schema):**
```sql
DELETE FROM INCIs WHERE DataSource = 'ALL_INCIS_NEW_FILE';
```
(Εφόσον πριν η `INCIs` ήταν κενή/λίγες εγγραφές. Για πλήρη επαναφορά σε προηγούμενη κατάσταση: restore.)

**Επίπεδο B — πλήρες restore (προτεινόμενο):**
```sql
ALTER DATABASE [INCIS_SaaP] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
RESTORE DATABASE [INCIS_SaaP]
  FROM DISK = N'...\INCIS_SaaP_PRE_ALLINCIS_yyyymmdd_hhmm.bak'
  WITH REPLACE, RECOVERY;
ALTER DATABASE [INCIS_SaaP] SET MULTI_USER;
```

**Επίπεδο C — rollback migrations (αν χρειάζεται υποβάθμιση schema):**
```
dotnet ef database update EnlargeInciCatalogFields  # αφαιρεί το AddInciFullName
# περαιτέρω: ... 20260702214036_AddFormulaApprovalWorkflow
```
> Προσοχή: το `Down` του `EnlargeInciCatalogFields` μικραίνει ξανά τα πεδία· εκτελέστε το μόνο αφού διαγραφούν τυχόν τιμές που υπερβαίνουν τα αρχικά μεγέθη.

## 6. Αποτελέσματα δοκιμών

- Πλήρες test suite: **560/560 passed**.
- Import στο `INCIS_SaaP_inspect` (restored backup):
  - 1η εκτέλεση: 19.724 inserted, 49 duplicates, 7 shortened keys, 0 errors.
  - 2η εκτέλεση: 0 inserted, 19.724 updated (idempotent).
  - Ακεραιότητα: 19.724 rows / 19.724 distinct FullName / 0 duplicate names.
  - Οι 7 εγγραφές με `FullName>500`: πλήρη (584, 840, 1383, 1435, 1886, 1894, 1903 χαρ.) — **καμία απώλεια**.
  - CosIng & λοιποί πίνακες: αμετάβλητοι.

## 7. Τι ΔΕΝ αλλάζει

- CosIng CSV/JSON και οι λειτουργίες τους.
- Sample/demo & παραγωγικά δεδομένα (Clients, Suppliers, RawMaterials, Formulas).
- Τα `.xls` έγγραφα/εξαγωγές (`FILES/*.xls`, root/wwwroot `.xls`) παραμένουν ως έχουν.
- Η δοκιμαστική βάση `INCIS_SaaP_inspect` διατηρείται μέχρι την τελική επαλήθευση.

## 8. Εκκρεμότητες πριν την έγκριση

- Επιβεβαίωση από τον υπεύθυνο ότι η `INCIs` είναι ο επιθυμητός προορισμός του καταλόγου.
- Έγκριση προγραμματισμένου παραθύρου & backup.
