# PO/SEC Decision Package — INCIS SaaP

Ημερομηνία: 2026-11-01 · Έκδοση βάσης: **v16** (`TESTING_Release_v16_20261020.zip`, SHA256 `D517D7FEF5C2E51AD0F89B5D98493E8ED806F77E521742E14D194FF9310FDFA7`, αμετάβλητο).
Απόφαση συστήματος: **NO-GO** για production enforcement & δημοσίευση.

> **Εξουσιοδότηση:** Ο Ιδιοκτήτης του Έργου κατέχει **προσωπικά** και τους δύο ρόλους, **PO** και **SEC**.
> Η τελική επιχειρησιακή (PO) και τεχνική/ασφαλείας (SEC) έγκριση ανήκει σε αυτόν.
> **Δεν αναμένεται έγκριση από τρίτο πρόσωπο** για αποφάσεις που εμπίπτουν στους ρόλους αυτούς.
> **Η ανάληψη των ρόλων ΔΕΝ συνιστά αυτόματη έγκριση των D1–D6** — κάθε απόφαση απαιτεί ρητή δική του απόφαση.

---

## 1. Κατάσταση D1–D6 (σύνοψη)

| ID | Αντικείμενο | Ρόλος | Κατάσταση | Προϋπόθεση πριν την ενεργοποίηση |
|---|---|---|---|---|
| **D1** | Ενεργοποίηση `Api:Csrf:Enforce=true` | PO + SEC | **ΑΝΟΙΚΤΗ (προς απόφαση)** | E1, E2b, E3, E4–E6 + δοκιμή σε staging |
| **D2** | Ενεργοποίηση `Api:WritePolicy:Enforce=true` | PO + SEC | **ΑΝΟΙΚΤΗ (προς απόφαση)** | D1 σταθερό 24–48h + monitoring |
| **D3** | Μηχανισμός auth για API (antiforgery vs bearer/JWT) | SEC (+PO) | **ΑΝΟΙΚΤΗ (κατεύθυνση)** | καταγραφή clients (E1) |
| **D4** | Αποδοχή/απόρριψη M-1, M-2, L-1..L-5 | PO + SEC | **ΑΝΟΙΚΤΗ (προς απόφαση)** | δικό σου review; M-2 πριν επόμενη έκδοση |
| **D5** | Έγκριση δημοσίευσης repository (484 candidates) | PO + SEC | **ΑΝΟΙΚΤΗ (προς απόφαση)** | review `publish_candidates.csv` + tracked check + secrets |
| **D6** | Αποδοχή ορίων ML (heuristics vs πραγματικό ML) | PO + SEC | **ΑΝΟΙΚΤΗ (προς απόφαση)** | labeling στο UI |

**Τρέχουσα παραγωγική κατάσταση flags (επιβεβαιωμένη, αμετάβλητη):**
`Api:WritePolicy:Enforce=false` · `Api:Csrf:Enforce=false` · `Api:DemoToken:Enabled=false`.

---

## 2. Αποφάσεις που απαιτούν τη δική σου έγκριση (τεκμηριωμένα)

### D1 — CSRF enforcement
- **Τι σημαίνει:** τα cookie-authenticated API writes απαιτούν έγκυρο `X-CSRF-TOKEN`· αλλιώς **400**.
- **Τεχνικά στοιχεία:** `ApiCsrfFilter`· σειρά **CSRF → WritePolicy** (400 πριν 403)· GET/HEAD ανεπηρέαστα· Phase 1 επαληθευμένο σε απομονωμένο env (`ApiCsrfTests` 8/8, `CsrfH2CombinationTests` 7/7).
- **Κίνδυνος αν εγκριθεί πρόωρα:** διακοπή νόμιμων cookie-based API clients (400).
- **Προϋποθέσεις:** E1 (consumers), E2b (14ήμερα logs), E3 (official CI), E4–E6 (DBA), δοκιμή σε staging ότι οι clients στέλνουν token.
- **Συνέπεια αναβολής:** παραμένει το εύρημα R2 (CSRF, Medium)· μετριασμός `SameSite=Lax`.
- **Rollback:** `Api__Csrf__Enforce=false` + restart (άμεσο).
- **Εισήγηση:** **Έγκριση μετά** E1/E2b/E3/E4–E6 και δοκιμή σε staging.

