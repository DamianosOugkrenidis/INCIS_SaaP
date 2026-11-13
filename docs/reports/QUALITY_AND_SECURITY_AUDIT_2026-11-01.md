# Quality & Security Audit — INCIS SaaP (2026-11-01)

Περιβάλλον: **αποκλειστικά τοπικό** (`LAPTOP-2K6OPG6H`, dotnet SDK 10.0.403, target `net9.0`, SQL `.\SQLEXPRESS02`).
Καμία αλλαγή παραγωγής/backup/dataset/push/release. Flags παραγωγής: `WritePolicy=false`, `Csrf=false`, `DemoToken=false`. v16 ZIP αμετάβλητο (`D517…FA7`).

---

## 0. Αποτελέσματα (σύνοψη)

| Κατηγορία | Αποτέλεσμα |
|---|---|
| xUnit | **693/693 PASS** (0 failed/skipped, 5m41s) |
| Playwright | **32/32 PASS** (0 failed/skipped, 1m39s) |
| Tool suite | **failures=0** |
| Build warnings (Debug) | **24 → 0** |
| Build warnings (Release) | **0** |
| Coverage Debug | line **88,23%** · branch 35,72% |
| Coverage Release (v16 frozen) | line **89,82%** · branch 36,01% |
| API write endpoints | **23** — inventory test PASS |
| Migrations (isolated DB) | **16 applied** (incl. 3 INCI) |
| Data integrity (isolated) | PASS (fingerprints ταυτόσημα) |
| Release build | **reproducible** (ίδιο SHA256 ×2) |
| Dependency vulnerabilities | **9 transitive packages** με advisories (βλ. §8) |

---

## 1. Εκτελεσμένες εντολές

```
dotnet build INCIS_SaaP.Tests/INCIS_SaaP.Tests.csproj -c Debug -t:Rebuild -v:n
dotnet build INCIS_SaaP/INCIS_SaaP.csproj -c Release -t:Rebuild -v:n
.\run-tests.ps1 -Coverage
.\run-playwright-tests.ps1
.\tools\tests\test-tools.ps1
dotnet list INCIS_SaaP/INCIS_SaaP.csproj package --vulnerable --include-transitive
dotnet nuget why INCIS_SaaP/INCIS_SaaP.csproj <package>
sqlcmd -S .\SQLEXPRESS02 -E -d INCIS_SaaP_inspect -Q "..."
```

---

## 2. Test κάλυψη κρίσιμων λειτουργιών

| Κρίσιμη λειτουργία | Tests |
|---|---|
| API authorization (H-1 MVC + H-2 API) | `AccessPolicyTests`, `EndpointAuthorizationTests`, `ApiWriteAccessTests`, `ApiWriteStatusCodesTests`, `MutatingEndpointTests` |
| CSRF / auth flows | `ApiCsrfTests`, `CsrfH2CombinationTests`, `ApiWritePolicyLoggingTests` |
| Import/Export INCI | `InciCatalogImportServiceTests`, `ImportServiceTests`, `ExportServiceBranchTests` |
| DB / migrations / integrity | `SqlServerIntegrationTests`, `MigrationRollbackTests` |
| ML | `MLVerificationTests`, `FormulaSuccessModelTests`, `FormulaIntelligenceServiceTests` |
| UI/UX | Playwright `WebFlowTests` + `WebFlowsExtendedTests` (32) |
| Pricing/formulas/controllers | `PriceCalculationServiceBranchTests`, controller test suites |

## 3. Coverage — γιατί Debug 88,23% ≠ Release 89,82%

Τα δύο **δεν είναι άμεσα συγκρίσιμα**· προέρχονται από διαφορετικές συνθήκες:

| Παράγοντας | Debug (τώρα) | Release (v16 frozen) |
|---|---|---|
| Configuration | Debug (`-O0`, χωρίς optimization) | Release |
| Artifact | νέο `coverage/f9e8…/coverage.cobertura.xml` | `docs/reports/ci-coverage.cobertura.xml` |
| Χρόνος | 2026-11-01 | v16 packaging (2026-10-20) |
| line / branch | 88,23% / 35,72% | 89,82% / 36,01% |

