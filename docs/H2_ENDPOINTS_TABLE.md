# H-2 — Πλήρης Πίνακας API Write Endpoints

Πηγή: `INCIS_SaaP/Controllers/ApiExtendedController.cs` (`[ApiController] [Route("api/v1")] [Authorize]`).
Αυθεντικοποίηση: **cookie** (ASP.NET Identity)· **δεν** υπάρχει ενεργό JWT bearer.
Εξουσιοδότηση: global `ApiWritePolicyFilter` — Phase 0 `Enforce=false` (log-only, default) · Phase 1 `Enforce=true` (403).

| # | Method & Route | Action | Επηρεαζόμενα δεδομένα (entity) | Σημερινή εξουσιοδότηση | Προβλεπόμενη |
|---|---|---|---|---|---|
| 1 | POST `/api/v1/clients` | CreateClient | `Client` | authenticated (cookie) | Admin,SuperUser |
| 2 | PUT `/api/v1/clients/{id}` | UpdateClient | `Client` | authenticated | Admin,SuperUser |
| 3 | DELETE `/api/v1/clients/{id}` | DeleteClient | `Client` | authenticated | Admin,SuperUser |
| 4 | POST `/api/v1/suppliers` | CreateSupplier | `Supplier` | authenticated | Admin,SuperUser |
| 5 | PUT `/api/v1/suppliers/{id}` | UpdateSupplier | `Supplier` | authenticated | Admin,SuperUser |
| 6 | DELETE `/api/v1/suppliers/{id}` | DeleteSupplier | `Supplier` | authenticated | Admin,SuperUser |
| 7 | POST `/api/v1/incis` | CreateINCI | `INCI` | authenticated | Admin,SuperUser |
| 8 | PUT `/api/v1/incis/{id}` | UpdateINCI | `INCI` | authenticated | Admin,SuperUser |
| 9 | DELETE `/api/v1/incis/{id}` | DeleteINCI | `INCI` | authenticated | Admin,SuperUser |
| 10 | POST `/api/v1/rawmaterials` | CreateRawMaterial | `RawMaterial` | authenticated | Admin,SuperUser |
| 11 | PUT `/api/v1/rawmaterials/{id}` | UpdateRawMaterial | `RawMaterial` | authenticated | Admin,SuperUser |
| 12 | DELETE `/api/v1/rawmaterials/{id}` | DeleteRawMaterial | `RawMaterial` | authenticated | Admin,SuperUser |
| 13 | POST `/api/v1/rawmaterials/copy` | CopyRawMaterial | `RawMaterial` | authenticated | Admin,SuperUser |
| 14 | POST `/api/v1/rawmaterials/{id}/samples` | CreateSample | `RawMaterialSample` | authenticated | Admin,SuperUser |
| 15 | PUT `/api/v1/rawmaterials/{id}/samples/{sampleId}` | UpdateSample | `RawMaterialSample` | authenticated | Admin,SuperUser |
| 16 | DELETE `/api/v1/rawmaterials/{id}/samples/{sampleId}` | DeleteSample | `RawMaterialSample` | authenticated | Admin,SuperUser |
| 17 | POST `/api/v1/rawmaterials/{id}/change-supplier` | ChangeSupplier | `RawMaterial` | authenticated | Admin,SuperUser |
| 18 | POST `/api/v1/restrictions` | CreateRestriction | `Restriction` | authenticated | Admin,SuperUser |
| 19 | PUT `/api/v1/restrictions/{id}` | UpdateRestriction | `Restriction` | authenticated | Admin,SuperUser |
| 20 | DELETE `/api/v1/restrictions/{id}` | DeleteRestriction | `Restriction` | authenticated | Admin,SuperUser |
| 21 | POST `/api/v1/formulas/{id}/validate` | ValidateFormula | `Formula` (state) | authenticated | Admin,SuperUser |
| 22 | POST `/api/v1/formulas/{id}/invalidate` | InvalidateFormula | `Formula` (state) | authenticated | Admin,SuperUser |
| 23 | POST `/api/v1/formulas/{id}/wip` | ToggleWIP | `Formula` (state) | authenticated | Admin,SuperUser |

## Επιβεβαιωμένες συμπεριφορές (tests)
| Έλεγχος | Test | Αποτέλεσμα |
|---|---|---|
| Anonymous API write → **401** | `ApiWriteStatusCodesTests.Anonymous_ApiWrite_Returns401` | ✅ |
| Phase 1 non-admin → **403** | `ApiWriteStatusCodesTests.Phase1_NonAdmin_ApiWrite_Returns403` | ✅ |
| Phase 1 Admin → 2xx | `ApiWriteStatusCodesTests.Phase1_Admin_ApiWrite_Succeeds` | ✅ |
| Phase 0 (default) → επιτρέπεται + log | `ApiWritePolicyTests.Phase0_UserRole_IsAllowed_ButLogged` | ✅ |
| CSRF posture (API χωρίς antiforgery· MVC Import με antiforgery) | `ApiWriteStatusCodesTests.ApiWrites_DoNotUseAntiforgeryToken_MvcImportDoes` | ✅ |
| CSRF Phase 0/1 για API writes (`ApiCsrfFilter`) | `ApiCsrfTests` (8): Phase 0 log-only allow, Phase 1 400, valid token, cookie flags, CSP | ✅ |
| Πλήθος endpoints = 23 | `ApiWriteAccessTests.ApiExtended_WriteEndpoints_InventoryAndCurrentAuth` | ✅ |

## CSRF / cookie
- Cookies: `SameSite=Lax` (`Program.cs:461`) → **δεν** αποστέλλονται σε cross-site POST (μετριασμός CSRF).
- Τα API writes **δεν** χρησιμοποιούν antiforgery token (API-style). Εσωτερικά MVC POST actions (Import) **έχουν** antiforgery.
- **Σημείωση κινδύνου (τεκμηριωμένο):** αν στο μέλλον προστεθεί bearer/JWT auth, το CSRF posture αλλάζει· να επανεξεταστεί.

## Επίπτωση ενεργοποίησης Phase 1
- Εσωτερικές ροές: **καμία** (καμία εσωτερική κλήση HTTP στα API· βλ. `THIRD_PARTY_INTEGRATIONS.md`).
- Εξωτερικοί consumers: **REQUIRES ENVIRONMENT ACCESS** (ερωτηματολόγιο + Phase 0 logs).
- Production: `Enforce=false` έως έγκριση.
