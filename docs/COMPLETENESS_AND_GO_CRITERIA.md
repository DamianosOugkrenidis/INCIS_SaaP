# Πληρότητα & Κριτήρια NO-GO → GO — INCIS SaaP

Ημερομηνία: 2026-11-01 · Βάση: **v16** (αμετάβλητο) · Flags: `WritePolicy=false`, `Csrf=false`, `DemoToken=false`.
Τρέχουσα απόφαση: **NO-GO** για production enforcement & δημοσίευση.

---

## A. Ολοκληρωμένες εργασίες

| # | Εργασία | Αποδεικτικό |
|---|---|---|
| 1 | Tests: 693 xUnit + 32 Playwright = **725/725**, 0 failed/skipped | `docs/reports/*.trx`, `reports/repro/` |
| 2 | Coverage Release **89,82%** (line) — gate ≥70% PASS | `docs/reports/ci-coverage.cobertura.xml` |
| 3 | H-1 (MVC writes role restriction, 19 POST actions) | `AccessPolicyTests` 11/11 |
| 4 | H-2 Phase 0/1 μηχανισμός (log-only default) | `ApiWriteAccessTests` 16/16, `ApiWritePolicyTests` 8/8, `ApiWriteStatusCodesTests` |
| 5 | CSRF Phase 0/1 + συνδυασμός H-2 | `ApiCsrfTests` 8/8, `CsrfH2CombinationTests` 7/7 |
| 6 | Phase 0 logging εκπέμπει πραγματικά logs | `ApiWritePolicyLoggingTests` 3/3 |
| 7 | Κατάλογος INCI: importer ALL_INCIS_NEW_FILE, lossless & idempotent (19.724) | `InciCatalogImportServiceTests` (9), `Database/Scripts/*` |
| 8 | Τοπικό backup/restore/rollback rehearsal | `local_rehearsal_validation.log`, `MigrationRollbackTests` 2/2 |
| 9 | ML verification (πραγματικό μοντέλο + recovery) | `MLVerificationTests` 6 |
| 10 | Publish safety scan clean (0 findings) | `tools/verify-repo-publish.ps1`, `publish_candidates.csv` |
| 11 | Εργαλεία ελέγχου 14/14 PASS | `tools/tests/test-tools.ps1` |
| 12 | Έλεγχος ακεραιότητας (v16 restore) PASS | `docs/FILE_INTEGRITY_CHECK_2026-11-01.md` |
| 13 | Τεκμηρίωση ρόλων PO/SEC=Ιδιοκτήτης | ledger + package docs |

---

## B. Αποφάσεις που απαιτούν τη δική σου έγκριση

| ID | Αντικείμενο | Εισήγηση | Κατάσταση |
|---|---|---|---|
| D1 | CSRF enforce | DEFER | **ΑΝΑΜΕΝΕΙ** |
| D2 | WritePolicy enforce | DEFER | **ΑΝΑΜΕΝΕΙ** |
| D3 | API auth mechanism | APPROVE(A)/DEFER(B) | **ΑΝΑΜΕΝΕΙ** |
| D4 | Αποδοχή κινδύνων M/L | APPROVE + plan | **ΑΝΑΜΕΝΕΙ** |
| D5 | Δημοσίευση repo | DEFER | **ΑΝΑΜΕΝΕΙ** |
| D6 | Όρια ML | APPROVE + labeling | **ΑΝΑΜΕΝΕΙ** |
| E1 | Δήλωση consumers (Α/Β) | — | **ΑΝΑΜΕΝΕΙ** |

---

## C. Εξωτερικά αποδεικτικά που εκκρεμούν

| ID | Αποδεικτικό | Φορέας/πρόσβαση | Κατάσταση |
|---|---|---|---|
| E2b | 3 reports + completeness + deployed commit/tag | DevOps (prod logs) | **NOT RECEIVED** |
| E3 | Run URL + SHA + artifacts + strict verifier PASS | Repo Owner/GitHub | **NOT RECEIVED** |
| E4 | .bak + SHA256 + VERIFYONLY | DBA (prod DB) | **NOT RECEIVED** |
| E5 | FILELISTONLY + rehearsal + μετρητές + CHECKDB | DBA | **NOT RECEIVED** |
| E6 | Restore-based rollback + χρόνοι/χώρος | DBA | **NOT RECEIVED** |

Βλ. `docs/requests/EXTERNAL_EVIDENCE_TRACKER.md`.

---

## D. Τεχνικοί κίνδυνοι που παραμένουν

| ID | Sev | Κίνδυνος | Μετριασμός τώρα | Κλείνει με |
|---|---|---|---|---|
| H-2 | **High** | Authenticated μη-admin χρήστης μπορεί να γράψει μέσω API | authenticated-only (όχι anonymous)· cookie | D2 (WritePolicy) |
| R2 | Medium | CSRF (cookie writes) | `SameSite=Lax`· Phase 0 logs | D1 (CSRF enforce) |
| M-1 | Medium | Content assertions ~50 views | μερικό Playwright | επέκταση tests (D4) |
| M-2 | Medium | Email/2FA flows μη αυτοματοποιημένα | — | mock IEmailService (πριν επόμενη έκδοση) |
| M-3 | Medium | API POST testing μερικό | — | POST tests |
| L-1..L-5 | Low | PDF layout, Hangfire, ML stubs, branch coverage, search index | τεκμηριωμένα | remediation plan |
| — | Medium | Swagger διαφημίζει ανενεργό Bearer | — | D3 |
| — | Low | Tracked-file check NOT VERIFIED (no repo) | — | D5/E3 |

---

## E. Κριτήρια μετάβασης NO-GO → GO (όλα απαιτούνται)

- [ ] **E1** υπογεγραμμένη δήλωση consumers (Α ή Β).
- [ ] **E2b** πλήρες 14ήμερο + 0 OBSERVATION από μη-Admin/SuperUser + 3 reports.
- [ ] **E3** official CI run URL + SHA + artifacts + strict verifier PASS.
- [ ] **E4–E6** 11 DBA αποδεικτικά (VERIFYONLY valid, CHECKDB 0 errors, μετρητές/fingerprints, rollback).
- [ ] **D1** εγκεκριμένο & δοκιμασμένο σε staging.
- [ ] **D2** εγκεκριμένο & D1 σταθερό 24–48h.
- [ ] **D4** αποδοχή κινδύνων M/L με remediation plan.
- [ ] **D5** review + `EXIT=0` + tracked check (αν απαιτηθεί δημοσίευση).
- [ ] **D6** αποδοχή ορίων ML με labeling.
- [ ] Flags ενεργοποίησης μόνο κατόπιν ρητής έγκρισης + monitoring/rollback στο `GO_LIVE_PLAN.md`.

---

## F. Τρέχουσα απόφαση

**NO-GO** για production enforcement, δημοσίευση repository και οποιαδήποτε παραγωγική αλλαγή, μέχρι να συγκεντρωθούν/επαληθευτούν τα αποδεικτικά και να δοθούν ρητές εγκρίσεις D1–D6.

**Υπογραφή (Project Owner — PO+SEC):** ____________________  **Ημερομηνία:** __________