- Διαφορές προκύπτουν από **compiler optimization/inlining** (οι optimized Release assemblies εκθέτουν διαφορετικές sequence points/branches στον collector), όχι από διαφορετικό κώδικα ή tests.
- **Και τα δύο ≥ gate 70%.** Δεν συνιστάται σύγκριση των δύο αριθμών ως ένδειξη βελτίωσης/υποβάθμισης.

## 4. Build warnings — πλήρης κατάλογος & διορθώσεις

Πριν: **24 warnings** (app: 15, tests: 9). Μετά: **0**.

| # | Αρχείο(γραμμή) | Κωδικός | Αιτία | Κίνδυνος | Διόρθωση |
|---|---|---|---|---|---|
| 1 | FormulaCommands.cs(86) | CS1570 | `R&D` σε XML doc comment (undefined entity 'D') | Κανείς (comment) | `R&amp;D` |
| 2 | FormulaCode.cs(89) | CS1570 | `R&D` | Κανείς | `R&amp;D` |
| 3 | FormulaSpecifications.cs(115) | CS1570 | ` & ` σε comment | Κανείς | `&amp;` |
| 4 | ProfileController.cs(76) | CS1570 | ` & ` σε comment | Κανείς | `&amp;` |
| 5 | ApiFormulaDocumentsController.cs(59) | CS1570 | ` & ` σε comment | Κανείς | `&amp;` |
| 6 | ApiFormulaDocumentsController.cs(158) | CS1570 | ` & ` σε comment | Κανείς | `&amp;` |
| 7 | QualityAndProductionML.cs(210) | CS1570 | ` & ` σε comment | Κανείς | `&amp;` |
| 8 | INCISDbContext.cs(37) | CS8618 | non-nullable field `_httpContextAccessor` | Κανείς (ήδη `?.`) | nullable annotation `IHttpContextAccessor?` (+`?.` στη 207) |
| 9 | ResiliencePolicies.cs(155) | CS0168 | unused `ex` | Κανείς | `catch (Exception)` |
| 10 | FormulaDocumentService.cs(782) | CS8601 | `RawMaterialName = rm.ProductName` (nullable→non-null) | Κανείς (ήδη null αποδιδόταν) | DTO πεδίο → `string?` |
| 11 | FormulaDocumentService.cs(763) | CS0219 | dead `allVegan` | Κανείς | αφαιρέθηκε (dead code) |
| 12-15 | KeyManagement.cs(317,326) | CA1416 | DPAPI `ProtectedData` Windows-only | Κανείς (app Windows) | scoped `#pragma warning disable CA1416` + αιτιολόγηση |
| 16 | ApiCsrfTests.cs(63) | CS8625 | `controller: null` | Κανείς (test) | `null!` |
| 17 | ApiWritePolicyLoggingTests.cs(60) | CS8625 | `controller: null` | Κανείς (test) | `null!` |
| 18 | SqlServerIntegrationTests.cs(233) | EF1002 | `SqlQueryRaw` interpolated | Κανείς (εσωτερικές σταθερές) | scoped `#pragma warning disable EF1002` |
| 19-24 | MessageBusTests.cs(42,43,62,63), RawMaterialsControllerTests.cs(132), TwoFactorServicePureTests.cs(71) | xUnit1031 | blocking `.Result` σε tests | Κανείς (test) | `async Task` + `await` |

**Επαλήθευση:** μετά τις διορθώσεις, `-t:Rebuild` Debug = **0 warnings**, Release = **0 warnings**· πλήρες suite **ξανά 725/725 PASS**.

## 5. API authorization — 23 write endpoints

- Πηγή: `docs/H2_ENDPOINTS_TABLE.md` (23 endpoints: clients/suppliers/incis/rawmaterials/restrictions/formulas).
- **Inventory test:** `ApiWriteAccessTests.ApiExtended_WriteEndpoints_InventoryAndCurrentAuth` **PASS** — επιβεβαιώνει πλήθος = 23.
- Σημερινή κατάσταση: authenticated (cookie) επιτρέπεται· **Phase 1** προβλέπει 403 για μη-Admin/SuperUser (επιβεβαιωμένο σε isolated env: anonymous **401**, non-admin **403**, Admin 2xx).
- Production **Enforce=false** (αναμενόμενο, NO-GO).

