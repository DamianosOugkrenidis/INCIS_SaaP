# Κατάλογος Συστήματος — INCIS SaaP

Έκδοση καταγραφής: 2026-10-20. Τεχνολογία: ASP.NET Core 9 (MVC + REST API) + EF Core 9 + SQL Server.

## 1. Σύνοψη μεγεθών

| Στοιχείο | Πλήθος |
|---|---|
| Controllers | 38 (337 actions) |
| Razor Views | 111 |
| Services | 21 |
| Models (οντότητες) | 29 |
| DTOs | 8 αρχεία |
| Ρόλοι | `Admin`, `SuperUser`, `User`, `TenantAdmin` |

## 2. Controllers & Endpoints (authorization)

| Controller | Class auth | Actions | Σημείωση |
|---|---|---|---|
| AccountController | authenticated | 8 | Login/Register/2FA/Reset |
| ApiController | authenticated | 13 | `/api/v1/Api` |
| ApiCosIngController | authenticated + `[AllowAnonymous]` σε search/check/stats | 10 | CosIng/Annex II–VI |
| ApiDocumentsController | authenticated | 4 | |
| ApiExtendedController | authenticated | 37 | Clients/Suppliers/INCIs/RawMaterials |
| ApiFeatureFlagsController | **Admin** | 7 | |
| ApiFormulaApprovalController | authenticated | 9 | |
| ApiFormulaCloneController | authenticated | 5 | |
| ApiFormulaDocumentsController | authenticated | 13 | |
| ApiFormulaTemplateController | authenticated | 10 | |
| ApiFormulaVersionController | authenticated | 8 | |
| ApiProductionController | authenticated | 8 | |
| ApiProductManagementController | authenticated | 28 | |
| ApiReportsController | authenticated | 8 | |
| ApiSubscriptionController | authenticated | 4 | plans δημόσιο |
| ApiUtilityController | authenticated | 6 | |
| ApiWebhooksController | **Admin,SuperUser** | 6 | |
| ClientsController | authenticated | 6 | CRUD |
| FormulaAnalysisController | authenticated | 11 | |
| FormulaApprovalController | authenticated | 6 | |
| FormulaDocumentsController | authenticated | 14 | |
| FormulaGroupingController | none | 0* | helper |
| FormulasController | authenticated | 10 | CRUD + Produce |
| FormulaTemplateController | authenticated | 5 | |
| FormulaVersionController | authenticated | 5 | |
| HelpController | none | 1 | δημόσιο |
| HomeController | none | 3 | δημόσιο |
| **ImportController** | authenticated + **Admin,SuperUser** (InciCatalog/SeedInciCatalog) | 6 | bulk import |
| INCIsController | authenticated | 7 | CRUD + AdvanceSearch (Name/FullName/CAS/EC) |
| LegalController | none | 2 | δημόσιο |
| ProductManagementController | authenticated | 39 | Inci/Allergens/Claims/… |
| ProfileController | authenticated | 3 | |
| RawMaterialsController | authenticated | 9 | CRUD + Samples |
| ReportsController | authenticated | 8 | |
| RestrictionsController | authenticated | 6 | CRUD |
| SuppliersController | authenticated | 6 | CRUD |
| ToolsController | **Admin** | 2 | |
| UsersController | **Admin,TenantAdmin** | 4 | |

\* Το FormulaGroupingController εκθέτει endpoints μέσω `[Route]` σε άλλα σημεία.

## 3. Λειτουργίες ανά κατηγορία

- **CRUD:** Clients, Suppliers, INCIs, RawMaterials, Formulas, Restrictions (+ ProductManagement υποενότητες: Inci, Allergens, Claims, HeavyMetals, Toxicology, Marketing, Price, Natural, Safety).
- **Αναζητήσεις/φίλτρα:** INCIs (Name/FullName/CAS/EC), RawMaterials (κατηγορία, προμηθευτής, samples), Formulas, Reports, CosIng search/by-function.
- **Εισαγωγές (Import):** template-based (Clients/Suppliers/INCIs/RawMaterials/Formulas) + **κατάλογος INCI από ALL_INCIS_NEW_FILE**.
- **Εξαγωγές:** ExportService (Excel), FormulaDocuments (13 τύποι εγγράφων), Reports.
- **Αναφορές:** Clients/Suppliers/INCIs/RawMaterials/Used-Unused/ExpiredSamples + API reports.
- **Ρυθμιστικά (regulatory):** CosIng Annex II–VI checks, Restricted/Prohibited, Toxicological profile.
- **Λοιπά:** Production batches, Formula versioning/approval/clone/templates, Webhooks, Feature flags, Subscription/Billing, Documents, Audit log, 2FA.

## 4. Δεδομένα & εξαρτήσεις

| Εξάρτηση | Χρήση |
|---|---|
| SQL Server | Κύρια βάση (EF Core 9, migrations) |
| Hangfire | Background jobs (απενεργοποιείται στα tests) |
| IMemoryCache / SQL distributed cache (`AppCache`) | Caching (CosIng search, functions) |
| SMTP (EmailService) | Email (2FA, reset) |
| JWT (`JwtService`) | API auth tokens |
| ASP.NET DataProtection | Κρυπτογράφηση (AES-256-GCM, `KeyManagement`) |
| CosIng CSV/JSON | Δεδομένα Annex II–VI (διατηρούνται) |
| ALL_INCIS_NEW_FILE.xls | Πηγή καταλόγου INCI |

**Οντότητες (29):** Allergen, ApplicationUser, AuditLog, Certification, Client, CosIngEntry, CosIngIngredient, Document, Formula, FormulaApproval, FormulaBatchOperation, FormulaRawMaterial, FormulaTemplate, FormulaVersion, INCI, INCIIngredient, MarketingInfo, ProductCategory, ProductionBatch, RawMaterial, Restriction, SafetyData, Supplier, SystemSettings, Tenant, ToxicologicalData, Webhook (+ PaginatedList, IOwnedEntity).

## 5. Δικαιώματα & Ασφάλεια

- Πιστοποίηση: ASP.NET Identity (cookies, MVC) + JWT (API).
- Εξουσιοδότηση: `[Authorize]` σε όλους τους business controllers· role-based για FeatureFlags/Tools/Users/Webhooks και τα import endpoints.
- CSRF: `[ValidateAntiForgeryToken]` σε POST actions του Import.
- Δημόσια (`[AllowAnonymous]`): health, swagger, subscription plans, CosIng search/check/stats, Login/Register/Legal/Help/Privacy.
