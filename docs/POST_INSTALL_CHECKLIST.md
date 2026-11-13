# Μετα-Εγκαταστασιακός Έλεγχος (Post-Installation Checklist)

Εκτελείται μετά την παραγωγική εγκατάσταση, με έγκριση. Όλα τα βήματα είναι **read-only** εκτός από το import που έχει εγκριθεί ρητά.

## 0. Προϋποθέσεις
- [ ] Υπάρχει **επαληθευμένο** backup παραγωγής: `RESTORE VERIFYONLY` → valid.
- [ ] Έχει σημειωθεί το SHA256 του backup.
- [ ] Έχει εγκριθεί παράθυρο συντήρησης & το import.

## 1. Schema/Migrations
- [ ] `dotnet ef database update` (ή `04_migrations_up.sql`) ολοκληρώθηκε χωρίς σφάλμα.
- [ ] `SELECT MigrationId FROM __EFMigrationsHistory` περιέχει:
  `20261020132411_AddInciCatalogFields`, `20261020133742_EnlargeInciCatalogFields`, `20261020141235_AddInciFullName`.
- [ ] Οι νέες στήλες υπάρχουν στον `INCIs` (`DataSource, ImportedAt, Origin, Restriction, ToxicologicalProfile, FullName`).
- [ ] **Μετρητές πριν = μετά** για CosIng/Formulas/RawMaterials/Clients/Suppliers.

## 2. Εισαγωγή καταλόγου INCI
- [ ] `POST /Import/SeedInciCatalog` (Admin/SuperUser) ή upload.
- [ ] Αποτέλεσμα: `Inserted` ≈ 19.724, `ErrorRows = 0`.
- [ ] Επανάληψη → `Inserted = 0`, `Updated` ≈ 19.724 (idempotent).
- [ ] `SELECT COUNT(*), COUNT(DISTINCT FullName) FROM INCIs` → ίσα, 0 διπλά.
- [ ] Σύνθετα ονόματα: `SELECT COUNT(*) FROM INCIs WHERE Name <> FullName` = **7**, `MAX(LEN(FullName))` = 1903.

## 3. Λειτουργικότητα
- [ ] `03_post_import_checks.sql` — όλες οι ενότητες αναμενόμενες.
- [ ] Αναζήτηση INCI με Name / FullName / CAS / EC επιστρέφει σωστά αποτελέσματα.
- [ ] CosIng search/annex/stats λειτουργούν (Annex II–VI).
- [ ] Σελίδες Clients/Suppliers/RawMaterials/Formulas/Reports φορτώνουν.
- [ ] Σύνδεση/αποσύνδεση, ρόλοι, Access Denied λειτουργούν.
- [ ] Εξαγωγές (Excel/PDF) λειτουργούν.
- [ ] Logs: κανένα unhandled exception σε startup/import.

## 4. Ασφάλεια
- [ ] Endpoints εισαγωγής απορρίπτουν anonymous (401/302) και non-admin (403).
- [ ] POST χωρίς antiforgery token → 400.
- [ ] Δημόσια endpoints (health, swagger, plans, cosing search) παραμένουν προσβάσιμα.

## 5. Απόδοση
- [ ] Αναζήτηση INCI < 200 ms (19.7k).
- [ ] Import ολοκληρώνεται εντός αποδεκτού χρόνου (~<60 s).

## 6. Rollback (αν αποτύχει κάτι)
- [ ] `DELETE FROM INCIs WHERE DataSource='ALL_INCIS_NEW_FILE';` (μερικό) ή
- [ ] Restore από το backup (`WITH REPLACE, RECOVERY`).
- [ ] `dotnet ef database update 20260702214036_AddFormulaApprovalWorkflow` (migration down).
- [ ] Επανέλεγχος §1 μετρητών.

## 7. Σημείωση
Οι αυτοματοποιημένες δοκιμές (663 xUnit — 0 skipped, + 32 Playwright) εκτελούνται **μόνο** σε απομονωμένες βάσεις· δεν υποκαθιστούν τον παραπάνω έλεγχο παραγωγής. **H-2:** Phase 0 (log-only) = default· Phase 1 επαληθεύτηκε σε **απομονωμένο test env** (403/allow)· **production enforce ΔΕΝ ενεργοποιήθηκε** — απαιτείται έγκριση + έλεγχος εξωτερικών consumers. Demo token = **501 by default**.

