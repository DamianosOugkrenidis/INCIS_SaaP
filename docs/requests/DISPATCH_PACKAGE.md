# DISPATCH PACKAGE — 4 Αιτήματα (PO / DevOps / Repo Owner / DBA)

> **ΚΑΤΑΣΤΑΣΗ: NOT SENT (εξωτερικά).** Δεν υπάρχει δυνατότητα αποστολής από το παρόν περιβάλλον για τα E2b/E3/E4–E6. Απαιτείται **εξουσιοδοτημένο άτομο** (email/ticketing) και καταγραφή ημερομηνίας/απάντησης.
> **Ο ρόλος PO καλύπτεται προσωπικά από τον Ιδιοκτήτη** → το **E1 δεν απαιτεί αποστολή σε τρίτο**· είναι **αυτο-δήλωση του Ιδιοκτήτη** (`docs/requests/PO_API_CONSUMERS_DECLARATION.md`) και δεν εκκρεμεί λόγω τρίτου.
> Οι προθεσμίες D+2 / D+3 / D+14 είναι **ενδεικτικές** μέχρι ορισμό πραγματικής D0.
> v16 frozen · Production flags: `WritePolicy=false`, `Csrf=false`, `DemoToken=false`.
> **Περιβάλλον:** αρχική παραγωγική φάση σε **τοπικό PC** (1 instance· SQL Server Express· DB `INCIS_SaaP`· 4 tenants). Οι ρόλοι DevOps/DBA ενδέχεται να ασκούνται από τον **ίδιο τοπικό operator**.

## Πίνακας παρακολούθησης
| ID | Παραλήπτης | Απαιτούμενη ενέργεια | Αποδεικτικό | Προθεσμία | Sent (ημερομηνία) | Απάντηση |
|---|---|---|---|---|---|---|
| E1 | **PO = Ιδιοκτήτης (self)** | Επιβεβαίωση consumers (ή «δεν υπάρχουν» + εύρος/υπεύθυνος) | Υπογεγραμμένο `PO_API_CONSUMERS_DECLARATION.md` | D+2 (indicative) | — | — |
| E2b | **DevOps** | Deployed build με filters + 14ήμερα production logs + analysis | 3 reports + completeness statement | D+14 (indicative) | — | — |
| E3 | **Repo Owner** | Owner authorization + push + official CI + artifacts | Run URL + SHA + artifacts + strict verifier PASS | D+3 (indicative) | — | — |
| E4–E6 | **DBA** | Production backup + rehearsal + rollback | 11 αποδεικτικά (`DBA_REQUEST.md` §2) | D+3 (indicative) | — | — |

---

## 1) PO — E1
**Παραλήπτης:** Product Owner INCIS · **Αποδεικτικό:** υπογεγραμμένο `docs/requests/PO_API_CONSUMERS_DECLARATION.md` (το `PO_REQUEST.md` είναι το ερωτηματολόγιο)

```text
Θέμα: INCIS SaaP — Γραπτή επιβεβαίωση εξωτερικών API consumers (E1)

Για την ασφαλή ενεργοποίηση των ελέγχων στα API write endpoints (/api/v1/*), χρειαζόμαστε
γραπτή επιβεβαίωση. Από τον κώδικα δεν εντοπίστηκε consumer, αλλά αυτό ΔΕΝ αποκλείει χρήση
στο πραγματικό περιβάλλον.

Για κάθε εξωτερικό σύστημα δώστε: όνομα, ιδιοκτήτη/υπεύθυνο, endpoints (POST/PUT/DELETE),
HTTP methods, τρόπο auth (cookie session / token / άλλο), συχνότητα και περιβάλλον (prod/test).

Ή δηλώστε ρητά «Δεν υπάρχουν εξωτερικοί API consumers» με:
- εύρος ελέγχου (συστήματα, δίκτυα, ομάδες)
- ονοματεπώνυμο/ρόλο αυτού που το επιβεβαιώνει.

Αποδεικτικό: υπογεγραμμένο docs/requests/PO_API_CONSUMERS_DECLARATION.md (ερωτηματολόγιο: PO_REQUEST.md).
Προθεσμία: [D+2 — προτεινόμενη].
Συνέπεια: χωρίς αυτό, WritePolicy/Csrf enforcement παραμένουν απενεργοποιημένα.
```

## 2) DevOps — E2b
**Παραλήπτης:** DevOps · **Αποδεικτικό:** `phase0_summary.csv`, `phase0_by_endpoint.csv`, `phase0_report.md` + δήλωση πληρότητας

