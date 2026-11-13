# FINAL CLOSURE ACTION PLAN — INCIS SaaP

Ημερομηνία: 2026-11-11 · Βάση: **RC1 v17** `TESTING_Release_v17_RC1_20261111.zip`
SHA256 `F95491300E300ECFD99C2D57DD19C9B0C9FE2E97536F4301C76BC39DD38DE694` (επαληθεύτηκε 2026-11-11).
v16 frozen `TESTING_Release_v16_20261020.zip` SHA256 `D517D7FEF5C2E51AD0F89B5D98493E8ED806F77E521742E14D194FF9310FDFA7` (επαληθεύτηκε, **αμετάβλητο**).
Production flags: `Api__WritePolicy__Enforce=false`, `Api__Csrf__Enforce=false`, `Api__DemoToken__Enabled=false`.

Κατάσταση: **Τεχνικά GO** (build/tests/isolation/load/backup επαληθευμένα) · **Εμπορική παραγωγή NO-GO** έως E1–E6 + D1/D2 + έγκριση παραθύρου.

Σκοπός του εγγράφου: **οδηγός κλεισίματος** — ποιες εκκρεμότητες μένουν, ποιος τις εκτελεί, τι πακέτο παραδίδεται, με ποια κριτήρια κλείνουν, με ποια σειρά, και **ποια έγγραφα παραλαμβάνει/υπογράφει ο Ιδιοκτήτης (PO/SEC) και σε ποιο στάδιο**.
Δεν αποτελεί έγκριση εγκατάστασης. Καμία εξωτερική αποστολή/εκτέλεση δεν έχει γίνει.

---

## 0. Σύνοψη απόφασης

- Το προϊόν είναι **τεχνικά ολοκληρωμένο RC** για 4 ανεξάρτητους tenants (build 0/0, xUnit + Playwright + API E2E + isolation + soak + backup/restore).
- Κλείνει **μόνο** με: E1 (δήλωση Ιδιοκτήτη), E2b (14ήμερο prod logs), E3 (official CI), E4–E6 (DBA), και έγκριση **D1/D2**.
- **Κρίσιμο σημείο χρονοδιαγράμματος:** το 14ήμερο του **E2b** είναι ο μεγαλύτερος πόλος. Ξεκινά **μόνο** όταν υπάρχει εγκεκριμένο production deploy build που περιέχει **και** τους δύο filters (log-only, flags=false). Άρα η έγκριση παραθύρου (ή περιορισμένου Phase-0 deploy) είναι προαπαιτούμενο **νωρίς**, όχι στο τέλος.
- Οι τέσσερις λωρίδες (Ιδιοκτήτης, DevOps, Repo Owner, DBA) εκτελούνται **παράλληλα**· η κρίσιμη διαδρομή είναι: **E2b genesis deploy → 14 ημέρες → E2b reports → D1 → (24–48h) → D2 → deploy RC1+** → tenants → go-live.

---

## 1. Έλεγχος συνεκτικότητας RC1 (έγινε 2026-11-11)

