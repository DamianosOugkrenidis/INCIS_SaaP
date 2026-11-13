# COMMERCIAL SaaS READINESS AUDIT — INCIS SaaP

Ημερομηνία: 2026-11-11 · Περιβάλλον ελέγχου: **αποκλειστικά τοπικό** (dotnet 10.0.403 / target net9.0, SQL `.\SQLEXPRESS02`, απομονωμένες βάσεις).
Invariants: v16 ZIP αμετάβλητο (`D517…FA7`) · flags `WritePolicy=false`, `Csrf=false`, `DemoToken=false` · καμία παραγωγική/backup/dataset αλλαγή · κανένα push/release.

Στόχος: **πλήρης εμπορική λειτουργία SaaS με 4 αρχικούς πελάτες (tenants).**

---

## 1. Πραγματική κατάσταση ανά module

| Module | Κατάσταση | Στοιχεία |
|---|---|---|
| Auth (login/logout/2FA/forgot/reset/change) | **ΛΕΙΤΟΥΡΓΕΙ** | cookie Identity· lockout 5/30min· password 12+ σύνθετος· RequireConfirmedEmail=false |
| Εγγραφή/onboarding | **ΛΕΙΤΟΥΡΓΕΙ (μετά από διόρθωση)** | η φόρμα έλειπε το υποχρεωτικό `OrganizationName` (διορθώθηκε)· πρώτος χρήστης org = TenantAdmin (auto-approved), μέλη = pending |
| Tenant management (Provider) | **ΥΛΟΠΟΙΗΘΗΚΕ ΤΩΡΑ** | νέο `TenantsController` + `Views/Tenants/Index` (λίστα/δημιουργία/suspend/limits)· nav για Admin |
| Χρήστες/ρόλοι | **ΛΕΙΤΟΥΡΓΕΙ** | Users console (έγκριση/ενεργοποίηση, tenant-scoped)· ρόλοι Admin/SuperUser/TenantAdmin/User |
| Clients / Suppliers | **ΛΕΙΤΟΥΡΓΕΙ** | MVC CRUD (role-gated → διορθώθηκε σε TenantAdmin) |
| RawMaterials | **ΛΕΙΤΟΥΡΓΕΙ** | CRUD + δείγματα/αναζήτηση |
| INCI catalog | **ΛΕΙΤΟΥΡΓΕΙ** | 19.724 εγγραφές (ALL_INCIS_NEW_FILE), importer lossless/idempotent, CosIng 24.094 |
| Formulas | **ΛΕΙΤΟΥΡΓΕΙ** | CRUD/versions/templates/approval/documents/production |
| ProductManagement | **ΛΕΙΤΟΥΡΓΕΙ** | composition/allergens/claims/heavy metals/tox/marketing/price |
| Import/Export | **ΛΕΙΤΟΥΡΓΕΙ** | Excel import/preview/result· exports (xlsx/pdf) validated |
| Reports | **ΛΕΙΤΟΥΡΓΕΙ** | Reports module |
| API v1 | **ΠΕΡΙΟΡΙΣΜΕΝΟ** | cookie auth· WritePolicy/CSRF **Phase 0 (log-only)**· `/api/v1/Api/token` demo → 501 |
| Subscription/Billing | **ΜΕΡΙΚΟ** | μοντέλα Tenant/SubscriptionPlan/BillingInvoice + API (plans/status/invoices/subscribe)· **χωρίς** πάροχο πληρωμών |
| ML | **ΠΕΡΙΟΡΙΣΜΕΝΟ** | μόνο `FormulaSuccessModel` πραγματικό ML· metrics μη αποθηκευμένα |

---

## 2. SaaP αρχιτεκτονική & multi-tenancy

- **Μοντέλο:** shared DB, shared schema, **discriminator `TenantId`**· global query filters στο `INCISDbContext`.
- **Διόρθωση κρίσιμου defect (τώρα):** το `TenantId`/`_isPrivileged` διαβάζονταν **στον constructor** του DbContext, πριν το `TenantMiddleware` γράψει το `HttpContext.Items["TenantId"]` → τα query filters ήταν **ανενεργά** στην πράξη. Έγιναν **lazy properties** → τα φίλτρα εφαρμόζονται πλέον πραγματικά.
- **Ρόλοι:** Admin/SuperUser = provider-level (bypass tenant filter, βλέπουν όλα)· TenantAdmin/User = tenant-scoped.
- **Πολιτική εγγραφής (αναθεωρήθηκε):** `AppRoles.WriteAccess = "Admin,SuperUser,TenantAdmin"` (42 gates ενημερώθηκαν)· **ο `User` παραμένει read-only** (συμβατό με το H-1 policy test).
- **Suspension:** `SubscriptionStatus ∈ {Trial,Active,Suspended,Cancelled}`· `IsActive`· ο login ελέγχει την κατάσταση/λήξη του tenant → αποκλείει χρήστες ανενεργού/λήξαντος tenant.
- **Registration:** org-match → join tenant (pending) ή create tenant (TenantAdmin, auto-approved)· όριο `MaxUsers` (default 5).

