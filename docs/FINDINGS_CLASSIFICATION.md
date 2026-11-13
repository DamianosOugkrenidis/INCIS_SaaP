# Ταξινόμηση Ευρημάτων (Findings Classification) — v2

Ημερομηνία: 2026-10-20 · Βάσει πραγματικών αποτελεσμάτων (638 xUnit + 32 Playwright, 0 failures, 0 skipped).

| Severity | Πλήθος | Κλειστά | Ανοικτά |
|---|---|---|---|
| Critical | 0 | — | 0 |
| High | 2 | 1 (H-1) | 1 (H-2) |
| Medium | 3 | 0 | 3 |
| Low | 5 | 0 | 5 |

---

## HIGH

### H-1 — MVC POST Create/Edit/Delete χωρίς role restriction → ✅ ΚΛΕΙΣΤΟ
- **Λειτουργία:** INCIs/Clients/Suppliers/RawMaterials/Restrictions/Formulas εγγραφές.
- **Κίνδυνος:** ρόλος `User` μπορούσε να γράψει master data (μόνο authenticated).
- **Έλεγχος που υπήρχε:** `[Authorize]` + antiforgery.
- **Ενέργεια (έγινε):** `[Authorize(Roles=Admin,SuperUser)]` σε 19 POST actions· tests `AccessPolicyTests` (11/11). Καμία απώλεια λειτουργίας (Admin/SuperUser ανεπηρέαστοι).
- **Απόδειξη εφαρμοσιμότητας:** confirmations στον κώδικα (TenantAdmin/User δεν χρησιμοποιούνται σε master data) — βλ. `AUTHORIZATION_POLICY_H1.md`.

### H-2 — API write endpoints (`ApiExtendedController`, 23 endpoints) → 🔶 PHASE 1 επαληθεύτηκε (test env)· production enforce σε αναμονή
- **Λειτουργία:** `/api/v1/{clients,suppliers,incis,rawmaterials,restrictions}` POST/PUT/DELETE + `/formulas/{id}/{validate,invalidate,wip}`.
- **Κίνδυνος (production, Phase 0):** οποιοσδήποτε authenticated χρήστης (cookie-only) μπορεί να γράψει master data.
- **Υλοποίηση:** `ApiWritePolicyFilter` (`Api:WritePolicy:Enforce` default **false** = log-only· `true` = 403). Demo token → **501 by default**.
- **Phase 1 (test env):** ✅ User→403 + μη-αποθήκευση, Admin/SuperUser→OK, anonymous→denied· `ApiWriteAccessTests` **16/16 (0 skipped)**, το `H2_TARGET_UserRole_Denied_OnApiWrite` **ενεργό**.
- **Εκκρεμεί:** επιβεβαίωση εξωτερικών consumers (§9 — **μη επαληθεύσιμο από εδώ**) + έγκριση → production enforce → Phase 2 αφαίρεση flag.
- **Κατάσταση:** ευπάθεια **παραμένει στην παραγωγή** (Phase 0) — δηλώνεται ρητά.

---

## MEDIUM

### M-1 — Content-level assertions σε ~50 Razor views
ProductManagement/Documents καλύπτονται πλέον **εν μέρει** από Playwright· απομένουν λοιπές σελίδες. Έλεγχος: HTTP no-500 + Playwright subset. Πλάνο: επέκταση.

### M-2 — Email flows (Register confirm, ForgotPassword, 2FA)
Μόνο page-load. Πλάνο: mock `IEmailService`.

### M-3 — API POST endpoints πέραν των MVC
Μερική κάλυψη (unit + no-500). Πλάνο: POST tests (συνδέεται με H-2).

---

## LOW
- **L-1** PDF layout content (έλεγχος μόνο signature).
- **L-2** Hangfire jobs (disabled σε tests).
- **L-3** ML/DocumentIntelligence/SemanticSearch (smoke).
- **L-4** Branch coverage σε generated/markup (οι κρίσιμες κλάσεις 75–92%).
- **L-5** Search χωρίς full-text index (24–95 ms στα 19.7k).

## Κρίσιμες επιχειρησιακές κλάσεις (branch)
PriceCalculationService **91,7%** · AllergenService 92,3% · HeavyMetalCompliance 85% · ImportService 87,5% · InciCatalogImport 90,4% · ExportService 75%.
