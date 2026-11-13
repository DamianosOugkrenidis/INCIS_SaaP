# Αίτημα προς DBA — Production Backup / Restore Rehearsal / Rollback (E4–E6)

**Προθεσμία (προτεινόμενη): D+3** (D0=αποστολή· προς επιβεβαίωση πραγματικής ημερομηνίας).
**Κριτήρια αποδοχής:** τα 11 αποδεικτικά του §2 με VERIFYONLY valid, CHECKDB 0 errors, μετρητές/fingerprints ταυτόσημα, restore-based rollback επιτυχές.

**Απαιτείται:** εκτέλεση από εξουσιοδοτημένο DBA με πρόσβαση στην παραγωγή. **Καμία αλλαγή στην παραγωγική βάση** πέραν του backup (το restore γίνεται σε **ξεχωριστή** βάση).

## 0. Προϋποθέσεις & HARD SAFETY GUARDS
- Δικαιώματα: `BACKUP` στην `INCIS_SaaP`, `CREATE DATABASE` στον instance, πρόσβαση σε **εγκεκριμένο** backup path και data/log paths.
- Χώρος: ≈ μέγεθος DB (+20%) για το .bak· ≈ 2× μέγεθος DB για το rehearsal. Έλεγχος: `EXEC sp_spaceused; EXEC xp_fixeddrives;`.
- Έκδοση: `SELECT @@VERSION;`. **Express:** `WITH COMPRESSION` **αποτυγχάνει** (Msg 1844) → το script είναι χωρίς compression.
- **GUARDS στο script (πρέπει να περάσουν):**
  - `DB_ID('INCIS_SaaP') IS NOT NULL`
  - `INCIS_SaaP_rehearsal <> INCIS_SaaP` (απολύτως)
  - rehearsal name περιέχει `rehearsal`
  - **Καμία εγγραφή στην παραγωγή** πέραν του `BACKUP`.
- **DROP DATABASE:** το script έχει guarded block που **αρνείται** να κάνει DROP αν το target = production ή δεν είναι ρητά rehearsal· το `EXEC` είναι σχολιασμένο (ενεργοποίηση μόνο μετά από έλεγχο ταυτότητας).

## 1. Εκτέλεση
Ακολουθήστε `Database/Scripts/06_production_backup_and_rehearsal.sql` και `docs/DBA_BACKUP_REHEARSAL_RUNBOOK.md`:
1. `BACKUP DATABASE [INCIS_SaaP] … WITH INIT, CHECKSUM, STATS=10` (χωρίς COMPRESSION σε Express).
2. `RESTORE VERIFYONLY … WITH CHECKSUM`.
3. `RESTORE FILELISTONLY`.
4. `RESTORE DATABASE [INCIS_SaaP_rehearsal] … WITH MOVE …, RECOVERY`.
5. Μετρητές + fingerprints (CosIngIngredients, CosIngEntries, INCIs, RawMaterials, Formulas, Clients, Suppliers, AspNetUsers, AspNetRoles).
6. `DBCC CHECKDB ([INCIS_SaaP_rehearsal]) WITH NO_INFOMSGS`.
7. Εφαρμογή migrations στην rehearsal (`dotnet ef database update`, target = υποψήφια έκδοση).
8. **Restore-based rollback rehearsal** (προτιμώμενο): restore από το .bak στη rehearsal + επανέλεγχος μετρητών.
   - Migration-down: πλέον **μη-καταστροφικό** (δεν συρρικνώνει Name/CAS/EC)· χάνει τα additive πεδία (FullName κ.λπ.) — γι' αυτό προτιμάται το restore.
9. Καθαρισμός: `DROP DATABASE [INCIS_SaaP_rehearsal]` (μόνο rehearsal).

## 2. Αποδεικτικά που πρέπει να επιστραφούν (πίνακας)
| # | Αποδεικτικό | Αναμενόμενο |
|---|---|---|
| 1 | Backup output + path + μέγεθος | `successfully processed …` |
| 2 | SHA256 του .bak | τιμή hash |
| 3 | `RESTORE VERIFYONLY` | **“The backup set on file 1 is valid.”** |
| 4 | `RESTORE FILELISTONLY` | logical names (INCIS_SaaP / INCIS_SaaP_log) |
| 5 | Rehearsal restore output | `successfully processed …` |
| 6 | Μετρητές (9 πίνακες) | αναμενόμενα πλήθη (π.χ. INCIs = τρέχον, CosIng 24.094/2.353 αν αμετάβλητα) |
| 7 | Fingerprints (9 πίνακες) | σταθερά πριν/μετά |
| 8 | `DBCC CHECKDB` | **0 allocation/consistency errors** |
| 9 | Migrations στην rehearsal | εφαρμογή χωρίς σφάλμα· μετρητές αμετάβλητοι |
| 10 | Restore-based rollback | επιτυχής επαναφορά + μετρητές ταυτόσημοι |
| 11 | Χρόνοι/χώρος | πριν/μετά |

## 3. Κριτήρια επιτυχίας (Go για E4–E6)
- VERIFYONLY valid + CHECKDB 0 errors + μετρητές/fingerprints ταυτόσημα + rollback restore OK.
- Οποιαδήποτε απόκλιση → **No-Go** και διερεύνηση πριν την εγκατάσταση.

> **Δεν δηλώνεται ολοκληρωμένο** μέχρι να επιστραφούν τα παραπάνω πραγματικά outputs.
