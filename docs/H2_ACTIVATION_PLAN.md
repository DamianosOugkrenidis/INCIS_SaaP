# H-2 — Σχέδιο Ενεργοποίησης (Phase 1) & Rollback — ΠΡΟΣ ΕΓΚΡΙΣΗ

Κατάσταση: **ΠΡΟΣ ΕΓΚΡΙΣΗ** — καμία ενεργοποίηση στην παραγωγή. Το παραγωγικό config παραμένει `Api:WritePolicy:Enforce=false` (Phase 0).

## 0. Προϋποθέσεις (όλες απαιτούμενες)
- [ ] Επιβεβαίωση από τον υπεύθυνο συστήματος για τυχόν **εξωτερικούς consumers** των API write endpoints.
- [ ] Ανάλυση **Phase 0 logs** για ορισμένο διάστημα (βλ. §1) — μηδενικές μη-εξουσιοδοτημένες κλήσεις ή τεκμηριωμένη εξήγηση.
- [ ] Restore rehearsal (§3 του `06_production_backup_and_rehearsal.sql`) επιτυχές.
- [ ] Έγκριση αλλαγής config + παράθυρο.

## 1. Διαδικασία ανάλυσης Phase 0 logs
**Log marker (Serilog):** `API-write PHASE-0 OBSERVATION (no enforcement)`.
**Πεδία:** `{Method} {Path} user={User} id={UserId} requiredRoles={Roles}` — **χωρίς** secrets.
Εντολές συλλογής (παραδείγματα):
```powershell
# Από τα αρχεία logs του app (Serilog file sink: logs/incis-*.log)
Select-String -Path 'logs\incis-*.log' -Pattern 'API-write PHASE-0 OBSERVATION' |
  ForEach-Object { $_.Line } | Group-Object { ($_ -split 'user=')[1] } | Sort-Object Count -Descending
```
**Κριτήρια απόφασης:** για διάστημα ≥14 ημερών, οι κλήσεις από ρόλους εκτός `Admin/SuperUser` πρέπει να είναι **μηδενικές** ή να έχουν ταυτοποιημένη, εγκεκριμένη εξαίρεση.

## 2. Ενεργοποίηση (Phase 1) — βήματα
1. Backup παραγωγής (βλ. `06_production_backup_and_rehearsal.sql`) + `VERIFYONLY`.
2. Ορισμός `Api:WritePolicy:Enforce=true` (env var `Api__WritePolicy__Enforce=true` ή config) — **χωρίς** αλλαγή κώδικα/σχήματος.
3. Restart/rolling deploy.
4. **Επαλήθευση (εντός παραθύρου):**
   - Μη-εξουσιοδοτημένος (`User`) POST `/api/v1/incis` → **403**.
   - `Admin`/`SuperUser` → κανονική λειτουργία (2xx).
   - Anonymous → απόρριψη.
   - Log: `API-write DENY (enforced)`.
5. Παρακολούθηση 24–48h για `403` σε νόμιμες ροές.

## 2β. CSRF — ενιαία ενεργοποίηση με το H-2
Ανεξάρτητο config flag (ίδιο μοτίβο): `Api:Csrf:Enforce` (default **false**).
- **Phase 0 (τώρα):** log-only (`API-CSRF PHASE-0 OBSERVATION`), καμία αλλαγή συμπεριφοράς.
- **Phase 1:** `Api__Csrf__Enforce=true` → **400** χωρίς έγκυρο `X-CSRF-TOKEN`.
- **Σειρά:** CSRF Phase 1 **πριν ή μαζί** με H-2 Phase 1 (και τα δύο μετά E1/E2 + έγκριση).
- **Συνδυασμός:** CSRF check (400) → policy check (403). Καλύπτονται και τα δύο επίπεδα.
- Tests: `ApiCsrfTests` (8/8) — βλ. `CSRF_ASSESSMENT.md`.

**Rollback (άμεσο):** `Api__WritePolicy__Enforce=false` + restart. **Προϋπόθεση/περιορισμός:** οποιαδήποτε προσωρινή επαναφορά σε log-only είναι **χρονικά περιορισμένη, τεκμηριωμένη και εγκεκριμένη**, με **αντισταθμιστικά μέτρα** (αυξημένο monitoring, περιορισμός δικτύου/RBAC, επείγουσα επίλυση). ΔΕΝ αποτελεί μόνιμη επαναφορά ευάλωτης πρόσβασης.

## 3. Phase 2 — Οριστικοποίηση
Μετά επιτυχή περίοδο παρακολούθησης: αφαίρεση του flag και του filter-ρυθμιστή, μόνιμο `[Authorize(Roles = WriteRoles)]` στα API writes (συνέπεια με `ApiProductManagementController`).

## 4. Εξαιρέσεις
Τυχόν εξαίρεση (π.χ. συγκεκριμένος API consumer) ορίζεται **ρητά**, με συγκεκριμένο ρόλο/policy, **χρονικό όριο**, τεκμηρίωση και έγκριση — όχι αόριστη ανοχή.

## 5. Αποδεικτικά που θα συνοδεύουν την ενεργοποίηση
- Logs Phase 0 (συλλογή), πίνακας κλήσεων ανά ρόλο.
- Αποτέλεσμα επαλήθευσης 403/2xx.
- Backup + `VERIFYONLY` + rehearsal output.
- Ενημερωμένο `GO_NO_GO_REPORT.md` / `ACCEPTANCE_MATRIX.md`.
