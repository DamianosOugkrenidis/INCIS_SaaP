# Ανάλυση Κενών Κάλυψης (Gap Analysis)

Legend: ✅ καλύπτεται (εκτελέστηκε & πέρασε) · ◐ μερική · ⛔ δεν υλοποιήθηκε (αιτιολόγηση + πλάνο).

## 1. Κρίσιμες επιχειρησιακές λειτουργίες

| Λειτουργία | Κατάσταση | Tests |
|---|---|---|
| Υπολογισμοί τιμών (price/margins/VAT/discount) | ✅ | `PriceCalculationServiceTests` (7) + `PriceCalculationServiceBranchTests` (12) — branch 91,7% |
| Αλλεργιογόνα | ✅ | `AllergenServiceTests` (16) — branch 92,3% |
| Βαρέα μέταλλα | ✅ | `HeavyMetalComplianceServiceTests` (18) — branch 85% |
| Εισαγωγή INCI (lossless/idempotent) | ✅ | `InciCatalogImportServiceTests` (9) + SQL (3) — branch 90,4% |
| Εξαγωγές (Excel/PDF) | ✅ | `ExportServiceTests` (2) + `ExportServiceBranchTests` (3) — branch 75% |
| Import templates (Clients/Suppliers/RM/Formulas) | ✅ | `ImportServiceTests` (8) — branch 87,5% |
| CosIng Annex II–VI | ✅ | `CosIngRegressionTests` (7) + HTTP |
| Ρόλοι/πρόσβαση | ✅ | `EndpointAuthorizationTests` (21) + `ImportControllerAuthorizationTests` (3) |
| Μεταλλακτικά POST (MVC CRUD: INCIs/Clients Create/Edit/Delete) | ✅ | `MutatingEndpointTests` (6, real HTTP + persistence) |
| Μεταλλακτικά POST (API v1: ApiExtended/Production/Webhooks) | ◐ | partial (unit + HTTP no-500) — M-3 |
| Λοιπά POST CRUD (Clients/INCIs/...) | ◐ | ◐ μέσω controller unit tests + HTTP no-500 |

## 2. Σελίδες / Endpoints

| Περιοχή | GET | POST/ροές |
|---|---|---|
| Account (Login/Register/2FA/Reset) | ✅ HTTP + ✅ Playwright(login ok/fail) | ◐ (2FA/reset flows) |
| Home/Help/Legal/Privacy | ✅ HTTP | n/a |
| Clients CRUD | ✅ HTTP+unit | ◐ (Create/Edit/Delete μέσω unit) |
| Suppliers / Restrictions | ✅ HTTP+unit | ◐ |
| INCIs CRUD + search | ✅ HTTP+unit+**Playwright (Create+Search)** | ✅ create via browser |
| RawMaterials (+Samples) | ✅ HTTP+unit | ◐ |
| Formulas (+Produce/History) | ✅ HTTP+unit | ◐ |
| ProductManagement (Inci/Allergens/Claims/HM/Tox/Marketing/Price/Natural/Safety) | ✅ HTTP no-500 | ⛔ content/ροές (βλ. §3) |
| FormulaDocuments (13 έγγραφα) | ✅ HTTP no-500 | n/a (αναφορές) |
| Reports | ✅ HTTP+unit | n/a |
| Import (templates/InciCatalog) | ✅ HTTP+unit+security | ✅ CSRF/upload |
| Tools/Users/FeatureFlags/Webhooks | ✅ security roles | ◐ |
| API v1 (Api, ApiExtended, CosIng, Reports, Production, Subscription, Utility, Webhooks, Documents, Templates, Versions, Clone, Approval) | ✅ HTTP (≈120 URLs) | ◐ (mutating endpoints) |

## 3. Κενά που ΔΕΝ έχουν υλοποιηθεί (⛔) — αιτιολόγηση & πλάνο

| # | Κενό | Προτεραιότητα | Αιτιολόγηση | Πλάνο |
|---|---|---|---|---|
| 1 | Content-level assertions σε ~50 Razor views (ProductManagement υποσελίδες, Claims/Inci/Allergens, Import/Preview) | Μέτρια | Απαιτούν seeded formula + browser· καλύπτεται το «no-500» μέσω HTTP | Playwright tests με seeded formula ανά document |
| 2 | Μεταλλακτικά API POST endpoints (ApiExtended Create/Update, Production, Webhooks deliveries) | Υψηλή | Απαιτούν payload/CSRF/token state | HTTP POST tests με antiforgery-bypass fixture |
| 3 | Email flows (Register confirm, ForgotPassword, 2FA) | Μέτρια | Εξάρτηση SMTP | Mock `IEmailService` + token capture |
| 4 | Hangfire background jobs | Χαμηλή | Απενεργοποιούνται σε test | Fake `IBackgroundJobClient` |
| 5 | ML/DocumentIntelligence, SemanticSearch | Χαμηλή | Εξωτερικά/υψηλό κόστος μοντέλα | Smoke + deterministic stubs |
| 6 | ExportService PDF content (QuestPDF layout) | Χαμηλή | Μόνο PDF validity ελέγχεται | Snapshot/text extraction |

## 4. Branch coverage — τεκμηρίωση μη-κρίσιμου υπολείμματος

Το συνολικό 35,5% οφείλεται κυρίως σε:
- **Compiler-generated branches** (async/await state machines, LINQ, lambdas — π.χ. `<CloneMultipleFormulasAsync>d__6`).
- **Razor views** (`ExecuteAsync`) — markup, όχι business logic.
- **ViewModels/auto-properties** με μονομερείς διαδρομές.

Οι κρίσιμες business κλάσεις έχουν 75–92% branch (βλ. TEST_REPORT §4). Δεν κρίνεται σκόπιμο κυνήγι 100% σε generated/markup κώδικα.

## 5. Προτεραιοποιημένο σχέδιο

1. (Υψηλή) POST endpoints tests με CSRF bypass + seeded state.
2. (Μέτρια) Playwright seeded-formula ροές για ProductManagement/Documents.
3. (Μέτρια) Email/2FA mocks.
4. (Χαμηλή) Hangfire/ML smoke.
