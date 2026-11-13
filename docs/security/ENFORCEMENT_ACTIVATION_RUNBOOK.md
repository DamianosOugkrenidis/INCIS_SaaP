# ENFORCEMENT ACTIVATION RUNBOOK — D1 (CSRF) & D2 (WritePolicy)

Σκοπός: ασφαλής, σταδιακή ενεργοποίηση των API security controls. **Απαιτεί ρητή έγκριση Ιδιοκτήτη (PO+SEC).**
Ισχύουσα κατάσταση: `Api:Csrf:Enforce=false`, `Api:WritePolicy:Enforce=false`, `Api:DemoToken:Enabled=false`.

## Απαιτούμενα πριν το D1
- [ ] E1 (λίστα consumers / «δεν υπάρχουν»)
- [ ] E2b (14ήμερα Phase 0 logs, 0 OBSERVATION από μη-Admin/SuperUser/TenantAdmin)
- [ ] E3 (official CI run + SHA + artifacts + strict verifier)
- [ ] E4–E6 (backup/rehearsal/rollback)
- [ ] Γραπτή έγκριση D1

## D1 — CSRF enforcement
1. Backup παραγωγής + VERIFYONLY (βλ. DBA runbook).
2. Ορισμός `Api__Csrf__Enforce=true` (env) — χωρίς schema change.
3. Restart/rolling deploy.
4. **Επαλήθευση:**
   - Admin/TenantAdmin: κανονικές εγγραφές από UI/API με `X-CSRF-TOKEN` → 2xx.
   - POST χωρίς token → **400** (logs: `API-CSRF DENY (enforced)`).
   - Anonymous → **401**.
   - GET/HEAD ανεπηρέαστα.
5. **Κριτήρια επιτυχίας (24h):** 0 αύξηση 5xx· καμία διακοπή νόμιμων ροών· 400 μόνο σε μη-έγκυρα tokens.
6. **Rollback:** `Api__Csrf__Enforce=false` + restart (άμεσο).

## D2 — WritePolicy enforcement
1. Προϋπόθεση: D1 σταθερό 24–48h.
2. Ορισμός `Api__WritePolicy__Enforce=true`.
3. Restart/rolling deploy.
4. **Επαλήθευση ρόλων:**
   - `Admin`, `SuperUser`, `TenantAdmin` → 2xx σε επιτρεπόμενες εγγραφές.
   - `User` → **403** (καμία εγγραφή).
   - Anonymous → 401.
   - Cross-tenant: TenantAdmin A δεν βλέπει/εγγράφει δεδομένα B (403/404).
   - GET/HEAD ανεπηρέαστα.
5. **Κριτήρια επιτυχίας (24–48h):** 0 αύξηση 5xx· 403 μόνο σε μη-εξουσιοδοτημένες ενέργειες· καμία απώλεια λειτουργίας για Admin/TenantAdmin.
6. **Rollback:** `Api__WritePolicy__Enforce=false` + restart (άμεσο, εγκεκριμένο, χρονικά περιορισμένο).

## Monitoring
| Μετρική | Όριο | Ενέργεια |
|---|---|---|
| `API-CSRF DENY` | >5 σε 15′ | έλεγχος clients → rollback D1 |
| `API-write DENY` | >5 σε 15′ ή σε νόμιμο integration | rollback D2 |
| 5xx `/api/v1/*` | οποιαδήποτε αύξηση | rollback |

## Phase 2 (μετά σταθερότητα)
Αφαίρεση προσωρινών flags· μόνιμο `[Authorize(Roles=WriteRoles)]`· ενημέρωση docs/CI.

> Οι εντολές εκτελούνται από εξουσιοδοτημένο υπεύθυνο. Καμία ενεργοποίηση χωρίς έγκριση.
