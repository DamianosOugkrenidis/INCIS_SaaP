# Runbook — Production Backup, Restore Rehearsal & Rollback (για εξουσιοδοτημένο DBA)

Script: `Database/Scripts/06_production_backup_and_rehearsal.sql` (μη-destructive για την παραγωγή· restore σε ξεχωριστή βάση).

## 0. Προϋποθέσεις & δικαιώματα
- Λογαριασμός με: `db_backupoperator`/`sysadmin` στην παραγωγή (για `BACKUP`), δικαιώματα `CREATE DATABASE` στον instance (για rehearsal), πρόσβαση σε **backup path** και **data/log path**.
- **Έλεγχος χώρου:** το backup απαιτεί ≈ μέγεθος DB + 10–20%· ο rehearsal απαιτεί επιπλέον ≈ μέγεθος DB×2 (mdf+ldf).
  ```sql
  EXEC sp_spaceused; -- στην INCIS_SaaP
  EXEC xp_fixeddrives; -- ελεύθερος χώρος δίσκων
  ```
- **Παράθυρο:** το `BACKUP` είναι online, αλλά απαιτείται παράθυρο για τον rehearsal.
- **Επιβεβαίωση έκδοσης:** `SELECT @@VERSION;` και `SELECT product_version FROM sys.dm_exec_...`· το backup πρέπει να αντιστοιχεί στην ΠΡΑΓΜΑΤΙΚΗ παραγωγική βάση/έκδοση.

## 1. BACKUP (CHECKSUM)
```sql
BACKUP DATABASE [INCIS_SaaP]
  TO DISK = N'<path>\INCIS_SaaP_PRE_ALLINCIS_<ts>.bak'
  WITH INIT, CHECKSUM, STATS = 10;
```
> **ΕΥΡΗΜΑ (επαληθεύτηκε τοπικά):** `WITH COMPRESSION` **αποτυγχάνει σε SQL Server Express** (Msg 1844). Το script χρησιμοποιεί πλέον χωρίς compression. Αν η παραγωγή είναι Express, **απόφυγε** το compression.
**Αναμενόμενο:** `BACKUP DATABASE successfully processed … (CHECKSUM enabled)`.

## 2. RESTORE VERIFYONLY (CHECKSUM)
```sql
RESTORE VERIFYONLY FROM DISK = N'<...>.bak' WITH CHECKSUM;
```
**Αναμενόμενο:** `The backup set on file 1 is valid.`

## 3. Rehearsal restore σε ΞΕΧΩΡΙΣΤΗ βάση
```sql
RESTORE FILELISTONLY FROM DISK = N'<...>.bak';   -- λήψη logical names
RESTORE DATABASE [INCIS_SaaP_rehearsal] FROM DISK = N'<...>.bak'
  WITH MOVE N'INCIS_SaaP'     TO N'<datapath>\INCIS_SaaP_rehearsal.mdf',
       MOVE N'INCIS_SaaP_log' TO N'<datapath>\INCIS_SaaP_rehearsal_log.ldf',
       RECOVERY, STATS = 10;
```
⚠️ **Ποτέ** `REPLACE` στην `INCIS_SaaP`. Ο rehearsal πάντα σε `INCIS_SaaP_rehearsal` (ή άλλο μοναδικό όνομα).

## 4. Έλεγχος ακεραιότητας & μετρητών (στη rehearsal)
- Από το script: μετρητές & fingerprints για CosIngIngredients, CosIngEntries, INCIs, RawMaterials, Formulas, Clients, Suppliers, AspNetUsers, AspNetRoles.
- `DBCC CHECKDB ([INCIS_SaaP_rehearsal]) WITH NO_INFOMSGS;` → **αναμενόμενο:** `CHECKDB found 0 allocation errors and 0 consistency errors`.

## 5. Έλεγχος συμβατότητας με την υποψήφια έκδοση
```powershell
# Στη ΔΟΚΙΜΑΣΤΙΚΗ βάση (rehearsal) με την υποψήφια έκδοση κώδικα:
$env:INCIS_DB_CONNECTION_STRING='Server=.\<instance>;Database=INCIS_SaaP_rehearsal;Trusted_Connection=True;TrustServerCertificate=True;'
dotnet ef database update --project INCIS_SaaP --startup-project INCIS_SaaP
```
**Αναμενόμενο:** εφαρμογή των 3 νέων migrations χωρίς σφάλμα· μετρητές αμετάβλητοι.

## 6. Rollback rehearsal (σε rehearsal, ΟΧΙ παραγωγή)
- **ΠΡΟΤΙΜΩΜΕΝΟ (πλήρης επαναφορά): restore-based rollback** → επαναφορά από το backup (§3) στη rehearsal.
- Migration down: `dotnet ef database update 20260702214036_AddFormulaApprovalWorkflow` (στη rehearsal).
  > **ΕΝΗΜΕΡΩΣΗ:** το `Down` του `EnlargeInciCatalogFields` **διορθώθηκε ώστε να είναι μη-καταστροφικό** (δεν συρρικνώνει `Name/EC/CAS`) — **επαληθεύτηκε** με δεδομένα `Name` 200/201/500 (`MigrationRollbackTests`, SQL Server). Δεν προκαλεί πλέον SQL 2628/truncation.
  > Το migration-down **αφαιρεί τα additive πεδία** (`FullName`, `Origin`, …) — για πλήρη διατήρηση όλων των δεδομένων χρησιμοποίησε **restore**.
- Πλήρες: `RESTORE DATABASE [INCIS_SaaP_rehearsal] ... WITH REPLACE, RECOVERY;` + επανέλεγχος μετρητών.

## 7. Αποδεικτικά που πρέπει να επιστραφούν
1. `BACKUP` output (path, μέγεθος, timestamp, SHA256 του .bak).
2. `RESTORE VERIFYONLY` → valid.
3. FILELISTONLY (logical names).
4. Rehearsal restore output + μετρητές/ fingerprints (πίνακας).
5. `DBCC CHECKDB` output.
6. `dotnet ef database update` output (στη rehearsal).
7. Αποτέλεσμα rollback rehearsal.
8. Χρόνοι εκτέλεσης + ελεύθερος χώρος πριν/μετά.

## 8. Καθαρισμός
`DROP DATABASE [INCIS_SaaP_rehearsal];` (μόνο rehearsal· διατήρησε το backup).