---

## 3. Multi-tenancy κατάσταση

| Πτυχή | Κατάσταση | Απόδειξη |
|---|---|---|
| Query-level απομόνωση | **ΝΑΙ** | `TenantIsolationTests` (InMemory) + 4-tenant browser test |
| Tenant stamping νέων εγγραφών | **ΝΑΙ** | `SaveChanges` → `ITenantOwned.TenantId` |
| Legacy (null-tenant) κοινοχρήστεα | ΝΑΙ (by design) | φίλτρο `e.TenantId == null` |
| Suspension blocks login | **ΝΑΙ** | 4-tenant test |
| Provider console | **ΝΑΙ (νέο)** | `Tenants/Index` |
| Cross-tenant leakage | **ΚΑΛΥΜΜΕΝΟ** | tests: tenant i δεν βλέπει clients tenant j |
| API tenant scoping | **ΚΑΤΑ RULE** | Ίδιο scoped DbContext → ίδια φίλτρα· WritePolicy Phase 0 |

---

## 4. Αποτελέσματα 4-tenant tests

**`FourTenantSaasTests.FourIndependentTenants_AreIsolated_AndSuspensionBlocksLogin` — PASS**
- Δημιουργία **4 ανεξάρτητων tenants** μέσω Provider console (TenantAdmin ο καθένας, προσωρινός κωδικός).
- Κάθε TenantAdmin: πρώτη σύνδεση → υποχρεωτική αλλαγή κωδικού → δημιουργία **δικού του Client**.
- **Απομόνωση:** κάθε tenant βλέπει **μόνο** τον δικό του client· **0** διαρροές στους άλλους 3.
- **Suspension:** αναστολή tenant 4 → **μπλοκάρεται** η σύνδεσή του· οι ενεργοί tenants συνεχίζουν κανονικά.

**`ThreeUserPilotTests.ThreeTenantAdmins_ConcurrentlyCreateInci_AllSucceed` — PASS**
- 3 TenantAdmins σε 3 tenants δημιουργούν **ταυτόχρονα** INCI → όλα αποθηκεύονται χωρίς σφάλμα concurrency.

**Approval flow:** ✅ **S-1 ΔΙΟΡΘΩΘΗΚΕ & ΕΠΑΛΗΘΕΥΤΗΚΕ ΜΕ ΠΡΑΓΜΑΤΙΚΟ BROWSER CLICK.** Αιτία: τα action buttons ήταν 32×32 με μακρύ ελληνικό κείμενο (overflow), με αποτέλεσμα το κέντρο του κουμπιού «Έγκριση» να χτυπά το γειτονικό «Απενεργοποίηση» (POST σε `/Users/ToggleActive`). Διόρθωση: text-sized buttons (`.erp-action-text`), `data-testid`, ρητό `formaction`. Tests: `UsersApprovalTests` **7/7** (real click approve/deactivate, pending cannot login, cross-tenant isolation) + `TenantApprovalTests` (server-side).

---

## 5. Security findings

