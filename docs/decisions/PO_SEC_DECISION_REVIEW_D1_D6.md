# PO/SEC Decision Review — D1–D6 (μία προς μία)

Ημερομηνία: 2026-11-01 · Βάση: **v16** (αμετάβλητο) · Flags παραγωγής: `WritePolicy=false`, `Csrf=false`, `DemoToken=false`.
> Το παρόν είναι **εισήγηση**. **Καμία απόφαση δεν έχει εφαρμοστεί.** Οι προτάσεις Approve/Defer/Reject δίνονται για δική σου απόφαση.

Υπόμνημα κωδικών: **Approve** = ενεργοποίηση (μετά τις προϋποθέσεις)· **Defer** = αναβολή έως τις προϋποθέσεις· **Reject** = μη υιοθέτηση.

---

## D1 — CSRF enforcement (`Api:Csrf:Enforce=true`)

**Υφιστάμενη τεχνική κατάσταση**
- `ApiCsrfFilter` υλοποιημένο· **Phase 0 (log-only)** σε παραγωγή (`Enforce=false`).
- Phase 1 επαληθευμένο σε απομονωμένο env: `ApiCsrfTests` **8/8**, `CsrfH2CombinationTests` **7/7**.
- Σειρά που επιβεβαιώθηκε: **CSRF → WritePolicy** (400 πριν 403)· GET/HEAD ανεπηρέαστα· invalid token 400· anonymous 401· cookie flags/CSP.
- Μετριασμός εκκρεμότητας: `SameSite=Lax` (δεν είναι πλήρης προστασία).

**Τι αλλάζει αν εγκριθεί**
- Τα **cookie-authenticated** API writes απαιτούν έγκυρο header `X-CSRF-TOKEN`· αλλιώς **400**.
- Non-cookie (αν υπάρξουν στο μέλλον με token) δεν επηρεάζονται από CSRF.

**Κίνδυνοι / επιπτώσεις / εξαρτήσεις**
- *Κίνδυνος:* διακοπή νόμιμων cookie-based clients που δεν στέλνουν token (400).
- *Εξαρτήσεις:* E1 (γνώση clients), E2b (logs), E3 (CI), E4–E6 (DBA)· ιδανικά δοκιμή σε staging με τους πραγματικούς clients.
- *Impact αν εγκριθεί λάθος:* λειτουργική διακοπή integrations.

**Προϋποθέσεις ενεργοποίησης & rollback**
- Προϋπόθεση: E1 εγγράφως (λίστα ή «δεν υπάρχουν» + scope)· 0 OBSERVATION από μη-Admin στο 14ήμερο (E2b)· CI green (E3)· DBA αποδεικτικά (E4–E6).
- Rollback: `Api__Csrf__Enforce=false` + restart (**άμεσο**).

**Εισήγηση:** **DEFER** — έως E1/E2b/E3/E4–E6 + δοκιμή staging.

---

## D2 — WritePolicy enforcement (`Api:WritePolicy:Enforce=true`)

**Υφιστάμενη τεχνική κατάσταση**
- `ApiWritePolicyFilter` υλοποιημένο· **Phase 0 (log-only)** (`Enforce=false`).
- Phase 1 επαληθευμένο: `ApiWriteAccessTests` **16/16**, `ApiWritePolicyTests` **8/8**, `ApiWriteStatusCodesTests` — anonymous **401**, non-admin **403**, Admin 2xx.
- Αφορά **23** API write endpoints· πολιτική `WriteRoles=Admin,SuperUser`.
- Το εύρημα **H-2 (High)** παραμένει ανοικτό όσο `Enforce=false`.

**Τι αλλάζει αν εγκριθεί**
- API writes → μόνο ρόλοι `Admin`/`SuperUser`· διαφορετικά **403**.

**Κίνδυνοι / επιπτώσεις / εξαρτήσεις**
- *Κίνδυνος:* απόρριψη μη-admin integrations (403).
- *Εξάρτηση:* **D1 σταθερό 24–48h** + monitoring (thresholds `GO_LIVE_PLAN.md`).
- *Impact:* κλείνει το H-2· αυστηροποιεί πρόσβαση master data.

**Προϋποθέσεις ενεργοποίησης & rollback**
- Προϋπόθεση: D1 ενεργό & σταθερό· E1/E2b/E3/E4–E6.
- Rollback: `Api__WritePolicy__Enforce=false` + restart.

**Εισήγηση:** **DEFER** — μετά D1 + monitoring.

---

## D3 — Μηχανισμός auth για API (antiforgery vs bearer/JWT)

**Υφιστάμενη τεχνική κατάσταση**
- API auth = **cookie (Identity)**· bearer/JWT **ανενεργό** (δεν υπάρχει `AddJwtBearer`).
- Το Swagger **διαφημίζει** `Bearer` → ασυμφωνία τεκμηρίωσης/υλοποίησης.

**Τι αλλάζει αν εγκριθεί**
- (A) Antiforgery tokens για cookie clients (βραχυπρόθεσμα, Phase 1) — breaking για clients που δεν στέλνουν token.
- (B) Bearer/JWT με πλήρες lifecycle (issue/refresh/revoke) — ξεχωριστό έργο.

