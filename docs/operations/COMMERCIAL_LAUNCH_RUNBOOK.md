# COMMERCIAL LAUNCH RUNBOOK — 4 πρώτοι tenants

Βάση: `TESTING_Release_v17_RC1_20261111.zip` (SHA256 `F9549…E694`). Production flags παραμένουν `false` μέχρι D1/D2.
Απαιτείται: εγκεκριμένο deployment, secrets, TLS, backup, DBA/DevOps.

## 1. Προϋποθέσεις
- [ ] Backup παραγωγής + VERIFYONLY (DBA).
- [ ] Secrets: `ConnectionStrings__DefaultConnection`, `Jwt__Secret`, `AdminSeed__Email/Password`, SMTP.
- [ ] TLS certificate· `Security__EnforceHttps=true`· `Security__AllowSelfRegistration=false`.
- [ ] Monitoring `/health` + Serilog + alerts.

## 2. Deploy εφαρμογής
1. Deploy artifact RC1.
2. `Database.Migrate()` αυτόματα στο startup (περιλαμβάνει `TenantOwnershipDocumentsWebhooks`).
3. Επαλήθευση `/health` = 200 και login domain Admin.

## 3. Δεδομένα
- Αν χρησιμοποιείται υπάρχουσα βάση: restore verified backup (καμία επανεισαγωγή).
- Αν fresh DB: import INCI (`POST /Import/SeedInciCatalog`, Admin) → 19.724· CosIng dataset μέσω εγκεκριμένης διαδικασίας (CSV/JSON)· επαλήθευση μετρητών.

## 4. Δημιουργία 4 tenants (Provider Console `/Tenants`)
Για κάθε πελάτη:
1. (Προαιρετικά) Δημιουργία πακέτου (Plans card) με MaxUsers/δυνατότητες.
2. «Νέος Πελάτης»: όνομα, email TenantAdmin, MaxUsers/plan, trialDays, activate.
3. Καταγραφή προσωρινού κωδικού (εμφανίζεται μία φορά).
4. Επαλήθευση: tenant row με σωστό plan/όρια.

## 5. TenantAdmin onboarding
1. TenantAdmin login με προσωρινό κωδικό → **αλλαγή κωδικού** (MustChangePassword).
2. `/Users`: έγκριση/ενεργοποίηση χρηστών (browser flow).
3. Έλεγχος ορίου: επιπλέον εγγραφή πάνω από MaxUsers → μήνυμα ορίου.
4. Δημιουργία Client/Formula, INCI search, export (smoke).

## 6. Έλεγχοι απομόνωσης/δικαιωμάτων
- TenantAdmin A δεν βλέπει δεδομένα B (Clients/Formulas/Documents).
- `User` δεν έχει πρόσβαση `/Users`/`/Tenants`.
- Documents/Webhooks: tenant-scoped· storage quota + HasWebhooks gating.

## 7. Backup & recovery
- Ενεργοποίηση `07_scheduled_backup.sql` (FULL daily + LOG 15′) + retention.
- Εβδομαδιαίο VERIFYONLY· μηνιαίο restore rehearsal (`06_...sql`) με CHECKDB 0 errors.
- RPO ≤15′ / RTO ≤4h.

## 8. Monitoring & errors
- `/health`, 5xx rate, login failures, `API-CSRF DENY`/`API-write DENY`, disk, backup failures.
- Runbook σφαλμάτων: 5xx → logs/`GlobalExceptionFilter`· DB down → `/health` + SQL service· backup fail → alert + manual run.

## 9. Χειροκίνητη εμπορική διαχείριση (αρχική περίοδος)
- Τιμολόγηση/πληρωμές χειροκίνητα· καταγραφή σε `BillingInvoice` (Status, DueDate, PaidAt).
- Ανανέωση: `/Tenants` → «+1/+12 μήνες»· αλλαγή πακέτου μέσω plan assignment.
- Payment provider: επόμενη φάση (δεν μπλοκάρει το launch).

## 10. Go-live checklist (ανά tenant)
- [ ] Login TenantAdmin OK, αλλαγή κωδικού.
- [ ] Χρήστες εγκρίθηκαν, όρια σωστά.
- [ ] Client/Formula/INCI/Export smoke OK.
- [ ] Απομόνωση OK vs άλλους tenants.
- [ ] Backup OK.
- [ ] Audit log καταγράφει ενέργειες.

## 11. Λειτουργικές οδηγίες
- Ρόλοι: Admin/SuperUser (provider), TenantAdmin (οργανισμός), User (χρήστης).
- Υποστήριξη: reset κωδικού μέσω Admin/SEC process· suspend/reactivate από Provider Console.
- Αλλαγές: μέσω RC release process· όχι απευθείας σε production χωρίς backup.
