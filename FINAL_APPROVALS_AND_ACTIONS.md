# FINAL APPROVALS & EXTERNAL ACTIONS — INCIS SaaP (RC1 v17)

Ημερομηνία: 2026-11-11 · Βάση: RC1 `TESTING_Release_v17_RC1_20261111.zip` (SHA256 `F95491300E300ECFD99C2D57DD19C9B0C9FE2E97536F4301C76BC39DD38DE694`).
Κατάσταση: **Τεχνικά GO** · **Παραγωγή NO-GO** μέχρι τα παρακάτω.

> **Επίσημο έγγραφο E1:** `docs/requests/PO_API_CONSUMERS_DECLARATION.md` (προς υπογραφή).
> Το `docs/requests/PO_REQUEST.md` είναι το **ερωτηματολόγιο/οδηγός** που καταλήγει στο επίσημο έγγραφο.

---

## ΜΕΡΟΣ Α — Τι πρέπει να εγκρίνεις ΕΣΥ (Ιδιοκτήτης PO+SEC)

### A1. E1 — Δήλωση εξωτερικών API consumers (αυτο-δήλωση, όχι τρίτος)
Συμπλήρωσε/υπόγραψε: `docs/requests/PO_API_CONSUMERS_DECLARATION.md`.
Επιλογή **μίας**:
- [ ] **A: Δεν υπάρχουν** consumers → συμπλήρωσε **εύρος ελέγχου** (συστήματα/δίκτυα/ομάδες/gateway logs) + ονοματεπώνυμο/ρόλο/ημερομηνία/υπογραφή.
- [ ] **B: Υπάρχουν** → επισύναψε πίνακα: σύστημα · ιδιοκτήτης · endpoints · HTTP methods · auth · συχνότητα · περιβάλλον.

### A2. D1 — Έγκριση CSRF enforcement (μετά τα αποδεικτικά)
- `Api__Csrf__Enforce=true` + restart.
- Επαλήθευση: Admin/TenantAdmin writes 2xx με `X-CSRF-TOKEN`· POST χωρίς token **400**· anonymous **401**· GET/HEAD ανεπηρέαστα.
- Επιτυχία 24h: 0 αύξηση 5xx, καμία διακοπή νόμιμων ροών.
- Rollback: `Api__Csrf__Enforce=false` + restart.

### A3. D2 — Έγκριση WritePolicy enforcement (μετά D1 σταθερό 24–48h)
- `Api__WritePolicy__Enforce=true` + restart.
- Επαλήθευση: Admin/SuperUser/TenantAdmin 2xx· `User` **403**· anonymous 401· cross-tenant απορρίπτεται· GET/HEAD ανεπηρέαστα.
- Rollback: `Api__WritePolicy__Enforce=false` + restart.

### A4. Έγκριση παραθύρου deployment
Έγκριση ημερομηνίας/ώρας για deploy RC1 + migrations + δημιουργία 4 tenants.

---

## ΜΕΡΟΣ Β — Πackage DevOps (E2b)
**Παραλήπτης:** DevOps · **Αποδεικτικό:** `phase0_summary.csv`, `phase0_by_endpoint.csv`, `phase0_report.md` + δήλωση πληρότητας.
1. Επιβεβαίωση deployed build με `ApiWritePolicyFilter`/`ApiCsrfFilter` + commit/tag + δείγμα log.
2. Logs **≥14 συνεχόμενες ημέρες**.
3. `.\tools\analyze-phase0-logs.ps1 -LogPath <logs_dir> -SinceDays 14 -OutDir .\phase0-report`
4. Reports + completeness (πρώτο/τελευταίο timestamp, ημέρες, κενά).
**Αποδοχή:** 0 OBSERVATION από ρόλους εκτός **Admin/SuperUser/TenantAdmin**· πλήρες 14ήμερο· redaction.

## ΜΕΡΟΣ Γ — Package Repo Owner (E3)
**Παραλήπτης:** Repo Owner · **Αποδεικτικό:** Run URL + commit SHA + artifacts + strict verifier PASS.
1. Repo/owner/visibility + branch protection.
2. `.\tools\verify-repo-publish.ps1 -Root .` → `EXIT=2 NOT_AUTHORIZED` (**όχι PASS**)· review `publish_candidates.csv`.
3. Με ρητή εξουσιοδότηση: `-OwnerAuthorized -RepoUrl <URL> -Branch main` → εκτυπώνει εντολές· μετά review `git init/add/commit/push`.
4. GitHub Secrets: `CI_SQL_SA_PASSWORD`, `CI_JWT_SECRET`, `CI_ADMIN_SEED_PASSWORD`.
5. Workflow `ci.yml` (SQL 2022) → run URL/SHA/artifacts.
6. `.\tools\verify-ci-artifacts.ps1 -ResultsDir <artifacts> -MinCoverage 70 -ExpectedCommitSha <sha> -Strict` → **PASS (693 xUnit + 54 Playwright)**.

## ΜΕΡΟΣ Δ — Package DBA (E4–E6)
**Παραλήπτης:** DBA · **Αποδεικτικό:** 11 outputs (`docs/requests/DBA_REQUEST.md` §2).
Script: `Database/Scripts/06_production_backup_and_rehearsal.sql` (guards: production exists· rehearsal ≠ production name/database_id· USE guard· DROP disabled).
1. BACKUP `WITH INIT, CHECKSUM` + SHA256. 2. VERIFYONLY. 3. FILELISTONLY. 4. Rehearsal restore (MOVE). 5. Μετρητές+fingerprints. 6. `DBCC CHECKDB` 0 errors. 7. Migrations στην rehearsal. 8. Restore-based rollback. 9. Cleanup μόνο rehearsal.
**Αποδεικτικά:** .bak+SHA256, VERIFYONLY valid, FILELISTONLY, restore output, μετρητές/fingerprints, CHECKDB 0, migrations OK, rollback OK, χρόνοι/χώρος.

---

## ΜΕΡΟΣ Ε — Σειρά μέχρι την έναρξη
1. E1 υπογραφή (εσύ). 2. E2b reports (DevOps). 3. E3 CI+verifier (Repo Owner). 4. E4–E6 (DBA). 5. D1 → D2 με έγκριση. 6. Deploy RC1 + migrations. 7. 4 tenants + TenantAdmins. 8. Backup+monitoring 24–48h. 9. Go-live checklist.

> **Καμία ενέργεια δεν έχει εκτελεστεί** για E1–E6/D1–D2. v16, datasets, backups ανέπαφα. Flags `false`.