| ID | Εύρημα | Sev | Κατάσταση |
|---|---|---|---|
| S-1 | Η έγκριση χρήστη **μέσω browser** | **Medium** | ✅ **CLOSED** — text-sized action buttons + `data-testid` + `formaction`· real-click tests 7/7 |
| S-2 | H-2: authenticated non-admin API writes | **High** | Phase 1 **επαληθεύτηκε σε απομονωμένο env** (44 tests· TenantAdmin πλέον allowed)· `Enforce=false` μέχρι D2 |
| S-3 | CSRF API writes | Medium | Phase 1 **επαληθεύτηκε σε απομονωμένο env** (400/allow, combo tests)· `Enforce=false` μέχρι D1 |
| S-4 | Δημόσια εγγραφή (`Security:AllowSelfRegistration=true`) | Medium | **Flag προστέθηκε** (false = invite-only) |
| S-5 | Rate limiting login: policy υπάρχει αλλά εφαρμόζεται μόνο μέσω edge (δεν τέθηκε attribute για να μη σπάσει το suite) | Low | Ανοικτό — IIS/reverse-proxy |
| S-6 | Swagger διαφημίζει Bearer χωρίς ενεργό JWT | Low | Ανοικτό |
| S-7 | Billing/payments: μόνο μοντέλα+API, **χωρίς** πάροχο | Medium (commercial) | Ανοικτό |
| S-8 | ML metrics μη αποθηκευμένα· χωρίς labeled hold-out | Medium | Ανοικτό |
| — | Vulnerabilities εξαρτήσεων | — | **9 → 0 runtime** (3 design-time) |
| — | HTTPS/HSTS· cookies HttpOnly/SameSite· antiforgery· lockout· audit | — | Ενεργά |

**Επιβεβαιώθηκε:** `User` role **δεν** έχει πρόσβαση σε write pages (`AccessPolicyTests` 13/13) ούτε στη Διαχείριση Χρηστών.

---

## 6. Performance benchmarks (τοπικά, in-process· βλ. `bench/results/`)

| Μετρική | Τιμή |
|---|---|
| `GET /health` | p50 **1,56ms** · p95 4,32 · p99 8,08 |
| `GET /Account/Login` | p50 3,57 · p95 6,30 · p99 7,52 |
| Throughput (10 parallel) | **478,6 req/s**, 0 failed |
| INCI import 19.724 rows | 48,06s (2η: idempotent) |
| SQL count/search | p50 ≤ 4,2ms |
| ML inference | p50 3µs · p95 3,2µs |
| WorkingSet (in-process) | 447,7MB |

**Περιορισμοί:** in-process· single-node· χωρίς sustained load· WorkingSet με OTel console exporter. **Δεν** αντικαθιστά out-of-process load test σε staging.

### 6β. Out-of-process load/stability smoke (2026-11-11)
Πραγματικό Kestrel + isolated SQL DB (`tools/load-test.ps1`):
| Μετρική | Τιμή |
|---|---|
| Sequential sample (50) | p50 **27 ms** · p95 **39,1 ms** · failures **0** |
| Parallel 200 @ concurrency 20 | **200/200** success · **271,5 req/s** |
| Process CPU (during) | 2,56 s |
| WorkingSet | 225,3 → **244,8 MB** |
| Startup/migrations (fresh DB) | OK (health 200) |

---

## 7. Απαιτούμενη υποδομή (συγκεκριμένη πρόταση, 4 tenants)