```text
Θέμα: INCIS SaaP — Phase 0 logging & 14ήμερα production logs (E2b)

Πριν το 14ήμερο, επιβεβαιώστε ότι το deployed production build περιέχει:
- API-write PHASE-0 OBSERVATION (ApiWritePolicyFilter)
- API-CSRF PHASE-0 OBSERVATION (ApiCsrfFilter)
Δώστε commit/tag + δείγμα log line. Αν λείπουν, το 14ήμερο δεν ξεκινά.

Ενέργειες:
1. Logs >=14 συνεχόμενες ημέρες.
2. .\tools\analyze-phase0-logs.ps1 -LogPath <logs_dir> -SinceDays 14 -OutDir .\phase0-report
3. Επιστρέψτε τα 3 reports + δήλωση κάλυψης (πρώτο/τελευταίο timestamp, ημέρες, κενά).

Ιδιωτικότητα: πλήρες redaction ταυτοτήτων (user-<hash>)· δεν εξάγονται emails/tokens.
Κριτήρια: 14ήμερο πλήρες/συνεχές και 0 OBSERVATION από ρόλους εκτός Admin/SuperUser/TenantAdmin.
Η απουσία κλήσεων δεν αποδεικνύει απουσία consumers.
Προθεσμία: [D+14 — προτεινόμενη].
```

## 3) Repo Owner — E3
**Παραλήπτης:** Repository Owner · **Αποδεικτικό:** Run URL + commit SHA + artifacts + `verify-ci-artifacts.ps1 -ExpectedCommitSha <sha> -Strict` = PASS

```text
Θέμα: INCIS SaaP — Ασφαλής δημοσίευση & επίσημο GitHub Actions (E3)

Δεν έγινε push από την πλευρά μας. Πριν από οποιαδήποτε δημοσίευση:

1. Επιβεβαιώστε repository/owner/visibility και branch protection.
2. Υποχρεωτικό pre-push gate: .\tools\verify-repo-publish.ps1 -Root .
   EXPECTED: EXIT=2 REASON=NOT_AUTHORIZED (scan clean, ΟΧΙ έγκριση).
   Exit contract: 0=PASS, 1=SECURITY_FINDINGS, 2=NOT_AUTHORIZED, 3=INVALID_INPUT.
3. Review: docs/reports/publish_candidates.csv (500 files) + tracked files + πιθανά secrets/PII.
4. Μόνο με ρητή εξουσιοδότηση:
   .\tools\verify-repo-publish.ps1 -Root . -OwnerAuthorized -RepoUrl <URL> -Branch main
   (εκτυπώνει εντολές, δεν τις εκτελεί). Μετά review: git init/add συγκεκριμένων paths/
   commit/push. Επιβεβαιώστε branch + commit SHA.
5. Εκτελέστε ci.yml (SQL 2022) και επιστρέψτε run URL, SHA, artifacts.
6. .\tools\verify-ci-artifacts.ps1 -ResultsDir <artifacts> -MinCoverage 70 `
      -ExpectedCommitSha <sha> -Strict   -> PASS (693 xUnit + 54 Playwright)
Προθεσμία: [D+3 — προτεινόμενη].
```

## 4) DBA — E4–E6
**Παραλήπτης:** DBA · **Αποδεικτικό:** 11 outputs του `docs/requests/DBA_REQUEST.md` §2

```text
Θέμα: INCIS SaaP — Production backup/rehearsal/rollback με hard guards (E4–E6)

Καμία εγγραφή στην παραγωγή πέραν του BACKUP. Restore/migrations/rollback ΜΟΝΟ σε rehearsal DB.

Hard guards (πρέπει να περάσουν):
- DB_ID('INCIS_SaaP') IS NOT NULL
- rehearsal <> production (name + database_id)
- STEP 0 identity evidence: name, database_id, state_desc, create_date, owner
- USE guard (DB_NAME)
- DROP disabled (ConfirmDrop=0, EXEC σχολιασμένο) μέχρι έγκριση

Ενέργειες (06_production_backup_and_rehearsal.sql):
1 BACKUP WITH INIT, CHECKSUM (χωρίς COMPRESSION σε Express) + SHA256
2 RESTORE VERIFYONLY WITH CHECKSUM
3 RESTORE FILELISTONLY
4 RESTORE DATABASE [INCIS_SaaP_rehearsal] WITH MOVE..., RECOVERY
5 Μετρητές + fingerprints (9 πίνακες)
6 DBCC CHECKDB (rehearsal) WITH NO_INFOMSGS
7 Migrations στην rehearsal
8 Restore-based rollback rehearsal + επανέλεγχος μετρητών
9 Καθαρισμός ΜΟΝΟ rehearsal (μετά από identity check)

Επιστρέψτε τα 11 αποδεικτικά (VERIFYONLY valid, CHECKDB 0 errors, μετρητές/fingerprints,
rollback, χρόνοι/χώρος).
Προθεσμία: [D+3 — προτεινόμενη].
```

---

## Κριτήρια GO (όλα απαιτούνται)
- [ ] E1: υπογεγραμμένη επιβεβαίωση/δήλωση PO (**αυτο-δήλωση Ιδιοκτήτη — χωρίς τρίτο**).
- [ ] E2b: πλήρες 14ήμερο + 0 OBSERVATION από μη-Admin/SuperUser + 3 reports.
- [ ] E3: official run URL + SHA + artifacts + strict verifier PASS.
- [ ] E4–E6: 11 DBA αποδεικτικά.
- [ ] E7: έγκριση PO+SEC (Ιδιοκτήτης) + staged activation με monitoring/rollback.

**Καμία αποστολή/εκτέλεση δεν έχει πραγματοποιηθεί. Απόφαση: NO-GO.**
