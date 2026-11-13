# Πίνακας Ιχνηλασιμότητας — Λειτουργία ↔ Tests

Legend: ✅ πλήρης κάλυψη · ◐ μερική · ⛔ κενό (με αιτιολόγηση/πλάνο).

## 1. Πρόσφατες αλλαγές (ALL_INCIS_NEW_FILE)

| Λειτουργία | Tests | Είδος | Κατ. |
|---|---|---|---|
| Import καταλόγου από ALL_INCIS_NEW_FILE (.xls/.xlsx) | `InciCatalogImportServiceTests` (9) | Unit | A |
| Αντιστοίχιση πεδίων (Name/CAS/EC/Origin/Restriction/Tox/Function) | `Import_PersistsFields_AndPreservesFullName` | Integration(SQL) | B |
| Αποφυγή διπλοεγγραφών (dedup) | `Import_DeduplicatesByName_KeepsFirst` | Unit | A |
| Διατήρηση πλήρων ονομάτων (no truncation loss) | `Import_LongName_KeepsFullNameWithoutLoss`, `Import_TwoLongNamesSharingPrefix_AreBothImported`, `RealAllIncisFile_...` | Unit + SQL | A/B |
| Idempotency | `Import_IsIdempotent_UpdatesExistingByName`, `Import_LongNames_IsIdempotent`, `Import_IsIdempotent_OnSqlServer`, `RealAllIncisFile_...` | Unit + SQL | A/B/D |
| Πλήρης εισαγωγή πραγματικού αρχείου | `RealAllIncisFile_FullImport_IsIdempotent_AndLossless` | SQL | B/D |
| Αναζήτηση Name + FullName (σύνθετα ονόματα) | `Index_SearchByFullNameTail_FindsTruncatedNameRow`, `Search_ByName_FullName_Cas_Ec_WorksOnSqlServer` | Unit + SQL | A/B/D |
| Placeholders (0/-/None) → null | `Import_TreatsPlaceholdersAsNull` | Unit | A |
| Migrations/σχήμα | `Migrations_AreApplied_AndNewMigrationsPresent`, `Schema_IncisHasExpectedColumnTypes` | SQL | B |
| Μη-παρέμβαση στα CosIng | `InciImport_DoesNotAffect_CosIng` | SQL | B/D |
| Endpoints εισαγωγής (auth/ρόλοι) | `ImportControllerAuthorizationTests` (3) | Unit | E |
| Endpoints εισαγωγής (anonymous/role/CSRF) | `EndpointAuthorizationTests` | Security(HTTP) | E |
| Επικύρωση upload (ext/size/empty) | `ImportControllerUploadValidationTests` (6) | Unit | A/E |

## 2. CosIng / Annex II–VI (παλινδρόμηση)

| Λειτουργία | Tests | Είδος | Κατ. |
|---|---|---|---|
| Search ingredients (name/CAS) | `CosIngRegressionTests.SearchIngredients_*` | Unit(controller) | D |
| Ανά function | `GetByFunction_FiltersByFunction`, `GetFunctions_ReturnsDistinctFunctions` | Unit | D |
| Annex II (prohibited) | `CheckProhibited_DetectsAnnexII` | Unit | D |
| Annex III (restricted) | `CheckRestricted_DetectsAnnexIII` | Unit | D |
| Στατιστικά | `GetStats_ReturnsCounts` | Unit | D |
| HTTP πρόσβαση (public/protected) | `EndpointAuthorizationTests` | Security | E |

## 3. Βασικές οντότητες/CRUD

| Λειτουργία | Tests | Κάλυψη |
|---|---|---|
| Clients CRUD + import | `ClientsControllerTests` (14), `ImportServiceTests` | ✅ |
| Suppliers CRUD + import | `SuppliersControllerTests` (11), `ImportServiceTests` | ✅ |
| INCIs CRUD + search | `INCIsControllerTests` (14) | ✅ |
| RawMaterials CRUD + samples | `RawMaterialsControllerTests` (16), `ImportServiceTests` | ✅ |
| Restrictions CRUD | `RestrictionsControllerTests` (11) | ✅ |
| Formulas/ProductManagement | `ProductManagementControllerTests` (13), `FormulaServiceTests`, `FormulaVersioningTemplateTests` | ◐ |
| Reports | `ReportsControllerTests` (6), `ApiReports*` (via HTTP) | ◐ |
| CosIng API | `CosIngRegressionTests`, HTTP coverage | ✅ |
| Λοιπά API controllers | `ApiControllerTests` (19), HTTP broad coverage | ◐ |

## 4. Υποδομή/Ασφάλεια/Απόδοση

