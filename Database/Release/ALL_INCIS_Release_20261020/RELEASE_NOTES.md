# RELEASE NOTES — ALL_INCIS_NEW_FILE (INCI Catalog Unification)

Έκδοση: `ALL_INCIS_Release_20261020`
Κατάσταση: **Έτοιμο προς έγκριση / εγκατάσταση** — καμία αλλαγή στην παραγωγή.

## Σκοπός
Ο κατάλογος INCI (`INCIs`) τροφοδοτείται αποκλειστικά από το `ALL_INCIS_NEW_FILE.xls`.
Τα δεδομένα CosIng (CSV/JSON) διατηρούνται (Annex II–VI δεν καλύπτεται από το νέο αρχείο).
Δεν διαγράφηκαν δεδομένα από XLS dumps (δεν υπήρχαν στη βάση).

## Περιεχόμενα release
| Αρχείο | Περιγραφή |
|---|---|
| `04_migrations_up.sql` | Idempotent script εφαρμογής των 3 νέων migrations (schema-only) |
| `05_migrations_down.sql` | Script rollback των 3 νέων migrations |
| `01_verify_legacy_sources.sql` | Read-only προέλευση δεδομένων |
| `02_import_all_incis.sql` | SQL-only εναλλακτική εισαγωγής (BULK INSERT + MERGE, dry-run default) |
| `03_post_import_checks.sql` | Έλεγχοι μετά την εισαγωγή |
| `DEPLOYMENT_AND_ROLLBACK.md` | Πλάνο deployment & rollback |
| `CHANGED_FILES.txt` | Αλλαγές κώδικα/migrations |

## Αλλαγή schema (3 migrations, με σειρά)
1. `20261020132411_AddInciCatalogFields` — προσθήκη `DataSource`, `ImportedAt`, `Origin`, `Restriction`, `ToxicologicalProfile`.
2. `20261020133742_EnlargeInciCatalogFields` — `Name`→500, `CASNumber`/`ECNumber`→200.
3. `20261020141235_AddInciFullName` — προσθήκη `FullName` (nvarchar(max)).

Όλα είναι **προσθετικά/διευρυντικά** (add/widen) — δεν διαγράφουν δεδομένα.

## Αποτελέσματα δοκιμών
- Πλήρες test suite: **563/563 passed**.
- clean-restore + migrations: μετρητές πριν=μετά (CosIngIngredients 24094, CosIngEntries 2353, Formulas 2, RawMaterials 1, Clients 2, Suppliers 1, INCIs 1, Users 1, Roles 4). Καμία απώλεια/αλλοίωση.
- Rollback test: `down` → 13 migrations & αρχικό schema, `up` → 16 migrations, δεδομένα ακέραια.
- Import (ALL_INCIS_NEW_FILE.xls): 19.773 → 19.724 inserted, 49 duplicates, 0 errors.
- Idempotency: 2η/3η εκτέλεση → 0 inserted, 19.724 updated.
- Lossless: 7 σύνθετα ονόματα (584–1903 χαρ.) πλήρη στο `FullName`· 0 απώλεια.
- Αναζήτηση: `Name` και `FullName` (βρίσκει και substring πέραν των 500 χαρ.).
- Backup: `RESTORE VERIFYONLY` → «The backup set on file 1 is valid».
- CosIng & λοιπά υποσυστήματα: αμετάβλητα.

## Οδηγίες εγκατάστασης (σύντομες)
1. **Backup παραγωγής** (full, WITH CHECKSUM) + `RESTORE VERIFYONLY`.
2. Deploy κώδικα (περιέχει τα 3 migrations).
3. `dotnet ef database update` (ή `04_migrations_up.sql`).
4. Εισαγωγή: `POST /Import/SeedInciCatalog` ή `POST /Import/InciCatalog` (Admin/SuperUser) ή CLI `ImportFromFileAsync`.
5. `03_post_import_checks.sql`.

## Επαναφορά (rollback)
- Δεδομένα: `DELETE FROM INCIs WHERE DataSource='ALL_INCIS_NEW_FILE';`
- Πλήρες: restore από το backup παραγωγής (`WITH REPLACE, RECOVERY`).
- Migrations: `dotnet ef database update 20260702214036_AddFormulaApprovalWorkflow` (ή `05_migrations_down.sql`).

## Σημειώσεις ασφάλειας
- Τα endpoints εισαγωγής απαιτούν ρόλο **Admin/SuperUser** (`[Authorize(Roles=...)]`) και antiforgery token.
- Δεν έγινε καμία εγγραφή στην παραγωγική βάση.
- Η δοκιμαστική βάση `INCIS_SaaP_inspect` διατηρείται.