| Έλεγχος | Αποτέλεσμα |
|---|---|
| ZIP integrity / entries | 532 entries · 152.525.479 B (≈145,5 MB uncompressed) |
| SHA256 vs δηλωμένο | **MATCH** (`F9549130…E694`) |
| Source πλήρες (`code/`, `.sln`, `Program.cs`, `csproj`) | **ΝΑΙ** |
| Migrations παρούσες (έως `20261111141005_TenantOwnershipDocumentsWebhooks` + snapshot) | **ΝΑΙ** |
| Deployment package + template flags | **ΝΑΙ** (`docs/deployment/COMMERCIAL_DEPLOYMENT_PACKAGE.md`, `appsettings.Production.template.json`) |
| Backup/rollback scripts + runbook | **ΝΑΙ** (`Database/Scripts/06_…`, `07_…`, `DEPLOYMENT_AND_ROLLBACK.md`, `docs/DBA_BACKUP_REHEARSAL_RUNBOOK.md`) |
| CI workflow | **ΝΑΙ** (`.github/workflows/ci.yml`) |
| Tests + tools παρόντα | **ΝΑΙ** (`INCIS_SaaP.Tests/`, `INCIS_SaaP.PlaywrightTests/`, `tools/`) |
| Secrets/PII στο ZIP | Δεν εντοπίστηκε πραγματικό secret (`.xls` dataset **εξαιρέθηκε** σωστά· 0 matches) |
| **Release-hygiene εύρημα 1** | **9 stray logs στο `code/INCIS_SaaP/`**: `build.log`, `ed.log`, `fin.log`, `final.log`, `smoke.log`, `smoke2..5.log`. Περιέχουν εσωτερικό hostname (`SQL Server: PYTHAGORAS@INCIS_SaaP`), τοπικά absolute paths (`C:\Users\daoug\Desktop\project\INCIS_SaaP_v3\…`) και ένα **αποτυχημένο build** παλαιότερης έκδοσης. Δεν είναι RC artifacts. |
| **Release-hygiene εύρημα 2** | Δεν υπάρχει **script αναπαραγωγής** του release ZIP. Η εξαίρεση ήταν ad-hoc (εξαίρεσε `*.xls`, αλλά **όχι** `*.log`) → το build του RC δεν είναι πλήρως επαναλήψιμο/ελεγχόμενο. |
| **Doc consistency** | **ΕΠΙΛΥΘΗΚΕ (2026-11-11).** Τα RC1 reports έφεραν `714 xUnit` / `51 Playwright` χωρίς TRX. Πραγματικά RC1 v17: **693 xUnit + 54 Playwright** (TRX: `docs/reports/rc1_2026-11-11/`). Διορθώθηκαν τα RC1/v17 reports· τα v16 reports (693+32=725) διατηρήθηκαν ως ιστορικό. Ledger: `docs/reports/rc1_2026-11-11/RESULTS.md`. |

**Ετυμηγορία:** Το RC1 είναι **συνεκτικό ως προς το source** (πλήρες, χτίζεται, migrations/deploy/rollback παρόντα). Απομένουν **2 hygiene εκκαθαρίσεις συσκευασίας** πριν την αποστολή/δημοσίευση:
1. Αφαίρεση των 9 stray logs από το `code/INCIS_SaaP/` (και προσθήκη `*.log` στα excludes).
2. Προσθήκη **script συσκευασίας** με ρητό manifest include/exclude ώστε το RC να είναι επαναλήψιμο.

> Αυτές είναι αλλαγές **συσκευασίας** (όχι λειτουργικότητας/ανάπτυξης) και **δεν** εκτελέστηκαν. Απαιτούν ρητή έγκριση (βλ. §11). Δεν επηρεάζουν το source tree που περνά στα tests· αλλά επηρεάζουν την καθαρότητα του artifact που θα σταλεί σε Repo Owner/DBA.

---

## 2. Πίνακας εκκρεμοτήτων E1–E6 (+D1/D2)