## 8. Αποδεικτικά Backup (production)
- Αρχείο: `Database\INCIS_SaaP.bak`
- `RESTORE HEADERONLY` → DatabaseName `INCIS_SaaP`, CompatibilityLevel 160, DatabaseVersion 957, SoftwareVersionMajor 16 (SQL Server 2022).
- `RESTORE VERIFYONLY` → **«The backup set on file 1 is valid»**.
- **Δοκιμασμένη επαναφορά:** το backup έχει γίνει restore επιτυχώς 2 φορές (σε `INCIS_SaaP_inspect`/`INCIS_SaaP_verify`).
- SHA256: `DBDC376B890BCE0EA8D56CF53253A610A7F4FBC57AB6D3184F68F4A9D258674F`.
- **Απαίτηση:** πριν την εγκατάσταση, λήψη **νέου** full backup παραγωγής `WITH CHECKSUM` + `VERIFYONLY` (το παρόν είναι reference snapshot).
- **Ιδανικά:** δοκιμαστική επαναφορά (restore rehearsal) σε ξεχωριστή βάση πριν την εγκατάσταση.

## 9. Κριτήρια Επιτυχίας (Go) / Αποτυχίας (No-Go)
**Go** — όλα αληθή:
- [ ] Νέα migrations εφαρμόστηκαν· `__EFMigrationsHistory` περιέχει τα 3 νέα IDs.
- [ ] Μετρητές πριν = μετά για CosIng/Clients/Suppliers/RawMaterials/Formulas.
- [ ] `COUNT(*) = COUNT(DISTINCT FullName)` στον `INCIs`, 0 διπλά.
- [ ] `RowsWithShortenedKey = 7`, `MAX(LEN(FullName)) = 1903`.
- [ ] Επαναλαμβανόμενο import → 0 inserts (idempotent).
- [ ] CosIng Annex II–VI search/checks λειτουργούν.
- [ ] Login/ρόλοι/AccessDenied λειτουργούν· import endpoints απορρίπτουν anonymous/non-admin· POST χωρίς token → 400.

**No-Go — ενεργοποίηση rollback:**
- [ ] Οποιοδήποτε migration σφάλμα ή απώλεια δεδομένων.
- [ ] Μετρητές διαφέρουν (CosIng/κ.λπ.).
- [ ] Διπλές εγγραφές ή `ErrorRows > 0`.
- [ ] Λειτουργικό σφάλμα σε σελίδα/ενδόσηματά της (5xx).
- [ ] Αδυναμία σύνδεσης/ρόλων.

## 10. Rollback (συγκεκριμένα βήματα + κριτήρια)
**Επίπεδο Α — μόνο INCI (γρήγορο):**
1. `DELETE FROM INCIs WHERE DataSource = 'ALL_INCIS_NEW_FILE';`
2. *Κριτήριο επιτυχίας:* `SELECT COUNT(*) FROM INCIs WHERE DataSource='ALL_INCIS_NEW_FILE'` = 0· λοιποί πίνακες αμετάβλητοι.

**Επίπεδο Β — schema (migration down):**
1. `dotnet ef database update 20260702214036_AddFormulaApprovalWorkflow` (ή `05_migrations_down.sql`).
2. *Κριτήριο:* `__EFMigrationsHistory` χωρίς τα 3 νέα IDs· στήλη `FullName` απουσιάζει.
3. *Προσοχή:* να έχουν διαγραφεί τιμές που υπερβαίνουν τα αρχικά μεγέθη (Name>200), αλλιώς η `ALTER` αποτυγχάνει.

**Επίπεδο Γ — πλήρες restore:**
1. `ALTER DATABASE [INCIS_SaaP] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;`
2. `RESTORE DATABASE [INCIS_SaaP] FROM DISK=N'<backup>' WITH REPLACE, RECOVERY;`
3. `ALTER DATABASE [INCIS_SaaP] SET MULTI_USER;`
4. *Κριτήριο:* μετρητές/λειτουργίες ταυτόσημοι με το στιγμιότυπο πριν την εγκατάσταση.

