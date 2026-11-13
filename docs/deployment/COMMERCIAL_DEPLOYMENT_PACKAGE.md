# COMMERCIAL DEPLOYMENT PACKAGE — INCIS SaaP (4 αρχικοί tenants)

Ημερομηνία: 2026-11-11 · Κατάσταση: **έτοιμο για staging/production εγκατάσταση** (εκκρεμούν secrets, TLS cert, backup rehearsal, official CI).
Invariants: v16 ZIP αμετάβλητο · flags `WritePolicy=false`, `Csrf=false`, `DemoToken=false` · καμία παραγωγική αλλαγή από το εργαλείο.

## 1. Τοπολογία & sizing (4 tenants, μικρό φορτίο)
| Στοιχείο | Πρόταση |
|---|---|
| App | 1 instance · Windows Server 2022 (IIS 10 + ASP.NET Core Hosting Bundle) ή Linux (Kestrel + nginx) · **4 vCPU / 16 GB / 200 GB SSD** |
| SQL | SQL Server 2022 Standard · **4 vCPU / 16 GB** · data/log σε ξεχωριστά SSD volumes |
| Δίκτυο | domain + TLS 1.2/1.3 · μόνο 443 · HTTPS redirect + HSTS |
| Backups | full daily + log every 15′ · retention 30d · offsite/immutable · RPO ≤15′ / RTO ≤4h |
| Environments | Dev / Staging / Prod (ξεχωριστά secrets & DBs) |

## 2. Secrets & ρυθμίσεις (ΠΟΤΕ σε appsettings)
| Μεταβλητή | Σκοπός |
|---|---|
| `ConnectionStrings__DefaultConnection` | SQL connection (Integrated ή SQL auth) |
| `INCIS_DB_CONNECTION_STRING` | (προαιρετικά, priority) |
| `AdminSeed__Email` / `AdminSeed__Password` | αρχικός domain admin (υποχρεωτικό password) |
| `Jwt__Secret` | ≥32 chars random |
| `DataProtection__KeysDirectory` | persistent keys (ή Blob/KeyVault σε HA) |
| `Security__EnforceHttps=true` | HTTPS redirect/HSTS/secure cookies |
| `Security__AllowSelfRegistration=false` | **invite-only** onboarding |
| `Api__WritePolicy__Enforce=false` | μέχρι έγκριση D2 |
| `Api__Csrf__Enforce=false` | μέχρι έγκριση D1 |
| `Email__SmtpHost/Port/User/Password/From` | approval/2FA emails (best-effort) |

Template: `docs/deployment/appsettings.Production.template.json` (χωρίς πραγματικά secrets).

## 3. HTTPS
- Certificate (CA/Let's Encrypt) στο IIS/nginx· `Security__EnforceHttps=true`.
- Επιβεβαίωση: redirect 301/308, HSTS header, cookies `Secure; HttpOnly; SameSite=Lax`.

## 4. SQL Server
- 1 database `INCIS_SaaP`· EF migrations αυτόματα στο startup.
- `Max Pool Size=100` (ήδη).
- Import datasets (INCI 19.724 από `ALL_INCIS_NEW_FILE`, CosIng CSV/JSON) **πριν** το onboarding.

## 5. Logging & monitoring
- Serilog file sink (`logs/incis-*.log`, retention 30).
- `/health` endpoint (SQL check).
- Alerts: 5xx rate, disk <15%, backup failure, `/health` down, login failures spike.
- Προαιρετικά OpenTelemetry σε collector.

## 6. Αυτοματοποιημένα backups
- SQL Agent job (ανά 15′ log, ημερήσιο full) → `Database/Scripts/07_scheduled_backup.sql`.
- Εβδομαδιαίο `RESTORE VERIFYONLY` + μηνιαίο restore rehearsal σε isolated DB (`06_production_backup_and_rehearsal.sql`).
- Offsite/immutable copy.

## 7. Deployment steps
1. Provision server + SQL + TLS + DNS.
2. Ρύθμιση env secrets· `Security__EnforceHttps=true`· `AllowSelfRegistration=false`.
3. Deploy artifact· `Database.Migrate()` (auto).
4. Import INCI/CosIng datasets.
5. **Smoke:** login admin → create tenant → TenantAdmin πρώτη σύνδεση/αλλαγή κωδικού → create Client/Formula → INCI search → export → approve member (browser).
6. Ενεργοποίηση monitoring/alerts.
7. Onboard tenants 1→2→3→4 σταδιακά.

## 8. Rollback
- Redeploy προηγούμενο artifact.
- DB: restore από τελευταίο verified backup (restore-based rollback· migration-down μη-καταστροφικό αλλά χάνει additive πεδία).
- Flags: άμεσο `Api__*.Enforce=false` + restart.

## 9. Εμπορική διαχείριση (τι είναι έτοιμο)
- Provider console `/Tenants`: δημιουργία/ενεργοποίηση, suspend/cancel, όρια (`MaxUsers`), ημερομηνία λήξης.
- TenantAdmin + Users console: approve/activate/deactivate (browser flow **διορθώθηκε**).
- Audit: auto-audit στο SaveChanges· AuditLogs.
- Subscription: `SubscriptionPlan`/`BillingInvoice` models + API (plans/status/invoices/subscribe) — **χωρίς** payment provider.
- **Χειροκίνητη τιμολόγηση** εφικτή τώρα (invoice records + manual collection). Payment provider = επόμενη φάση.

## 10. Κόστος λειτουργίας (ενδεικτικό, EUR/μήνα)
| Στοιχείο | Εκτίμηση |
|---|---|
| App VM (4 vCPU/16GB) | €60–120 |
| SQL Server 2022 Standard license (ή VM) | €200–400 (ή SPLA) |
| Backup storage (immutable, 30d) | €20–50 |
| TLS/DNS | €0–15 |
| Monitoring | €0–30 |
| **Σύνολο (τάξη μεγέθους)** | **~€300–600/μήνα** |

## 11. Γνωστά κενά (δεν δηλώνονται ολοκληρωμένα)
- Payment provider/tιμολόγηση αυτοματοποίηση (S-7).
- API enforcement ενεργό (S-2/S-3) — απαιτεί D1/D2 + E1/E2b/E3/E4–E6.
- Out-of-process load/soak σε staging.
- ML hold-out metrics (S-8).