**Κίνδυνοι / επιπτώσεις / εξαρτήσεις**
- *Κίνδυνος:* ασυνεπής πολιτική auth αν δεν αποφασιστεί· παραπλάνηση από Swagger Bearer.
- *Εξάρτηση:* E1 (καταγραφή clients / machine clients).

**Προϋποθέσεις & rollback**
- Προϋπόθεση: απόφαση μετά E1. Rollback: επιστροφή σε cookie-only + διόρθωση Swagger.

**Εισήγηση:** **APPROVE (A)** ως βραχυπρόθεσμη κατεύθυνση· **DEFER (B)** ως ξεχωριστό έργο.

---

## D4 — Αποδοχή/απόρριψη υπολειπόμενων κινδύνων

**Υφιστάμενη τεχνική κατάσταση**
- M-1 (content assertions ~50 views): μερικό· M-2 (email/2FA flows): ανοικτό· M-3 (API POST testing): μερικό.
- L-1 (PDF layout), L-2 (Hangfire), L-3 (ML stubs), L-4 (branch coverage 36,01%), L-5 (search index): ανοικτά, τεκμηριωμένα στο `OPEN_FINDINGS_ASSESSMENT.md`.

**Τι αλλάζει αν εγκριθεί**
- Αποδοχή στο αρχικό GO με **τεκμηριωμένο remediation plan**· δεν αλλάζει κώδικα.

**Κίνδυνοι / επιπτώσεις / εξαρτήσεις**
- *Κίνδυνος:* UI regressions (M-1)· μη αυτοματοποιημένα email/2FA flows (M-2).
- *Εξάρτηση:* καμία τεχνική· δικό σου risk acceptance.

**Προϋποθέσεις & «rollback»**
- Δεν υπάρχει rollback (απόφαση αποδοχής)· επανεξέταση ανά έκδοση.

**Εισήγηση:** **APPROVE με remediation plan**· **M-2 πριν την επόμενη έκδοση**.

---

## D5 — Έγκριση δημοσίευσης repository

**Υφιστάμενη τεχνική κατάσταση**
- `verify-repo-publish.ps1`: `CANDIDATES 484`, `EXCLUDED 3425`, **0 findings**, `EXIT=2 NOT_AUTHORIZED` (scan clean, όχι PASS).
- Tracked-file check **NOT VERIFIED** (δεν υπάρχει repo).
- Χωρίς gate θα δημοσιεύονταν: `INCIS_SaaP.bak` (31.46MB), logs, build outputs, coverage (140MB), credentials.

**Τι αλλάζει αν εγκριθεί**
- `git init` + scoped add/commit + push σε repo + εκτέλεση official CI (E3).

**Κίνδυνοι / επιπτώσεις / εξαρτήσεις**
- *Κίνδυνος:* διαρροή PII/production data/secrets αν παραλειφθεί ο έλεγχος.
- *Εξάρτηση:* ορισμός GitHub secrets· review `publish_candidates.csv`· branch protection.

**Προϋποθέσεις & rollback**
- Προϋπόθεση: review + `EXIT=0` με `-OwnerAuthorized` + tracked check σε πραγματικό repo.
- Rollback: `git revert`/force-with-lease σε ιδιωτικό repo πριν public exposure· άμεση ανακλήρωση secrets.

**Εισήγηση:** **DEFER** — έγκριση **μόνο** μετά από πλήρη review + tracked check (μη αναστρέψιμη δημοσίευση).

---

## D6 — Αποδοχή ορίων ML

**Υφιστάμενη τεχνική κατάσταση**
- Πραγματικό ML: **μόνο** `FormulaSuccessModel` (persisted load + πραγματικό prediction επαληθεύτηκε, `MLVerificationTests` 6).
- Quality/Regulatory/Document/Semantic = **heuristics/placeholders** (καμία κανονιστική/επιστημονική ισχύς).

**Τι αλλάζει αν εγκριθεί**
- Αποδοχή ορίων + σαφές labeling στο UI· δεν αλλάζει μοντέλα.

**Κίνδυνοι / επιπτώσεις / εξαρτήσεις**
- *Κίνδυνος:* παραπλάνηση χρηστών αν δεν υπάρξει labeling.
- *Εξάρτηση:* καμία.

**Προϋποθέσεις & «rollback»**
- Προϋπόθεση: labeling.UI rollback: αφαίρεση labels (αμελητέο).

**Εισήγηση:** **APPROVE** με σαφή labeling· **όχι** κανονιστική χρήση.

---

## Σύνοψη εισηγήσεων (προς έγκριση — ΔΕΝ εφαρμόστηκαν)

| ID | Αντικείμενο | Εισήγηση |
|---|---|---|
| D1 | CSRF enforce | **DEFER** (μετά E1/E2b/E3/E4–E6 + staging) |
| D2 | WritePolicy enforce | **DEFER** (μετά D1 + monitoring) |
| D3 | API auth mechanism | **APPROVE (A)** τώρα· **DEFER (B)** |
| D4 | Αποδοχή κινδύνων M/L | **APPROVE** με remediation plan |
| D5 | Δημοσίευση repo | **DEFER** (μετά review + tracked check) |
| D6 | Όρια ML | **APPROVE** με labeling |

> Καμία εισήγηση δεν ενεργοποιείται. Flags παραμένουν `false`· κατάσταση **NO-GO**.