### D2 — WritePolicy enforcement
- **Τι σημαίνει:** **23** API write endpoints → μόνο `Admin`/`SuperUser`· αλλιώς **403**.
- **Τεχνικά στοιχεία:** `ApiWritePolicyFilter`· Phase 1 επαληθευμένο (`ApiWriteAccessTests` 16/16, `ApiWritePolicyTests` 8/8, `ApiWriteStatusCodesTests`: anonymous **401**, non-admin **403**, Admin 2xx).
- **Κίνδυνος αν εγκριθεί πρόωρα:** απόρριψη μη-admin integrations (403).
- **Προϋποθέσεις:** D1 σταθερό 24–48h + monitoring (thresholds στο `GO_LIVE_PLAN.md`).
- **Συνέπεια αναβολής:** παραμένει το εύρημα **H-2 (High)** — authenticated μη-admin χρήστης μπορεί να γράψει μέσω API.
- **Rollback:** `Api__WritePolicy__Enforce=false` + restart.
- **Εισήγηση:** **Έγκριση μετά** D1 + monitoring.

### D3 — Μακροπρόθεσμος μηχανισμός auth για API
- **Επιλογές:** (A) antiforgery tokens για cookie clients (μικρό κόστος, breaking για clients) — (B) bearer/JWT με πλήρες lifecycle (issue/refresh/revoke).
- **Σημείωση:** το Swagger διαφημίζει `Bearer` αλλά **δεν** υπάρχει `AddJwtBearer` → μη λειτουργικό.
- **Προϋποθέσεις:** καταγραφή clients (E1)· απόφαση για machine clients.
- **Εισήγηση:** (A) βραχυπρόθεσμα (Phase 1)· (B) ως ξεχωριστό έργο.

### D4 — Αποδοχή/απόρριψη υπολειπόμενων κινδύνων
- **Περιεχόμενο:** M-1 (content assertions ~50 views), M-2 (email/2FA flows), L-1 (PDF layout), L-2 (Hangfire), L-3 (ML stubs), L-4 (branch coverage), L-5 (search index).
- **Κίνδυνος:** UI regressions / μη αυτοματοποιημένα email flows.
- **Εισήγηση:** αποδοχή για το αρχικό GO με τεκμηριωμένο remediation plan· **M-2** πριν την επόμενη έκδοση.

### D5 — Έγκριση δημοσίευσης repository
- **Τεχνικά στοιχεία:** `verify-repo-publish.ps1` → τρέχον `CANDIDATES 484`, `EXCLUDED 3425`, **0 findings**, `EXIT=2 NOT_AUTHORIZED` (scan clean, όχι PASS).
- **Προϋποθέσεις:** review `docs/reports/publish_candidates.csv`, tracked-file check σε πραγματικό repo, `EXIT=0` με `-OwnerAuthorized`, ορισμένα GitHub secrets.
- **Κίνδυνος:** διαρροή PII/production data αν παραλειφθεί ο έλεγχος (θα δημοσιεύονταν `INCIS_SaaP.bak` 31.46MB, logs, coverage, credentials).
- **Συνέπεια αναβολής:** δεν εκτελείται E3 (official CI).
- **Εισήγηση:** έγκριση **μόνο** μετά από πλήρη review + tracked check.

### D6 — Αποδοχή ορίων ML
- **Περιεχόμενο:** μόνο το `FormulaSuccessModel` είναι πραγματικό ML· Quality/Regulatory/Document/Semantic είναι heuristics/placeholders (**καμία κανονιστική/επιστημονική ισχύς**).
- **Κίνδυνος:** παραπλάνηση χρηστών.
- **Εισήγηση:** αποδοχή με σαφές labeling στο UI· όχι κανονιστική χρήση.

---

## 3. Αντικειμενικά αποδεικτικά που εξακολουθούν να λείπουν

| ID | Τι λείπει (ακριβώς) | Εξωτερικός φορέας / τεχνική πρόσβαση | Αποδεικτικό κλεισίματος |
|---|---|---|---|
| **E1** | Γραπτή λίστα consumers **ή** δήλωση «δεν υπάρχουν» + εύρος ελέγχου | **Ο Ιδιοκτήτης (PO)** — αυτο-δήλωση (χωρίς third party) | υπογεγραμμένο `PO_API_CONSUMERS_DECLARATION.md` |
| **E2b** | 14 συνεχόμενες ημέρες **production** Phase 0 logs + 3 reports + δήλωση πληρότητας + deployed commit/tag | **DevOps** (πρόσβαση production logs) | `phase0_summary.csv`, `phase0_by_endpoint.csv`, `phase0_report.md` |
| **E3** | Push σε repo + official GitHub Actions run + artifacts + ταυτοποίηση commit SHA | **Repo Owner / GitHub** (repo + runner + secrets) | Run URL + commit SHA + artifacts + `verify-ci-artifacts.ps1 -ExpectedCommitSha <sha> -Strict` = PASS |
| **E4–E6** | Production backup (WITH CHECKSUM) + VERIFYONLY + FILELISTONLY + rehearsal restore + μετρητές/fingerprints + CHECKDB + migrations + restore-based rollback + χρόνοι/χώρος | **DBA** (πρόσβαση production DB) | 11 outputs `DBA_REQUEST.md` §2 |