| ID | Τι εκκρεμεί | Υπεύθυνος | Απαιτούμενη πρόσβαση | Έτοιμο πακέτο προς παράδοση | Παραδοτέο (αποδεικτικό) | Κριτήριο ολοκλήρωσης |
|---|---|---|---|---|---|---|
| **E1** | Γραπτή επιβεβαίωση εξωτερικών API consumers / εύρους | **Ιδιοκτήτης (PO+SEC)** — αυτο-δήλωση | Καμία (self) | `docs/requests/PO_API_CONSUMERS_DECLARATION.md` (+ οδηγός `PO_REQUEST.md`) | Υπογεγραμμένη δήλωση **Α** (δεν υπάρχουν + εύρος ελέγχου) ή **Β** (πίνακας consumers) + υπογραφές PO & SEC + ημερομηνία | Υπογεγραμμένο PDF/doc με επιλογή Α ή Β, εύρος, ονοματεπώνυμο/ρόλο, ημερομηνία |
| **E2b** | 14ήμερα **production** Phase 0 logs + 3 reports | **DevOps** | Production app logs + commit/tag του deployed build | `docs/requests/DEVOPS_REQUEST.md` + εργαλείο `tools/analyze-phase0-logs.ps1` | `phase0_summary.csv`, `phase0_by_endpoint.csv`, `phase0_report.md` + δήλωση πληρότητας (πρώτο/τελευταίο timestamp, ημέρες, κενά) + deployed commit/tag + δείγμα log | ≥14 **συνεχόμενες** ημέρες· **0 OBSERVATION** από ρόλους εκτός Admin/SuperUser/TenantAdmin· πλήρες redaction ταυτοτήτων |
| **E3** | Official CI run + artifacts + strict verifier | **Repo Owner** | GitHub repo + runner + secrets | `.github/workflows/ci.yml` + `tools/verify-repo-publish.ps1` + `tools/verify-ci-artifacts.ps1` + `docs/requests/GITHUB_CI_REQUEST.md` | Run URL + commit SHA + artifacts (`coverage-report`, `playwright-results`, TRX) + έξοδος `verify-ci-artifacts.ps1 -ExpectedCommitSha <sha> -Strict` = **PASS** | verifier exit 0 (0 failed, coverage ≥70%, SHA match, strict consistent) |
| **E4** | Production backup + verify | **DBA** | Production DB `BACKUP` δικαίωμα | `Database/Scripts/06_production_backup_and_rehearsal.sql` + `docs/requests/DBA_REQUEST.md` | `.bak` + SHA256 + `RESTORE VERIFYONLY` = valid | “The backup set on file 1 is valid.” |
| **E5** | Rehearsal restore + integrity | **DBA** | `CREATE DATABASE` στον instance + χώρος ~2×DB | (ίδιο script) | `FILELISTONLY` + restore output + μετρητές/fingerprints (9 πίνακες) + `DBCC CHECKDB` (rehearsal) + migrations στην rehearsal | CHECKDB **0 errors**· μετρητές/fingerprints ταυτόσημα· migrations χωρίς σφάλμα |
| **E6** | Restore-based rollback rehearsal | **DBA** | (ίδιο) | (ίδιο script) | Rollback output + επανέλεγχος μετρητών + χρόνοι/χώρος | Επιτυχής επαναφορά· μετρητές ταυτόσημοι· καταγεγραμμένοι χρόνοι/χώρος |
| **D1** | Απόφαση + σταδιακή ενεργοποίηση **CSRF** | **Ιδιοκτήτης (PO+SEC)** | Production config + restart | `docs/decisions/PO_SEC_DECISION_PACKAGE.md` (D1) + `docs/security/ENFORCEMENT_ACTIVATION_RUNBOOK.md` | Υπογεγραμμένη απόφαση D1 + logs επαλήθευσης | Admin/TenantAdmin writes 2xx με `X-CSRF-TOKEN`· POST χωρίς token **400**· anon **401**· GET/HEAD ανεπηρέαστα· 24h **0 αύξηση 5xx** |
| **D2** | Απόφαση + ενεργοποίηση **WritePolicy** | **Ιδιοκτήτης (PO+SEC)** | Production config + restart | (ίδιο) D2 | Υπογεγραμμένη απόφαση D2 + logs | Admin/SuperUser/TenantAdmin 2xx· `User` **403**· anon **401**· cross-tenant απορρίπτεται· GET/HEAD ανεπηρέαστα· D1 σταθερό 24–48h |

> Πλήρης φάκελος αποφάσεων D3–D6: `docs/decisions/PO_SEC_DECISION_PACKAGE.md` (D3 auth mechanism, D4 αποδοχή M/L κινδύνων, D5 δημοσίευση repo, D6 όρια ML).

---

## 3. Έτοιμα πακέτα προς παράδοση (inventory)