## 6. CSRF / auth flows

- `ApiCsrfTests` (8) + `CsrfH2CombinationTests` (7): σειρά **CSRF → policy** (400 πριν 403), GET/HEAD ανεπηρέαστα, invalid token 400, anonymous 401, cookie flags/CSP.
- Cookies `SameSite=Lax` (μετριασμός). API writes **δεν** χρησιμοποιούν antiforgery· τα MVC POST (Import) **έχουν**.
- `AuthenticatedApi`/`MutatingEndpointTests`: PASS.

## 7. Migrations & data integrity (isolated DB)

Isolated test DB: **`INCIS_SaaP_inspect`** (ONLINE), restore από `Database\INCIS_SaaP.bak`· αρχικό `.bak` ανέπαφο.

| Έλεγχος | Αποτέλεσμα |
|---|---|
| Εφαρμοσμένα migrations | **16** (`…AddInciCatalogFields`, `…EnlargeInciCatalogFields`, `…AddInciFullName` = 3 INCI) |
| INCIs | **19.724** (distinct names 19.724· 100% `DataSource=ALL_INCIS_NEW_FILE`) |
| CosIngIngredients / CosIngEntries | 24.094 / 2.353 (αμετάβλητα) |
| Data integrity | PASS (`SqlServerIntegrationTests`, 9 πίνακες fingerprints) |

## 8. INCI import/export

- Importer lossless & idempotent (FullName ≤1903 χαρ., 0 απώλειες)· 2η εκτέλεση 0 inserts.
- Export tests (`ExportServiceBranchTests`) PASS· πραγματικά downloads validated στα Playwright.
- **Δεν** εκτελέστηκε import/export σε παραγωγικά δεδομένα (αποδεικτικά που λείπουν: E2b/E4–E6).

## 9. ML evaluation (false positives/negatives)

- Πραγματικό ML = **μόνο** `FormulaSuccessModel` (ML.NET SDCA Logistic Regression)· φορτώνει persisted model + πραγματικές προβλέψεις (6/6 tests).
- **Περιορισμός:** **δεν** υπάρχει ανεξάρτητο hold-out set· οι μετρικές είναι cross-validation (Accuracy/AUC/F1), όχι εξωτερικό benchmark → **FP/FN ποσοστά δεν μετρήθηκαν** τοπικά.
- Quality/Regulatory/Document/Semantic = heuristics/placeholders (καμία κανονιστική ισχύς).

## 10. Performance & memory

- **Δεν** υπάρχει load/benchmark harness στο repo → **δεν μετρήθηκαν** latency/throughput/RSS.
- Proxy: πλήρες xUnit suite 5m41s, Playwright 1m39s, Release build ~second-scale· reproducible build.
- **Εκκρεμότητα:** δεν υπάρχουν performance tests (καταγράφεται ως gap, όχι PASS).

## 11. UI/UX regressions

- Playwright **32/32 PASS** (ProductManagement 9, Documents 6, πραγματικά downloads xlsx×3/pdf).
- **Gap (M-1):** content assertions ~50 views μη πλήρεις → πιθανές μη-ανιχνευόμενες UI regressions.

## 12. Dependencies / vulnerabilities

`dotnet list package --vulnerable --include-transitive` → **9 transitive packages** με advisories (καμία direct):

| Package | Ver | Severity | Προέλευση |
|---|---|---|---|
| Microsoft.AspNetCore.WebSockets | 2.2.0 | High | `Microsoft.AspNetCore.SignalR 1.1.0` (**legacy package**) |
| System.Security.Cryptography.Xml | 9.0.0 | High | `Microsoft.AspNetCore.DataProtection 9.0.0` (+Azure) |
| System.IO.Packaging | 8.0.0 | High | `ClosedXML 0.104.1` → `DocumentFormat.OpenXml` |
| System.Security.Cryptography.Pkcs | 9.0.0 | High | (DataProtection/Azure) |
| System.Net.WebSockets.WebSocketProtocol | 4.5.1 | High | `SignalR 1.1.0` |
| Microsoft.Build | 17.10.4 | High | design-time (CodeGeneration/EF Tools) |
| OpenTelemetry.Api | 1.11.1 | Moderate | OTel 1.10.0 → ProviderBuilderExtensions 1.11.1 |
| NuGet.Packaging / NuGet.Protocol | 6.11.0 | Low | tooling (EF Tools) |
| (tests) System.Net.Http 4.3.0, System.Text.RegularExpressions 4.3.0 | — | High | test tooling |