| Περιοχή | Tests | Κάλυψη |
|---|---|---|
| Encryption/Masking | `EncryptionServiceTests` (10) | ✅ |
| PasswordValidator | `PasswordValidatorTests` | ✅ |
| ABAC / ZeroTrust | `AbacEngineTests`, `ZeroTrustEngineTests` | ✅ |
| Tenant resolution | `TenantServiceTests` | ✅ |
| Global exception filter | `GlobalExceptionFilterTests` | ✅ |
| Resilience policies | `ResiliencePoliciesTests` | ✅ |
| UnitOfWork/Repository | `UnitOfWorkTests`, `RepositoryTests` | ✅ |
| Domain specs/value objects | `SpecificationTests`, `DomainValueObjectsTests`, `ValidationTests` | ✅ |
| Health/Swagger/headers (HTTP) | `ApiIntegrationTests` | ✅ |
| Endpoint auth (anonymous/roles/CSRF) | `EndpointAuthorizationTests` (21 cases) | ✅ |
| Performance | `PerformanceBenchmarks` (5), SQL search timing (see report) | ◐ |

## 5. Καταγεγραμμένα κενά (⛔) — με αιτιολόγηση

| Λειτουργία/Σημείο | Λόγος μη-πλήρους αυτοματοποίησης | Πλάνο |
|---|---|---|
| Razor view σώματα (`ExecuteAsync`) για ~50 views (ProductManagement υποσελίδες, Import/Preview, Claims/Inci/Allergens) | Απαιτούν browser rendering· το HTTP-level “no 500” καλύπτεται από `AuthenticatedEndpointTests`. Το περιεχόμενο εξαρτάται από seeded formula. | Προσθήκη Playwright (βλ. report § Gaps) + content assertions με seeded formula |
| Μεταλλακτικά POST actions (CreateClient, ChangePassword, ForgotPassword, Webhook deliveries, Formula Clone multiple) | Απαιτούν έγκυρα tokens/ροές (CSRF, email) και seeded καταστάσεις | Unit/HTTP tests με `[ValidateAntiForgeryToken]` bypass fixture |
| Email flows (SMTP) | Εξάρτηση εξωτερικού SMTP | Mock `IEmailService` |
| Hangfire background jobs | Απενεργοποιούνται στα tests | Fake `IBackgroundJobClient` |

## 6. Playwright — browser flows (εκτελέστηκαν ✅)

| Ροή χρήσης | Test | Αποτέλεσμα |
|---|---|---|
| Σύνδεση (έγκυρα) | `Login_WithValidCredentials_Succeeds` | ✅ |
| Σύνδεση (λάθος κωδικός) → μήνυμα σφάλματος | `Login_WithInvalidPassword_ShowsError` | ✅ |
| Δημιουργία INCI → εμφάνιση στη λίστα → αναζήτηση | `Inci_Create_Then_AppearsInList_And_IsSearchable` | ✅ |
| Επεξεργασία INCI μέσω UI | `Inci_Edit_Flow_UpdatesRecord` | ✅ |
| Διαγραφή INCI μέσω UI (confirm) | `Inci_Delete_Flow_RemovesRecord` | ✅ |
| Αναζήτηση χωρίς αποτελέσματα (empty state) | `Inci_Search_NoResults_ShowsEmptyState` | ✅ |
| Ανώνυμος χρήστης → redirect σε login | `Anonymous_ProtectedPage_RedirectsToLogin` | ✅ |
| Φόρτωση σελίδων (Clients/Suppliers/RawMaterials/Formulas/Reports) | `AuthenticatedPages_Load_WithoutError` (5) | ✅ |

## 7. Κατάσταση ανά κατηγορία (περάστηκε vs δεν εκτελέστηκε)

| Κατηγορία | Πλήθος | Πέρασαν | Δεν εκτελέστηκαν/υλοποιήθηκαν |
|---|---|---|---|
| Unit (A) | ~490 | ✅ όλα | — |
| Integration SQL (B) | 7 | ✅ όλα | — |
| Functional HTTP (C) | ~120 URLs | ✅ όλα (no-500) | content assertions σε ~50 views (⛔ §3-κενά) |
| Functional Browser (C') | 32 | ✅ όλα | — |
| Integration HTTP mutating (CRUD POST) | 6 | ✅ όλα | — |
| Security — policy (H-1 κλειστό) | 11 | ✅ όλα (0 skipped) | — |
| Security — API writes (H-2) | 16 | ✅ όλα (0 skipped) | production enforce εκκρεμεί |
| Security — API write policy Phase 0/1 | 8 | ✅ όλα | Phase 1 enforce εκκρεμεί (prod) |
| Performance (F) | 5+ | ✅ όλα | — |

> Ό,τι δεν εκτελέστηκε/υλοποιήθηκε αναφέρεται ρητά στο `docs/GAP_ANALYSIS.md` με αιτιολόγηση και πλάνο — δεν παρουσιάζεται ως καλυμμένο.