- **App server:** Windows Server 2022 (IIS 10 + ASP.NET Core Hosting Bundle) ή Linux + nginx/Kestrel. **4 vCPU, 16 GB RAM, 200 GB SSD** (OS+app+logs).
- **SQL Server:** SQL Server 2022 Standard, dedicated (ή ισχυρό VM) **4 vCPU, 16 GB RAM**, data/log σε ξεχωριστά volumes (SSD), `Max Pool Size=100` (ήδη), 1 database (`INCIS_SaaP`) με tenant discriminator.
- **Δίκτυο/HTTPS:** δημόσιο domain, TLS 1.2/1.3 (CA ή Let's Encrypt), HTTPS redirect + HSTS (ήδη στο pipeline όταν `EnforceHttps=true`), firewall 443 only.
- **Backups/DR:** full daily + log every 15 min, retention 30 ημέρες, offsite/immutable, **δοκιμασμένο restore** (βλ. §8)· RPO ≤15min, RTO ≤4h.
- **Monitoring:** Serilog file logs + `/health` (ήδη)· προαιρετικά OTel. Alerts σε 5xx/disk/backup failures.
- **Περιβάλλοντα:** Dev / Staging / Prod (ξεχωριστά secrets/DBs).
- **Secrets:** env vars ή Windows DPAPI / Azure KeyVault· **ποτέ** στο appsettings. `AdminSeed__Password` υποχρεωτικό.
- **Εκτίμηση φορτίου 4 tenants (μικρό):** 1 app instance αρκεί· HA σε επίπεδο DB backups. Scaling: stateless app → horizontal + shared DataProtection keys (Blob/KeyVault) + SQL.

---

## 8. Εμπορική διαχείριση (τι υπάρχει / τι λείπει)

**Υπάρχει (υλοποιημένο + δοκιμασμένο):** δημιουργία/ενεργοποίηση πελάτη, TenantAdmin provisioning, suspend/cancel, όρια χρηστών (`MaxUsers`), κατάσταση/ημερομηνία λήξης συνδρομής, tenant data model (SubscriptionPlan/BillingInvoice), audit (auto-audit στο SaveChanges).
**Νέες επαληθεύσεις (2026-11-11):** `CommercialOperationsTests` **3/3** — MaxUsers limit blocks 3rd registration, provider **SetLimits** takes effect, **audit log** records TenantAdmin Client creation (actor + payload). `FourTenantSaasTests`/`ThreeUserPilotTests` πράσινα (4 tenants, isolation, suspension/reactivation, concurrency).
**Δεν υπάρχει:** πάροχος πληρωμών/tιμολόγησης (integration), κεντρικό dashboard provider metrics ανά tenant, αυτόματη τιμολόγηση/υπενθυμίσεις.
**Δεν δηλώνεται ολοκληρωμένο:** billing/subscription automation — μόνο τα μοντέλα/API.

---

## 9. Εκκρεμή τεχνικά & επιχειρησιακά ζητήματα

1. **S-1** browser approval flow (Medium) — follow-up· workaround: console provisioning.
2. **S-2/S-3** ενεργοποίηση WritePolicy/CSRF (D1/D2) — **απαιτεί έγκριση Ιδιοκτήτη** + E1/E2b/E3/E4–E6.
3. **E2b/E3/E4–E6** εξωτερικά αποδεικτικά (production logs, official CI, DBA backup/restore) — δεν έχουν παραληφθεί.
4. **S-7** payments/τιμολόγηση (commercial).
5. **S-8** ML evaluation dataset/metrics.
6. Out-of-process load/soak test σε staging.
7. User-role write permissions (τώρα read-only by policy) — ενδεχόμενη επέκταση RBAC.
8. Test-suite count: λόγω αφαίρεσης 2 εύθραφτων browser-approval tests, η κάλυψη του approval γίνεται in-process.

---

## 10. Deployment plan

1. **Staging:** provision server+SQL, TLS cert, env secrets (`ConnectionStrings__DefaultConnection`, `AdminSeed__Email/Password`, `DataProtection__KeysDirectory`), `Security:EnforceHttps=true`, `Security:AllowSelfRegistration=false`.
2. Deploy v16+patches (Debug/Release 0 warnings, deterministic build).
3. `Database.Migrate()` αυτόματα στο startup· import INCI/CosIng datasets· δημιουργία Domain Admin.
4. **Smoke:** login, create tenant (console), TenantAdmin login→change password, create Client/Formula, INCI search, export.
5. **Backup:** full + log, **restore rehearsal** σε isolated DB (VERIFYONLY, CHECKDB 0 errors).
6. **Monitoring:** `/health` + Serilog + alerts.
7. **Rollback:** redeploy προηγούμενου artifact + DB restore από το τελευταίο backup· migration-down μη-καταστροφικό (επαληθευμένο τοπικά).
8. Onboard πελάτη 1 → 2 → 3 → 4, σταδιακά, με monitoring.

---

## 11. Τελικός πίνακας GO/NO-GO

| Κριτήριο | Κατάσταση |
|---|---|
| Multi-tenant isolation (4 tenants) | **GO** (verified) |
| Provider console / tenant provisioning | **GO** (υλοποιημένο) |
| Suspension/activation | **GO** (verified) |
| Auth/anonymous protection/RBAC | **GO για Admin/SuperUser/TenantAdmin· User read-only** |
| Tests | **GO**: xUnit **693/693**, Playwright **54/54**, 0 warnings (RC1 v17) |
| Vulnerabilities (runtime) | **GO**: 0 runtime |
| Browser approval flow (S-1) | **GO** (διορθώθηκε & real-click verified) |
| API enforcement H-2/CSRF (S-2/S-3) | **GO σε isolated env** (44 tests)· production activation pending E1/E2b/E3/E4–E6 + D1/D2 |
| Payments/billing integration (S-7) | **NO-GO (commercial)** |
| External evidence E2b/E3/E4–E6 | **NO-GO** — δεν παραλήφθηκαν |
| Out-of-process load (smoke) | **GO**: 200/200 @ C=20, 271,5 req/s, p50 27ms, RAM 245MB · sustained soak pending |

### **ΤΕΛΙΚΗ ΑΠΟΦΑΣΗ: NO-GO για πλήρη εμπορική διάθεση σε 4 πραγματικούς πελάτες.**

**Αναγκαίες ενέργειες πριν το GO (με σειρά):**
1. ~~Διόρθωση S-1 (browser approval) + επανέλεγχος~~ ✅ **DONE** (2026-11-11).
2. E2b/E3/E4–E6 (production logs, official CI, DBA backup/restore).
3. Εγκρίσεις D1/D2 και ενεργοποίηση `WritePolicy`/`Csrf` με monitoring.
4. S-7 payments/τιμολόγηση.
5. Out-of-process load/soak test.
6. Secrets/HTTPS/monitoring σε staging→production.

**Δεν προτείνεται conditional GO με απενεργοποιημένα security controls.** Η ενεργοποίηση `WritePolicy`/`Csrf` είναι τεχνικά έτοιμη και επαληθευμένη (isolated, 44 tests)· απαιτεί μόνο τις εγκρίσεις D1/D2 και τα εξωτερικά αποδεικτικά E1/E2b/E3/E4–E6 για παραγωγή.

---

## 12. Συνολική αξιολόγηση

**Ισχυρά:** πλήρης λειτουργικότητα modules· **αληθινή multi-tenant απομόνωση (Documents/Webhooks/Billing/Policies)**· Provider console με plans/renewal· RBAC· **693 xUnit + 54 Playwright**· out-of-process load smoke PASS· 0 runtime vulnerabilities· reproducible build.
**Φραγμοί εμπορικής διάθεσης:** S-7 payments provider (manual εφικτό), E2b/E3/E4–E6 εξωτερικά αποδεικτικά, sustained staging soak, ενεργοποίηση D1/D2.
**Η τελική απόφαση GO/NO-GO παραμένει αποκλειστικά του Ιδιοκτήτη.**

---

## 13. S-1 — Root cause, fix & verification (2026-11-11)

**Root cause (πραγματικό):** τα action buttons στη σελίδα `/Users` ήταν `32×32px` (`.erp-actions .btn`) με μακρύ ελληνικό κείμενο. Το κείμενο ξεχείλιζε το κουτί και το hit-testing του κέντρου του κουμπιού «Έγκριση» κατέληγε στο γειτονικό κουμπί «Απενεργοποίηση». Αποτέλεσμα: πραγματικό click → **POST `/Users/ToggleActive/{id}`** αντί `/Users/Approve/{id}` (επιβεβαιώθηκε με `elementFromPoint` + καταγραφή όλων των POST).

**Fix (ασφαλής, χωρίς αλλαγή λειτουργικότητας):**
- `Views/Users/Index.cshtml`: `data-testid` σε approve/toggle φόρμες & κουμπιά, ρητό `formaction`/`formmethod`, class `erp-action-text`.
- `wwwroot/css/odoo-erp.css`: `.erp-actions .btn.erp-action-text { width:auto; padding:0 .65rem; gap:.35rem; white-space:nowrap; }`.

**Verification (πραγματικά):**
- `UsersApprovalTests` **7/7**: real-click approve → member login OK· real-click deactivate → login blocked· pending cannot login· TenantAdmin δεν βλέπει άλλου tenant pending users.
- Full suites: **xUnit 706/706**, **Playwright 46/46** (0 failed, 0 skipped).
- `TenantApprovalTests` (server-side) παραμένει PASS.

**Εναπομένοντες blockers (ενημερωμένο):** S-2/S-3 ενεργοποίηση (απαιτεί D1/D2 + E1/E2b/E3/E4–E6), S-7 payments/τιμολόγηση (χειροκίνητη διαχείριση εφικτή τώρα), E2b/E3/E4–E6 εξωτερικά αποδεικτικά, out-of-process load/soak.

**Εμπορική ετοιμότητα 4 tenants:** multi-tenancy, provider console, RBAC, suspension, invitation-only onboarding και πλήρη business modules είναι **λειτουργικά και δοκιμασμένα**. Για **πλήρη εμπορική διάθεση** απαιτούνται: production secrets/HTTPS/monitoring, backup/restore rehearsal, official CI, και απόφαση για API enforcement. Payments μπορούν να ξεκινήσουν **χειροκίνητα** (BillingInvoice model) χωρίς πάροχο.

---

## 14. Τεχνικές διορθώσεις & επαληθεύσεις (2026-11-11, βράδυ)

### 14.1 Multi-tenant isolation — ΔΙΟΡΘΩΘΗΚΑΝ πραγματικά κενά
| Εύρημα | Διόρθωση |
|---|---|
| `ApiDocumentsController` έθετε **TenantId = 1** (hardcoded) | `Document : ITenantOwned` (nullable TenantId) + tenant context (`ITenantService`)· 400 αν δεν υπάρχει tenant |
| `ApiWebhooksController` **TenantId = 1** + `FindAsync` (bypass φίλτρων) | `WebhookSubscription : ITenantOwned` + tenant context· `FirstOrDefaultAsync` με φίλτρα· επιτρέπεται και `TenantAdmin` |
| `ApiSubscriptionController` διάβαζε τον **πρώτο ενεργό tenant** (cross-tenant leak) | `GetStatus`/`GetInvoices`/`Subscribe` χρησιμοποιούν τρέχοντα tenant· Admin μπορεί να δώσει `tenantId` |
| `BillingInvoice` χωρίς tenant filter | Προστέθηκε global query filter |
| Documents/Webhooks χωρίς query filters (IDOR) | Global filters για `Document`, `WebhookSubscription`, `WebhookDelivery` |
| Migration | `20261111141005_TenantOwnershipDocumentsWebhooks` (nullable TenantId) |

**Tests:** `DocumentWebhookIsolationTests` **4/4** (Documents/Webhooks/Billing isolation + Subscription current-tenant).

### 14.2 Provider Console — plans & renewals (ήταν ανύπαρκτα)
- `TenantsController`: **CreatePlan**, **TogglePlan**, **Renew(months)**· plan-based `MaxUsers`/`MaxStorageMB` στο Create.
- `Views/Tenants/Index.cshtml`: φόρμα/λίστα πακέτων, κουμπιά «+1/+12 μήνες».
- **Tests:** `CommercialOperationsTests` **5/5** (plan create/assign, renew extends expiry, MaxUsers limit, SetLimits, audit).

### 14.3 Formula versioning — πραγματικό restore/compare
- `CompareVersionsAsync`: πραγματικές διαφορές (top-level πεδία/συλλογές) + `FormulaVersionDetail` rows.
- `RestoreVersionAsync`: εφαρμόζει **scalar fields + raw materials + INCI ingredients** από το snapshot και δημιουργεί νέα έκδοση («Restoration»). Τα sub-documents (safety/price/marketing/allergens/heavy metals/claims) **δεν** επαναφέρονται — ρητά στο ChangeReason/Notes.
- **Tests:** `FormulaVersionRestoreTests` **1/1**, `FormulaVersioningTemplateTests` PASS.

### 14.4 API security ρόλοι
- `Api:WritePolicy:Roles` → **Admin,SuperUser,TenantAdmin** (ο TenantAdmin είναι απαραίτητος για SaaS· `Enforce=false` μέχρι D2).

### 14.5 Αποτελέσματα
- **xUnit 693/693** · **Playwright 54/54** (0 failed/skipped).
- Out-of-process load: 200/200 @ C=20, 271,5 req/s, p50 27ms, RAM 245MB.

### 14.6 Απομένοντα τεχνικά κενά (ρητά)
1. **Storage quota** (`MaxStorageMB`) δεν επιβάλλεται στο upload (μόνο MaxUsers επιβάλλεται).
2. **Feature gating πακέτων** (`HasWebhooks/HasProduction/HasReports/HasApiAccess`) δεν επιβάλλεται σε όλα τα APIs.
3. **Sustained soak** (>30′) σε staging δεν εκτελέστηκε.
4. Payments provider (εκτός τεχνικού scope· manual invoices λειτουργούν).
5. Εξωτερικά αποδεικτικά E2b/E3/E4–E6 και ενεργοποίηση D1/D2.

**Απόφαση:** τεχνικά **έτοιμο για pilot 4 tenants** μετά την επιβολή (1)+(2) ή ρητή αποδοχή τους, και **πλήρως GO** μετά E2b/E3/E4–E6 + D1/D2. Καμία παραγωγική αλλαγή/ενεργοποίηση δεν έγινε.


## 15. Release Candidate 1
Το τελικό RC report βρίσκεται στο `docs/reports/RC_FINAL_REPORT.md`. RC package: `TESTING_Release_v17_RC1_20261111.zip` (SHA256 F95491300E300ECFD99C2D57DD19C9B0C9FE2E97536F4301C76BC39DD38DE694).
