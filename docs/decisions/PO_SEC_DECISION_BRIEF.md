# PO/SEC Decision Brief — INCIS SaaP

Ημερομηνία: 2026-10-26 (ενημ. 2026-11-01) · **Ρόλοι PO και SEC: Ιδιοκτήτης Έργου (ίδιο πρόσωπο).** Οι αποφάσεις παρακάτω απαιτούν **δική σου** έγκριση· δεν εκκρεμούν λόγω τρίτου προσώπου.
Κατάσταση: **NO-GO** έως την ολοκλήρωση των αποδεικτικών και την υπογραφή των αποφάσεων.

> **Πλήρες συγκεντρωτικό πακέτο:** `docs/decisions/PO_SEC_DECISION_PACKAGE.md` (κατάσταση D1–D6, αποδεικτικά που λείπουν, αυτόνομες εργασίες, κίνδυνοι/προϋποθέσεις ανά στάδιο, τελική εικόνα ετοιμότητας).
> **Η ανάληψη των ρόλων δεν ισοδυναμεί με αυτόματη έγκριση των D1–D6.**

---

## ΜΕΡΟΣ Α — Αποφάσεις προς έγκριση

### D1 — Ενεργοποίηση CSRF enforcement (`Api:Csrf:Enforce=true`)
- **Ρόλος:** PO + SEC.
- **Τι σημαίνει:** τα cookie-authenticated API writes απαιτούν έγκυρο `X-CSRF-TOKEN`· αλλιώς **400**.
- **Προϋποθέσεις:** E1 (consumers), E2b (14ήμερα logs), E3 (official CI), E4–E6 (DBA), έλεγχος ότι οι clients μπορούν να στείλουν token.
- **Κίνδυνος αν εγκριθεί πρόωρα:** διακοπή νόμιμων API clients (400).
- **Συνέπεια αναβολής:** παραμένει το εύρημα R2 (CSRF, Medium) — μετριασμός SameSite=Lax.
- **Rollback:** `Api__Csrf__Enforce=false` + restart (άμεσο).
- **Εισήγηση:** Έγκριση **μετά** E1/E2b/E3/E4–E6 και δοκιμή σε staging.

### D2 — Ενεργοποίηση WritePolicy enforcement (`Api:WritePolicy:Enforce=true`)
- **Ρόλος:** PO + SEC.
- **Τι σημαίνει:** 23 API write endpoints → μόνο `Admin`/`SuperUser`· αλλιώς **403**.
- **Προϋποθέσεις:** E1, E2b, E3, E4–E6, D1 σταθερό 24–48h.
- **Κίνδυνος:** απόρριψη μη-admin integrations (403).
- **Συνέπεια αναβολής:** παραμένει H-2 (High) — authenticated χρήστης μπορεί να γράψει μέσω API.
- **Rollback:** `Api__WritePolicy__Enforce=false` + restart.
- **Εισήγηση:** Έγκριση μετά D1 + monitoring.

### D3 — Μακροπρόθεσμος μηχανισμός auth για API (antiforgery vs bearer)
- **Ρόλος:** SEC (με PO για συμβατότητα).
- **Επιλογές:** (A) antiforgery tokens για cookie clients (μικρό κόστος, breaking για clients), (B) bearer/JWT με πλήρες lifecycle (issue/refresh/revoke).
- **Προϋποθέσεις:** καταγραφή clients (E1), απόφαση για machine clients.
- **Κίνδυνος:** ασυνεπής πολιτική αν δεν αποφασιστεί.
- **Εισήγηση:** (A) βραχυπρόθεσμα (Phase 1), (B) ως ξεχωριστό έργο.

### D4 — Αποδοχή/απόρριψη υπολειπόμενων κινδύνων (M-1, M-2, L-1..L-5)
- **Ρόλος:** PO + SEC.
- **Περιεχόμενο:** M-1 (content assertions ~50 views), M-2 (email/2FA flows), L-1 (PDF layout), L-2 (Hangfire), L-3 (ML stubs), L-4 (branch coverage), L-5 (search index).
- **Κίνδυνος:** UI regressions / μη αυτοματοποιημένα email flows.
- **Εισήγηση:** Αποδοχή για το αρχικό GO με τεκμηριωμένο remediation plan· **M-2** πριν από επόμενη έκδοση.

### D5 — Έγκριση δημοσίευσης repository (484 candidates)
- **Ρόλος:** PO (ιδιοκτήτης repo) + SEC.
- **Προϋποθέσεις:** review `docs/reports/publish_candidates.csv`, tracked-file check σε πραγματικό repo, `EXIT=0` από `verify-repo-publish.ps1 -OwnerAuthorized`, GitHub secrets ορισμένα.
- **Κίνδυνος:** διαρροή PII/production data αν παραλειφθεί ο έλεγχος.
- **Συνέπεια αναβολής:** δεν εκτελείται E3 (official CI).
- **Εισήγηση:** Έγκριση **μόνο** μετά από πλήρη review και tracked check.

### D6 — Αποδοχή ορίων ML (heuristics vs ML)
- **Ρόλος:** PO + SEC.
- **Περιεχόμενο:** μόνο το `FormulaSuccessModel` είναι πραγματικό ML· Quality/Regulatory/Document/Semantic είναι heuristics/placeholders. Καμία κανονιστική/επιστημονική ισχύς.
- **Κίνδυνος:** παραπλάνηση χρηστών.
- **Εισήγηση:** Αποδοχή με σαφή labeling στο UI· όχι κανονιστική χρήση.

**Υπογραφή (Project Owner — PO+SEC):** ____________________  **Ημερομηνία:** __________
| D1 | D2 | D3 | D4 | D5 | D6 |
|---|---|---|---|---|---|
| ☐ Approve ☐ Defer ☐ Reject | ☐ | ☐ | ☐ | ☐ | ☐ |

---

## ΜΕΡΟΣ Β — Αντικειμενικά αποδεικτικά που λείπουν

| ID | Τι λείπει | Εξωτερικός φορέας / πρόσβαση | Αποδεικτικό |
|---|---|---|---|
| E1 | Γραπτή λίστα consumers ή δήλωση «δεν υπάρχουν» + scope | **Εσύ (PO)** — αυτο-δήλωση, χωρίς τρίτο | Υπογεγραμμένο `requests/PO_API_CONSUMERS_DECLARATION.md` |
| E2b | 14ήμερα **production** Phase 0 logs + 3 reports + completeness + deployed commit/tag | **DevOps** (production log access) | `phase0_report.md`, `phase0_summary.csv`, `phase0_by_endpoint.csv` |
| E3 | Repo + secrets + owner authorization + run URL + commit SHA + artifacts + strict verifier PASS + tracked-file check | **Repo Owner / GitHub** | Run URL, SHA, artifacts, verifier output |
| E4–E6 | Backup+SHA256, VERIFYONLY, FILELISTONLY, rehearsal restore, μετρητές/fingerprints, CHECKDB, migrations, restore-based rollback, χρόνοι/χώρος | **DBA** (production DB access) | 11 outputs (`DBA_REQUEST.md` §2) |
| E7–E9 | Υπογεγραμμένη απόφαση D1/D2 + logs 400/403 + monitoring | **Εσύ (PO+SEC)** + DEV | Flags + logs + monitoring |

> Καμία από τις παραπάνω δεν είναι ολοκληρωμένη. Η ανάληψη PO/SEC από εσένα **δεν** αντικαθιστά τα production/CI/DBA αποδεικτικά.