| Παραλήπτης | Πακέτο (paths) | Περιεχόμενο |
|---|---|---|
| **Ιδιοκτήτης (εσύ)** | `docs/requests/PO_API_CONSUMERS_DECLARATION.md`, `docs/requests/PO_REQUEST.md`, `docs/decisions/PO_SEC_DECISION_PACKAGE.md`, `docs/security/ENFORCEMENT_ACTIVATION_RUNBOOK.md` | Προς υπογραφή: E1 δήλωση + D1–D6 αποφάσεις + runbook ενεργοποίησης |
| **DevOps** | `docs/requests/DEVOPS_REQUEST.md`, `tools/analyze-phase0-logs.ps1` | Οδηγίες 14ημέρου + εργαλείο ανάλυσης Phase 0 |
| **Repo Owner** | `docs/requests/GITHUB_CI_REQUEST.md`, `.github/workflows/ci.yml`, `tools/verify-repo-publish.ps1`, `tools/verify-ci-artifacts.ps1`, `docs/reports/publish_candidates.csv` | Pre-push gate + CI workflow + verifiers + candidate list |
| **DBA** | `docs/requests/DBA_REQUEST.md`, `Database/Scripts/06_production_backup_and_rehearsal.sql`, `Database/Scripts/07_scheduled_backup.sql`, `docs/DBA_BACKUP_REHEARSAL_RUNBOOK.md` | Script με hard guards + runbook + λίστα 11 αποδεικτικών |
| **Όλοι (ενιαίο)** | `docs/requests/DISPATCH_PACKAGE.md` | Τα 4 αιτήματα έτοιμα προς αποστολή (text/email) |
| **Release (νέο, έτοιμο)** | `dist/v18_RC2_20261111.zip` (+ `.sha256`, `.MANIFEST.csv`) | Καθαρό, deterministic, buildable package — SHA256 `75DBB95D…419D` |
| **DevOps — Phase 0 (νέο)** | `docs/operations/PHASE0_DEPLOYMENT_PACKAGE.md` | Deploy/verify/rollback log-only (flags false) |
| **Repo Owner (νέο)** | `docs/requests/E3_REPO_OWNER_PACKET.md` | Αυτοτελές E3 |
| **DBA (νέο)** | `docs/requests/E4_E6_DBA_PACKET.md` | Αυτοτελές E4–E6 |
| **Ιδιοκτήτης — αποφάσεις (νέο)** | `docs/decisions/OWNER_DECISION_SEQUENCE.md` | Σειρά αποφάσεων |
| **Release hygiene (νέο)** | `docs/reports/release_hygiene/STRAY_LOGS_INVENTORY.md` + `.csv` | Inventory 9 logs + removal procedure |
| **Test evidence (νέο)** | `docs/reports/rc1_2026-11-11/RESULTS.md` | Πραγματικά αποτελέσματα + ιστορικό εκδόσεων |

Αποστολή: **εκκρεμεί** (NOT SENT) — απαιτεί εξουσιοδοτημένο άτομο & καταγραφή ημερομηνίας. Ο ρόλος PO καλύπτεται προσωπικά από τον Ιδιοκτήτη (E1 δεν απαιτεί τρίτο).

---

## 4. Τι απαιτείται από **DevOps** (E2b)

1. **Επιβεβαίωση build (πύλη έναρξης):** το deployed production build περιέχει `ApiWritePolicyFilter` **και** `ApiCsrfFilter` (markers `API-write … OBSERVATION`, `API-CSRF … OBSERVATION`). Παράδοση **commit/tag + δείγμα log line**. Αν λείπουν → το 14ήμερο **δεν ξεκινά** (απαιτείται deploy instrumented build — βλ. κρίσιμο σημείο §0).
2. **Συλλογή logs ≥14 συνεχόμενες ημέρες** (production).
3. **Ανάλυση:** `.\tools\analyze-phase0-logs.ps1 -LogPath <logs_dir> -SinceDays 14 -OutDir .\phase0-report`.
4. **Παράδοση:** `phase0_summary.csv`, `phase0_by_endpoint.csv`, `phase0_report.md` + δήλωση πληρότητας (πρώτο/τελευταίο timestamp, ημέρες, κενά).
5. **Αποδοχή:** 0 OBSERVATION από ρόλους εκτός Admin/SuperUser/TenantAdmin· πλήρες 14ήμερο· redaction ταυτοτήτων (`user-<hash>`, χωρίς emails/tokens).

---

## 5. Τι απαιτείται από **Repo Owner** (E3)

1. Επιβεβαίωση repo/owner/visibility + branch protection.
2. Pre-push gate: `.\tools\verify-repo-publish.ps1 -Root .` → **αναμενόμενο EXIT=2 `NOT_AUTHORIZED`** (scan clean, **όχι** PASS)· review `docs/reports/publish_candidates.csv`. Exit contract: 0=PASS, 1=SECURITY_FINDINGS, 2=NOT_AUTHORIZED, 3=INVALID_INPUT.
3. **Μόνο με ρητή εξουσιοδότηση (D5):** `.\tools\verify-repo-publish.ps1 -Root . -OwnerAuthorized -RepoUrl <URL> -Branch main` → εκτυπώνει εντολές· review `git init/add/commit/push`· επιβεβαίωση branch + commit SHA.
4. GitHub Secrets: `CI_SQL_SA_PASSWORD`, `CI_JWT_SECRET`, `CI_ADMIN_SEED_PASSWORD`.
5. Run `ci.yml` (SQL Server 2022) → **run URL + SHA + artifacts**.
6. Verifier: `.\tools\verify-ci-artifacts.ps1 -ResultsDir <artifacts> -MinCoverage 70 -ExpectedCommitSha <sha> -Strict` → **PASS**.

---

## 6. Τι απαιτείται από **DBA** (E4–E6)