**Εισήγηση (δεν εφαρμόστηκε — απαιτεί έγκριση γιατί αλλάζει dependencies):**
1. Αντικατάσταση `Microsoft.AspNetCore.SignalR 1.1.0` με **framework reference** (εξαλείφει WebSockets 2.2.0 / WebSocketProtocol 4.5.1).
2. Bump `Microsoft.AspNetCore.DataProtection` σε τελευταίο 9.0.x patched.
3. Bump `ClosedXML` (→ νεότερο DocumentFormat.OpenXml/System.IO.Packaging).
4. Ευθυγράμμιση OTel packages (1.11.x).

## 13. Release build & reproducibility

- Release build: **0 warnings**, 0 errors.
- Δύο διαδοχικά `-t:Rebuild` Release → **ταυτόσημο SHA256** `EF95B42F…3E0A` (deterministic build).

---

## 14. Διορθώσεις που εφαρμόστηκαν (τοπικά)

Αρχεία: `FormulaCommands.cs`, `FormulaCode.cs`, `FormulaSpecifications.cs`, `ProfileController.cs`, `ApiFormulaDocumentsController.cs`, `QualityAndProductionML.cs`, `Data/INCISDbContext.cs`, `Infrastructure/Resilience/ResiliencePolicies.cs`, `Services/FormulaDocumentService.cs`, `Dtos/FormulaDocumentViewModels.cs`, `Infrastructure/Encryption/KeyManagement.cs` (app) · `ApiCsrfTests.cs`, `ApiWritePolicyLoggingTests.cs`, `SqlServerIntegrationTests.cs`, `Architecture/MessageBusTests.cs`, `Controllers/RawMaterialsControllerTests.cs`, `Services/TwoFactorServicePureTests.cs` (tests).

**Χαρακτήρας:** comments-only, dead-code removal, nullable annotations, scoped suppressions, async-in-tests — **ουδεμία αλλαγή επιχειρησιακής λογικής**. Επαλήθευση: 725/725 PASS, 0 warnings.

---

## 15. Εκκρεμότητες & περιορισμοί

- **Εξωτερικά αποδεικτικά (δεν υπάρχουν):** E2b (production logs), E3 (official CI), E4–E6 (DBA rehearsal). — βλ. `docs/requests/EXTERNAL_EVIDENCE_TRACKER.md`.
- Vulnerable transitive packages (§12) — **δεν** εφαρμόστηκαν αλλαγές.
- Performance/memory: μη μετρήσιμα τοπικά (no harness).
- ML FP/FN: μη μετρήσιμα (no independent hold-out).
- UI content assertions (M-1), email/2FA (M-2), API POST (M-3) partial.
- Tracked-file check: NOT VERIFIED (δεν υπάρχει git repo).

---

## 16. Τελική αξιολόγηση τοπικής τεχνικής ετοιμότητας

**PASS** σε όλους τους εκτελέσιμους τοπικούς ελέγχους: 725/725 tests, 0 build warnings, ακεραιότητα δεδομένων, deterministic Release build, εργαλεία 14/14.

**Πραγματικά ευρήματα που διορθώθηκαν:** 24 build warnings → 0 (χωρίς αλλαγή συμπεριφοράς).

**Πραγματικά ευρήματα που μένουν:** vulnerabilities εξαρτήσεων (χρειάζεται έγκριση), gaps σε performance/ML/UI (§12/§15).

**Κατάσταση συστήματος:** τεχνικά αξιόπιστο τοπικά· **η απόφαση GO/NO-GO παραμένει αποκλειστικά του Ιδιοκτήτη** και εξαρτάται από τα εξωτερικά αποδεικτικά (E1/E2b/E3/E4–E6).
