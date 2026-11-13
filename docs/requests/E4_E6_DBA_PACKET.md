# E4–E6 — DBA Packet: Production Backup / Restore Rehearsal / Rollback

Κατάσταση: **ΕΤΟΙΜΟ ΠΡΟΣ ΕΚΤΕΛΕΣΗ (ανεξάρτητο — δεν απαιτεί επιπλέον προετοιμασία).**
**Υπεύθυνος:** DBA (εξουσιοδοτημένος, με πρόσβαση στην παραγωγή).
**Πρόσβαση που απαιτείται:** `BACKUP` στην `INCIS_SaaP`, `CREATE DATABASE` στον instance, εγκεκριμένο backup path, data/log paths· χώρος ≈ DB×1,2 (.bak) + ≈ DB×2 (rehearsal).

> **Καμία εγγραφή στην παραγωγή πέραν του BACKUP.** Restore/migrations/rollback **μόνο** σε rehearsal DB. Καμία διαγραφή production.

---

## 1. Παραδοτέο (11 αποδεικτικά — `DBA_REQUEST.md` §2)
.bak + SHA256 · VERIFYONLY valid · FILELISTONLY · rehearsal restore output · μετρητές (9 πίνακες) · fingerprints (9 πίνακες) · `DBCC CHECKDB` 0 errors · migrations στην rehearsal · restore-based rollback + επανέλεγχος μετρητών · χρόνοι · χώρος.

## 2. HARD SAFETY GUARDS (πρέπει να περάσουν πριν οποιαδήποτε ενέργεια)
- `DB_ID('INCIS_SaaP') IS NOT NULL`
- `INCIS_SaaP_rehearsal <> INCIS_SaaP` (name + `database_id`)· rehearsal name περιέχει `rehearsal`
- STEP 0 identity evidence: `name, database_id, state_desc, create_date, owner`
- `USE` guard (`DB_NAME`)
- `DROP DATABASE` **απενεργοποιημένο** (`ConfirmDrop=0`, `EXEC` σχολιασμένο) μέχρι έγκριση

## 3. Εκτέλεση
Script: `Database/Scripts/06_production_backup_and_rehearsal.sql` (runbook: `docs/DBA_BACKUP_REHEARSAL_RUNBOOK.md`).
1. `BACKUP DATABASE [INCIS_SaaP] … WITH INIT, CHECKSUM, STATS=10` (**χωρίς** COMPRESSION — Express Msg 1844) → SHA256 του .bak.
2. `RESTORE VERIFYONLY … WITH CHECKSUM`.
3. `RESTORE FILELISTONLY`.
4. `RESTORE DATABASE [INCIS_SaaP_rehearsal] … WITH MOVE …, RECOVERY`.
5. Μετρητές + fingerprints (9 πίνακες: CosIngIngredients, CosIngEntries, INCIs, RawMaterials, Formulas, Clients, Suppliers, AspNetUsers, AspNetRoles).
6. `DBCC CHECKDB ([INCIS_SaaP_rehearsal]) WITH NO_INFOMSGS`.
7. Migrations στην rehearsal (`dotnet ef database update`, target = υποψήφια έκδοση).
8. **Restore-based rollback rehearsal** + επανέλεγχος μετρητών (προτιμάται από migration-down, που είναι μη-καταστροφικό αλλά χάνει additive πεδία).
9. Καθαρισμός **μόνο** rehearsal (`DROP DATABASE [INCIS_SaaP_rehearsal]`, μετά από identity check).

## 4. Αναμενόμενα σημεία αναφοράς
- Μετρητές (σημείο σύγκρισης): **INCIs 19.724 · CosIngIngredients 24.094 · CosIngEntries 2.353** (ή τρέχουσες πραγματικές τιμές, ταυτόσημες πριν/μετά).
- `RESTORE VERIFYONLY` → “The backup set on file 1 is valid.”
- `DBCC CHECKDB` → **0 allocation/consistency errors**.

## 5. Κριτήρια αποδοχής (Go για E4–E6)
VERIFYONLY valid **και** CHECKDB 0 errors **και** μετρητές/fingerprints ταυτόσημα **και** rollback επιτυχές. Οποιαδήποτε απόκλιση → **No-Go** + διερεύνηση πριν την εγκατάσταση.

## 6. Ανάστροφη ενημέρωση
Τυχόν απόκλιση ή προειδοποίηση (π.χ. suspect page, χώρος, χρόνοι) επιστρέφεται **πριν** οποιασδήποτε παραγωγικής εγκατάστασης.

## 7. Παραπομπές
`docs/requests/DBA_REQUEST.md` · `Database/Scripts/06_production_backup_and_rehearsal.sql` · `Database/Scripts/07_scheduled_backup.sql` · `docs/DBA_BACKUP_REHEARSAL_RUNBOOK.md` · `docs/MIGRATION_ROLLBACK_ANALYSIS.md`.