Script: `Database/Scripts/06_production_backup_and_rehearsal.sql` (hard guards: production υπάρχει· rehearsal ≠ production name/database_id· USE guard· DROP απενεργοποιημένο/σχολιασμένο).
Βήματα: BACKUP `WITH INIT, CHECKSUM` (χωρίς COMPRESSION σε Express) → VERIFYONLY → FILELISTONLY → rehearsal restore (`WITH MOVE`) → μετρητές/fingerprints (9 πίνακες) → `DBCC CHECKDB` → migrations στην rehearsal → restore-based rollback → cleanup **μόνο** rehearsal.
**Παράδοση 11 αποδεικτικών** (βλ. `docs/requests/DBA_REQUEST.md` §2): .bak+SHA256 · VERIFYONLY valid · FILELISTONLY · restore output · μετρητές (INCIs 19.724 / CosIngIngredients 24.094 / CosIngEntries 2.353 ως σημείο αναφοράς) · fingerprints · CHECKDB 0 errors · migrations OK · rollback OK · χρόνοι · χώρος.
**Κανόνας:** καμία εγγραφή στην παραγωγή πέραν του BACKUP· απόκλιση σε οποιονδήποτε δείκτη → **No-Go** και διερεύνηση.

---

## 7. Τι απαιτείται από **εσένα (PO/SEC)**

| # | Έγγραφο | Ενέργεια | Στάδιο |
|---|---|---|---|
| P1 | `docs/requests/PO_API_CONSUMERS_DECLARATION.md` | Συμπλήρωση επιλογής **Α** ή **Β** + εύρος + **υπογραφή PO & SEC** + ημερομηνία | **ΑΜΕΣΑ (E1)** — ανεξάρτητα από τα υπόλοιπα |
| P2 | `docs/decisions/PO_SEC_DECISION_PACKAGE.md` → **D5** | Απόφαση δημοσίευσης repo (μετά review `publish_candidates.csv` + tracked check) | Πριν το E3 (παράλληλα) |
| P3 | `docs/decisions/PO_SEC_DECISION_PACKAGE.md` → **D4**, **D6**, **D3** | Αποδοχή κινδύνων M/L + remediation plan· όρια ML + labeling· κατεύθυνση auth | Πριν το D1 (παράλληλα) |
| P4 | `docs/decisions/PO_SEC_DECISION_PACKAGE.md` → **D1** | Έγκριση ενεργοποίησης CSRF — **μόνο μετά** E1+E2b+E3+E4–E6 + δοκιμή σε staging | Μετά τα E1–E6 |
| P5 | `docs/security/ENFORCEMENT_ACTIVATION_RUNBOOK.md` | Έγκριση/υπογραφή βήματος ενεργοποίησης + επαλήθευση | D1 ενεργό |
| P6 | `docs/decisions/PO_SEC_DECISION_PACKAGE.md` → **D2** | Έγκριση ενεργοποίησης WritePolicy — **μόνο μετά** D1 σταθερό 24–48h | Μετά το D1 |
| P7 | Έγκριση **παραθύρου deployment** | Ημερομηνία/ώρα deploy RC1 + migrations + 4 tenants | **Νωρίς** (απαιτείται και για την έναρξη E2b) |
| P8 | `docs/operations/COMMERCIAL_LAUNCH_RUNBOOK.md` §10 | Υπογραφή go-live checklist **ανά tenant** | Μετά deploy, ανά tenant |
| P9 | Τελικό **GO sign-off** | Τελική έγκριση εμπορικής διάθεσης | Τελευταίο στάδιο |

---

## 8. Έγγραφα που **παραλαμβάνεις** (από τρίτους) — και στάδιο

| Από | Παραλαμβάνεις | Στάδιο |
|---|---|---|
| DevOps | 3 Phase-0 reports + δήλωση πληρότητας + deployed commit/tag | Μετά το 14ήμερο (E2b) |
| Repo Owner | CI run URL + commit SHA + artifacts + verifier PASS output | Μετά το official CI (E3) |
| DBA | 11 αποδεικτικά (backup/verify/restore/CHECKDB/counters/rollback) | Πριν το deployment window (E4–E6) |

Όλα καταγράφονται στο `docs/requests/EXTERNAL_EVIDENCE_TRACKER.md` (καμία γραμμή **δεν** κλείνει χωρίς το πραγματικό αποδεικτικό).

---

