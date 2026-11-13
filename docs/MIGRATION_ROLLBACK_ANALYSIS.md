# Migration-DOWN — Τεχνική Ανάλυση & Απόφαση

Ημερομηνία: 2026-10-20 · Απόδειξη: `MigrationRollbackTests` (SQL Server, πέρασε).

## 1. Ακριβές αίτιο
- **Migration:** `20261020133742_EnlargeInciCatalogFields`
- **Στήλες που συρρίκνωνε το `Down`:** `INCIs.Name` 500→200, `INCIs.ECNumber` 200→100, `INCIs.CASNumber` 200→100.
- **Δεδομένα:** το `ALL_INCIS_NEW_FILE` παράγει `Name` έως 500 χαρ. (το πλήρες έως 1903 αποθηκεύεται στο `FullName`).
- **Σφάλμα:** SQL **2628** — `String or binary data would be truncated` κατά το `ALTER COLUMN Name nvarchar(200)`.
- **Συνέπεια:** το EF εκτελεί τη μετάβαση σε transaction → **αυτόματο rollback, καμία μερική κατάσταση** (επαληθεύτηκε: `InciCols=15`, migrations=16).

## 2. Επιλογές & αξιολόγηση
| Επιλογή | Απώλεια δεδομένων | Ασφάλεια | Απόφαση |
|---|---|---|---|
| A. Truncate πριν το down | **ΝΑΙ** (κόβει ονόματα) | — | **Απορρίπτεται** (απαγορεύεται) |
| B. Διαγραφή γραμμών >200 | **ΝΑΙ** | — | **Απορρίπτεται** |
| C. **Μη-καταστροφικό Down (δεν συρρικνώνει)** | **ΟΧΙ** (διατηρεί ευρείες στήλες) | Ασφαλές· συμβατό | **ΕΠΙΛΕΧΘΗΚΕ** |
| D. Μόνο restore-based rollback | ΟΧΙ (πλήρης επαναφορά) | Ασφαλές | **Προτιμώμενο για πλήρη rollback** |

## 3. Υλοποίηση (αρχείο)
`INCIS_SaaP/Migrations/20261020133742_EnlargeInciCatalogFields.cs` — το `Down` είναι πλέον **no-op** (με εκτενή σχόλια): δεν συρρικνώνει `Name/EC/CAS`, άρα **δεν προκαλεί truncation**. Οι πρόσθετες στήλες (`FullName`, `Origin`, …) αφαιρούνται από τα αντίστοιχα `Down` των άλλων migrations (feature rollback).

**Επιπτώσεις:** μετά το down, οι στήλες παραμένουν ευρύτερες (500/200) — **μη-καταστροφικό, μη-σπαστικό**· το ιστορικό migrations επιστρέφει σε 13. Δεν αλλάζει καμία παραγωγική συμπεριφορά (το migration είναι ήδη εφαρμοσμένο).

## 4. Tests (πραγματικά, SQL Server)
`INCIS_SaaP.Tests/Integration/MigrationRollbackTests.cs`:
- Seed: `Name` μήκη **200 (boundary), 201, 500**· `FullName` 1903.
- `migrator.MigrateAsync("20260702214036_AddFormulaApprovalWorkflow")` (DOWN) → **επιτυχία χωρίς 2628**.
- Μετά: `COUNT=3`, `MAX(LEN(Name))=500` (καμία περικοπή).
- migrations = 13 → UP → 16, δεδομένα ακέραια.
- **Αποτέλεσμα: 1/1 PASS.**

## 5. Απόφαση/Κανόνας
- **Migration-DOWN:** πλέον **ασφαλές** για το συγκεκριμένο migration (χωρίς απώλεια `Name`).
- **Πλήρες rollback με διατήρηση όλων των νέων πεδίων (FullName κ.λπ.):** **restore από επαληθευμένο backup** (προτιμώμενο· επαληθεύτηκε τοπικά).
- Ο DBA runbook ενημερώθηκε: restore-based rollback ως προτιμώμενο· migration-down πλέον ασφαλές αλλά χάνει τα additive πεδία (feature rollback).

---

## 6. Πλήρης χαρακτηρισμός DOWN (deep test)
`MigrationRollbackTests.Down_RemovesAdditiveFields_ButPreservesSurvivingData` (PASS):
| Κατηγορία | Πεδία | Μετά το DOWN |
|---|---|---|
| **Surviving (αμετάβλητα)** | `Name`, `CASNumber`, `ECNumber` | **Fingerprint ταυτόσημο** (καμία περικοπή/αλλαγή) |
| **Additive (αφαιρούνται)** | `FullName`, `Origin`, `Restriction`, `ToxicologicalProfile`, `DataSource`, `ImportedAt` | Η στήλη **αφαιρείται** (τα δεδομένα δεν είναι πλέον διαθέσιμα) |
| Μετά το UP | Επιστρέφουν οι στήλες | Surviving δεδομένα ταυτόσημα· additive πεδία = **NULL** |

**Συμπέρασμα:** migration rollback = **feature rollback** (χάνει τα additive πεδία)· **πλήρες rollback με διατήρηση όλων των δεδομένων = restore-based**.
**Συμβατότητα με προηγούμενες εκδόσεις:** μετά το DOWN το schema ταιριάζει με το προηγούμενο revision (13 migrations)· οι ευρύτερες στήλες (500/200) είναι συμβατές και δεν επηρεάζουν παλαιότερο κώδικα.