> Καμία από τις παραπάνω δεν είναι ολοκληρωμένη. **Δεν** έχουν παραληφθεί production logs, official CI artifacts ή DBA αποδεικτικά.
> Η ανάληψη PO/SEC **δεν** υποκαθιστά τα production/CI/DBA αποδεικτικά (απαιτούν πραγματική πρόσβαση).

---

## 4. Εργασίες που μπορούν να ολοκληρωθούν αυτόνομα (τοπικά)

**Ολοκληρώθηκαν σε αυτόν τον κύκλο:**
- Ρόλοι PO/SEC καταγεγραμμένοι σε status ledger & docs — **δεν εμφανίζονται ως εκκρεμείς λόγω τρίτου**.
- **Έλεγχος ακεραιότητας αρχείων** μετά το v16 → `docs/FILE_INTEGRITY_CHECK_2026-11-01.md` (καμία απώλεια· ελληνικά ανέπαφα).
- Σύνταξη **τελικής δήλωσης PO (E1)** → `docs/requests/PO_API_CONSUMERS_DECLARATION.md`.
- Επαλήθευση αμετάβλητου v16 ZIP (SHA256) & διατήρησης δοκιμαστικής DB `INCIS_SaaP_inspect` (ONLINE).

**Υπολείπονται — εκτελέσιμα τοπικά, απαιτούν τη ρητή εντολή σου (ενέργειες repo, όχι push):**
- **E3 local prep:** `git init` + scoped commit (χωρίς push) + `verify-repo-publish.ps1 -OwnerAuthorized -RepoUrl <URL>` → `EXIT 0`.
- Ενημέρωση `publish_candidates.csv` μετά το commit για το τελικό review (D5).

---

## 5. Κίνδυνοι & προϋποθέσεις ανά επόμενο στάδιο

| Στάδιο | Προϋποθέσεις | Κίνδυνος | Αντιμετώπιση / Rollback |
|---|---|---|---|
| E1 δήλωση | αυτο-δήλωση PO + εύρος | ελλιπής δήλωση → λάθος βάση απόφασης | ρητή «άγνωστο» όπου δεν τεκμηριώνεται |
| E2b logs | deployed build + 14ήμερο | άκυρο 14ήμερο αν λείπουν markers | έλεγχος commit/tag πριν την έναρξη |
| E3 CI | repo+secrets+runner | CI αποτυγχάνει χωρίς secrets | ορισμός secrets πριν το run |
| E4–E6 DBA | production access | έκθεση/αλλοίωση δεδομένων | hard guards, restore σε rehearsal, DROP σχολιασμένο |
| D1/D2 enforce | E1–E6 + staging | διακοπή clients (400/403) | flags rollout + monitoring + άμεσο rollback |
| D5 publish | review + tracked check | διαρροή PII/secrets | `verify-repo-publish.ps1` gate (μπλοκάρει πάντα σε ευρήματα) |

---

## 6. Τελική εικόνα ετοιμότητας INCIS SaaP

- **Τεχνική αρτιότητα: PASS** — 725/725 tests (693 xUnit + 32 Playwright), 0 failed/skipped, coverage 89,82%.
- **Ασφάλεια: τεκμηριωμένη, μη ενεργοποιημένη** — H-1 κλειστό· H-2/CSRF Phase 0/1 έτοιμα, flags `false`.
- **Δεδομένα: ακεραιότητα επαληθευμένη** — INCI 19.724 από `ALL_INCIS_NEW_FILE`· CosIng dataset διατηρημένο.
- **Απόφαση: NO-GO** για production enforcement & δημοσίευση, έως: E1 εγγράφως, E2b 14ήμερο, E3 official CI, E4–E6 DBA, και υπογεγραμμένες D1–D6.

**Μόλις συγκεντρωθούν τα αποδεικτικά:** θα συνταχθεί **συγκεντρωτικός έλεγχος πληρότητας + τεκμηριωμένη εισήγηση GO/NO-GO** προς τελική απόφαση του Ιδιοκτήτη.

---

## 7. Υπογραφή (Project Owner — PO + SEC)

| D1 | D2 | D3 | D4 | D5 | D6 |
|---|---|---|---|---|---|
| ☐ Approve ☐ Defer ☐ Reject | ☐ | ☐ | ☐ | ☐ | ☐ |

**Ονοματεπώνυμο:** ____________________  **Ημερομηνία:** __________

> Η παρούσα έκθεση **δεν** αποτελεί έγκριση GO. Καμία μη αναστρέψιμη ή παραγωγική ενέργεια δεν εκτελείται χωρίς ρητή έγκριση.