## 9. Συντομότερη ασφαλής ακολουθία (παράλληλες λωρίδες)

```mermaid
flowchart TD
  T0[T0: εξουσιοδότηση αποστολής] --> L1[Λωρίδα Owner]
  T0 --> L2[Λωρίδα DevOps]
  T0 --> L3[Λωρίδα Repo Owner]
  T0 --> L4[Λωρίδα DBA]

  L1 --> E1[E1: υπογραφή δηλωσης Α/Β]
  L1 --> P7[Εγκριση παραθυρου / Phase-0 deploy]

  P7 --> G0[Deploy instrumented build flags=false]
  G0 --> W14[14ημερες production Phase 0 logs]
  W14 --> E2b[E2b: 3 reports + completeness]

  L3 --> E3[E3: push + official CI + verifier PASS]
  E3 --> R3{D5 εγκριση δημοσιευσης?}
  R3 -->|ναι| E3
  L4 --> E45[E4-E6: backup + rehearsal + rollback = 11 αποδεικτικα]

  E1 --> CP{Critical path πληρης?}
  E2b --> CP
  E3 --> CP
  E45 --> CP
  CP -->|ναι| D1[D1: CSRF enforce + verify]
  D1 --> S24[24-48h stable]
  S24 --> D2[D2: WritePolicy enforce + verify]
  D2 --> DEP[DEPLOY RC1 + migrations (εγκεκριμενο παραθυρο)]
  DEP --> TEN[Δημιουργια 4 tenants + TenantAdmins]
  TEN --> MON[Monitoring/backup 24-48h]
  MON --> GOLIVE[Go-live ανα tenant checklist]
```

**Κρίσιμη διαδρομή (ελάχιστος χρόνος):** E2b genesis deploy → **+14 ημέρες** → E2b reports → D1 → **+24–48h** → D2 → deploy RC1 → tenants → monitoring → go-live.
**Παράλληλα & αμέσως:** E1 (υπογραφή), E3 (repo/CI — μετά D5), E4–E6 (DBA backup/rehearsal — read-only πλην backup).
**Καθοριστικός παράγοντας χρόνου:** η έγκαιρη έγκριση για Phase-0/instrumented production deploy (ξεκινά το ρολόι του E2b).

---

## 10. Τελικά κριτήρια **εμπορικού GO** & διάθεση σε 4 tenants

Όλα απαιτούνται (βλ. `docs/COMPLETENESS_AND_GO_CRITERIA.md` §E):

- [ ] **E1** υπογεγραμμένη δήλωση (Α ή Β) + εύρος.
- [ ] **E2b** πλήρες 14ήμερο + 0 OBSERVATION από μη-Admin/SuperUser/TenantAdmin + 3 reports + completeness.
- [ ] **E3** official CI run URL + SHA + artifacts + `verify-ci-artifacts.ps1 -Strict` = **PASS**.
- [ ] **E4–E6** 11 DBA αποδεικτικά (VERIFYONLY valid, CHECKDB 0, μετρητές/fingerprints ταυτόσημα, rollback OK).
- [ ] **D1** εγκεκριμένο & επαληθευμένο (400/401/2xx, 0 αύξηση 5xx 24h).
- [ ] **D2** εγκεκριμένο & D1 σταθερό 24–48h.
- [ ] **D4/D6** αποδοχή κινδύνων M/L + labeling ML· **D3** κατεύθυνση auth.
- [ ] **D5** (αν απαιτηθεί δημοσίευση) review + `EXIT=0` + tracked check.
- [ ] Έγκριση **παραθύρου deployment** (RC1 + migrations + 4 tenants).
- [ ] Flags ενεργοποίησης **μόνο** μετά D1/D2· monitoring/rollback βάσει `docs/security/ENFORCEMENT_ACTIVATION_RUNBOOK.md`.

**Διάθεση σε 4 ανεξάρτητους tenants** (`docs/operations/COMMERCIAL_LAUNCH_RUNBOOK.md`):
1. Deploy RC1 → `Database.Migrate()` αυτόματα (περιλαμβάνει `20261111141005_TenantOwnershipDocumentsWebhooks`).
2. Δεδομένα: restore verified backup **ή** fresh import (INCI 19.724 + CosIng 24.094/2.353) + επαλήθευση μετρητών.
3. Provider Console: δημιουργία 4 tenants + plans/όρια (MaxUsers/MaxStorageMB).
4. TenantAdmin onboarding (προσωρινός κωδικός → αλλαγή → έγκριση χρηστών).
5. Έλεγχοι απομόνωσης/δικαιωμάτων (tenant A ≠ B· `User` χωρίς `/Users`/`/Tenants`).
6. Backup/monitoring ενεργά· RPO ≤15′ / RTO ≤4h.
7. Go-live checklist **ανά tenant** (§10 runbook).

