# Χάρτης Βασικών Λειτουργιών Κοσμητολογίας & Εξαρτήσεων

Ημερομηνία: 2026-10-20 · Πηγή: `INCIS_SaaP/Controllers`, `Services`, `Models`, `ML`, `Data/INCISDbContext.cs`.

## 1. Επεξεργασία & αναζήτηση συστατικών
| Στοιχείο | Τιμή |
|---|---|
| Controllers | `INCIsController` (CRUD+search Name/FullName/CAS/EC), `ApiExtendedController` (`/incis` CRUD), `ApiCosIngController` (`/api/cosing/ingredients/*`, `/check/*`), `ProductManagementController` (Inci sub-pages) |
| Services | `InciCatalogImportService` (ALL_INCIS_NEW_FILE), `ImportService`, `ExportService` |
| Models/Tables | `INCI`, `CosIngIngredient`, `CosIngEntry`, `INCIIngredient`, `Restriction` |
| API | `/api/v1/incis`, `/api/cosing/ingredients/search`, `/check/prohibited`, `/check/restricted` |
| ML | `SemanticSearchService` (TF-IDF + keyword κατηγοριοποίηση) |

## 2. Περιορισμοί & κανονιστικές πληροφορίες
| Στοιχείο | Τιμή |
|---|---|
| Controllers | `RestrictionsController`, `ApiExtendedController` (`/restrictions`), `ProductManagementController` (Regulated/Tox), `ApiCosIngController` (Annex II–VI checks) |
| Models/Tables | `Restriction`, `CosIngEntry` (Annex II–VI), `ToxicologicalData` |
| API | `/api/v1/restrictions`, `/api/cosing/check/prohibited|restricted`, `/full-check` |
| ML | `RegulatoryIntelligenceService` (**placeholder**) |

## 3. Δημιουργία/επεξεργασία/αποθήκευση φορμουλών
| Στοιχείο | Τιμή |
|---|---|
| Controllers | `FormulasController`, `ApiExtendedController` (`/formulas/*`), `ApiProductManagementController` (`/api/v1/formulas/{id}/*`), `FormulaApprovalController`, `FormulaVersionController`, `FormulaTemplateController`, `FormulaAnalysisController` |
| Services | `FormulaService`, `FormulaCloneService`, `PriceCalculationService`, `FormulaDocumentService`, `ExportService` |
| Models/Tables | `Formula`, `FormulaRawMaterial`, `FormulaVersion`, `FormulaTemplate`, `FormulaApproval`, `ProductionBatch` |
| ML | `FormulaSuccessModel` (**REAL ML.NET**) + `FormulaIntelligenceService` |

## 4. Μοτίβα προϊόντων (product patterns)
| Στοιχείο | Τιμή |
|---|---|
| Controllers | `ProductManagementController` (Safety/Allergens/Claims/HeavyMetals/Toxicology/Marketing/Price/Natural/Inci), `FormulaGroupingController`, `FormulaTemplateController` |
| Models/Tables | `ProductCategory`, `FormulaTemplate`, `Allergen`, `MarketingInfo`, `SafetyData`, `Certification`, `ToxicologicalData` |
| ML | **Δεν** εντοπίστηκε ML για «μοτίβα»· χρήση templates + ML αξιολόγησης επιτυχίας (M1/M2). `QualityPredictionService` = heuristic/placeholder |

## 5. Επίδραση του H-2 στις λειτουργίες κοσμητολογίας
| Ερώτημα | Απόδειξη | Συμπέρασμα |
|---|---|---|
| Εξαρτώνται εσωτερικές λειτουργίες από τα `ApiExtendedController` write endpoints; | Καμία εσωτερική κλήση HTTP προς `/api/v1` (`THIRD_PARTY_INTEGRATIONS.md` §2) | **Όχι** — το H-2 δεν επηρεάζει το UI/εσωτερικές ροές |
| Πώς γράφουν οι σελίδες κοσμητολογίας; | MVC controllers: `INCIsController`, `RestrictionsController`, `FormulasController`, `ProductManagementController` | Τα GET ήταν ήδη `Admin,SuperUser`· τα POST ρυθμίστηκαν στο **H-1**· `ProductManagement` writes ήδη `WriteRoles` |
| Επηρεάζει το H-2 φόρμουλες/συστατικά/περιορισμούς/μοτίβα; | Τα writes των MVC + `ApiProductManagement` (WriteRoles) παραμένουν· μόνο τα `ApiExtended` writes αποκτούν role όταν ενεργοποιηθεί το enforce | **Καμία αλλαγή** σε εσωτερικές λειτουργίες· μόνο σε **εξωτερικούς** API callers |
| Απαιτείται αφαίρεση/αλλαγή λειτουργίας για το H-2; | — | **Όχι** — καμία λειτουργία δεν αφαιρείται/απλοποιείται/αντικαθίσταται |

## 6. Τάσεις H-1/H-2
- **H-1 (κλειστό):** role restriction σε 19 MVC POST actions.
- **H-2 (Phase 0):** config-driven policy για 23 API writes· `Enforce=false` στην παραγωγή (καμία αλλαγή σε λειτουργίες κοσμητολογίας μέχρι ενεργοποίηση).