**Περιορισμοί/γνωστά κενά που δεν μπλοκάρουν το αρχικό GO:** payment provider (S-7, χειροκίνητη τιμολόγηση λειτουργεί), branch coverage, ML hold-out metrics.

---

## 11. Τι ξεκινά **άμεσα** vs τι είναι **blocked**

**Ξεκινά άμεσα (δυνατό τώρα, χωρίς εξωτερική πρόσβαση):**
- **E1:** συμπλήρωση/υπογραφή δήλωσης από τον Ιδιοκτήτη (P1) — μπορεί να υπογραφεί **σήμερα**.
- Προετοιμασία εξουσιοδότησης αποστολής των 4 πακέτων (DISPATCH_PACKAGE) + καταγραφή D0.
- **D5/D4/D6/D3** αποφάσεις (παράλληλα).
- Έγκριση παραθύρου / περιορισμένου Phase-0 deploy (ξεκινά το ρολόι E2b).

**Blocked (απαιτεί τρίτο/πρόσβαση):**
- **E2b** — DevOps + production logs + deployed instrumented build.
- **E3** — Repo Owner + GitHub runner + secrets (+ D5).
- **E4–E6** — DBA + production DB access.
- **D1/D2** — μετά E1–E6 + ρητή έγκριση Ιδιοκτήτη.

**Release hygiene — κατάσταση (2026-11-11):**
- ✅ Inventory + redacted scan 9 stray logs: `docs/reports/release_hygiene/STRAY_LOGS_INVENTORY.md` — rotation **δεν** απαιτείται (0 credentials).
- ✅ Reproducible packaging: `tools/build-release.ps1` (deterministic) + `tools/scan-release-artifacts.ps1` → καθαρό **RC2** `dist/v18_RC2_20261111.zip` SHA256 `75DBB95D41D9C506EF21264493D103CBCBC34CBAD19F9FC8E9F348687E37419D` (FAIL=0, buildable, deterministic).
- ✅ Ενοποίηση Playwright/αποτελεσμάτων: **693 xUnit + 54 Playwright** (TRX: `docs/reports/rc1_2026-11-11/`).
- ⏳ Διαγραφή/quarantine source logs: **δεν εκτελέστηκε** (απαιτεί έγκριση)· το packaging τα αποκλείει ούτως ή άλλως.

---

## 12. Δεσμεύσεις / μη-ενέργειες

- **Καμία** παραγωγική αλλαγή/push/publish/αποστολή χωρίς ρητή εξουσιοδότηση.
- **Καμία** ενεργοποίηση `Api__Csrf__Enforce` / `Api__WritePolicy__Enforce` χωρίς D1/D2.
- v16 (`D517D7FE…0FDFA7`) και προηγούμενα releases **ανέπαφα**· datasets 19.724/24.094/2.353 ανέπαφα.
- Flags: `false` / `false` / `false`.
- Δεν δηλώνεται καμία εκκρεμότητα E1–E6 ως ολοκληρωμένη χωρίς το πραγματικό αποδεικτικό.

---

## Παραπομπές
`FINAL_DELIVERY_REPORT.md` · `FINAL_COMMERCIAL_GO_NO_GO.md` · `FINAL_APPROVALS_AND_ACTIONS.md` ·
`docs/requests/{DISPATCH_PACKAGE,PO_API_CONSUMERS_DECLARATION,PO_REQUEST,DEVOPS_REQUEST,GITHUB_CI_REQUEST,DBA_REQUEST,EXTERNAL_EVIDENCE_TRACKER,STATUS_LEDGER}.md` ·
`docs/decisions/PO_SEC_DECISION_PACKAGE.md` · `docs/security/ENFORCEMENT_ACTIVATION_RUNBOOK.md` · `docs/operations/COMMERCIAL_LAUNCH_RUNBOOK.md` ·
`docs/deployment/COMMERCIAL_DEPLOYMENT_PACKAGE.md` · `docs/COMPLETENESS_AND_GO_CRITERIA.md` · `docs/reports/RC_FINAL_REPORT.md`.
