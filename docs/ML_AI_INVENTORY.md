# Αποτύπωση ML/AI Δυνατοτήτων — INCIS SaaP

Ημερομηνία: 2026-10-20 · Μέθοδος: στατική ανάλυση κώδικα & εξαρτήσεων.
Τεχνολογίες που εντοπίστηκαν: **ML.NET 3.0.1 + Microsoft.ML.Recommender 0.21.1** (`INCIS_SaaP.csproj:86-87`).
**Δεν** εντοπίστηκαν: Python, ONNX Runtime, TensorFlow, OpenAI/Azure AI, εξωτερικές AI υπηρεσίες.

Διάκριση: **REAL/ENABLED** (εκπαιδεύεται & εκτελείται στην εφαρμογή) · **HEURISTIC** (κανόνες/통계, όχι ML) · **PLACEHOLDER** (μη ολοκληρωμένο).

## 1. Πίνακας μοντέλων/υπηρεσιών

### M1 — FormulaSuccessModel (REAL/ENABLED · ML.NET)
| Πεδίο | Τιμή |
|---|---|
| 1. Όνομα/Τεχνολογία | Formula success binary classifier — **ML.NET SDCA Logistic Regression** |
| 2. Αρχεία/Κλάσεις | `ML/FormulaIntelligence/FormulaSuccessModel.cs` (`IFormulaSuccessModel`, `FormulaMlFeatures`, `FormulaMlPrediction`), `FormulaSuccessPrediction.cs` |
| 3. Είσοδος/Έξοδος/Χρήση | Input: features φόρμουλας (one-hot `ProductType` + αριθμητικά· `ExtractFeatures`). Output: πιθανότητα επιτυχίας + παράγοντες. Χρήση: πρόβλεψη/αξιολόγηση φόρμουλας (`FormulaAnalysisController`) |
| 4. Εκπαίδευση/Εκτέλεση | `pipeline.Fit` + CrossValidate (AUC/Acc/F1)· `PredictionEngine`· εκπαίδευση **εντός εφαρμογής** (όχι εξωτερική υπηρεσία) |
| 5. Εξαρτήσεις | DB: `Formulas`, `FormulaTrainingRecord`, `FormulaCombinationLog`. **Καμία** εξωτερική υπηρεσία |
| 6. Κατάσταση/Tests | **Ολοκληρωμένο**. Persist: `ML/models/formula_success.zip` (**υπάρχει** στο repo). Tests: `FormulaSuccessModelTests`, `FormulaIntelligenceServiceTests` (ελέγχει `ModelSource != "None"`), `FormulaAnalysisControllerTests` |
| 7. Περιορισμοί/Επόμενα | Synthetic augmentation όταν ελάχιστα πραγματικά labels· retrain εβδομαδιαίο (Hangfire) + on-demand (`BackgroundJobs.RetrainMlModel`, `FormulaAnalysisController.cs:100`) |

### M2 — FormulaIntelligenceService (REAL/ENABLED · orchestration + data loop)
| Πεδίο | Τιμή |
|---|---|
| 1/2 | Service orchestrator πάνω στο M1 — `ML/FormulaIntelligence/FormulaSuccessPrediction.cs` (`IFormulaIntelligenceService`) |
| 3 | Predict + **RecordOutcome** (καταγραφή πραγματικών αποτελεσμάτων → βρόχος δεδομένων). Χρήση: αξιολόγηση φόρμουλας/συστατικών |
| 4 | Χρησιμοποιεί το M1· εκπαίδευση μέσω `IFormulaSuccessModel` |
| 5 | DB: training/combination logs |
| 6 | Tests: `FormulaIntelligenceServiceTests` (`PredictFormulaSuccess_ReturnsPredictionWithModelMetadata`) |
| 7 | Εξαρτάται από συσσώρευση labels για βελτίωση |

### M3 — QualityPredictionService (HEURISTIC + PLACEHOLDER)
| Πεδίο | Τιμή |
|---|---|
| 1/2 | `ML/QualityPrediction/QualityAndProductionML.cs` (`IQualityPredictionService`, `BatchQualityPrediction`) |
| 3 | Πρόβλεψη ποιότητας παρτίδας / shelf-life / anomalies. Input: `BatchProcessParameters` |
| 4 | **Heuristic υπολογισμοί**· CV/εικόνα: **PLACEHOLDER** («would use ONNX Runtime or Python microservice», `:547`) |
| 5 | DB: `BatchQualityRecord`, `AnomalyRecord`, `ImageQCRecord`, `ShelfLifeRecord` |
| 6 | Tests: έμμεση κάλυψη (`MlServicesTests` δεν καλύπτει ρητά· **ΜΗ ΕΠΑΛΗΘΕΥΜΕΝΟ**) |
| 7 | Δεν είναι ML μοντέλο — θέλει ολοκλήρωση/απόφαση αν απαιτείται CV |

### M4 — RegulatoryIntelligenceService (PLACEHOLDER/HEURISTIC)
| Περίοδος | Τιμή |
|---|---|
| 1/2 | `ML/RegulatoryIntelligence/RegulatoryIntelligenceService.cs` (`IRegulatoryIntelligenceService`) |
| 3 | Έλεγχος κανονιστικών/CosIng αλλαγών |
| 4 | **Placeholder** που διαβάζει CosIng δεδομένα (`:503`) |
| 5 | DB: CosIng tables |
| 6 | Tests: `MlServicesTests` (smoke) |
| 7 | Δεν είναι ML |

### M5 — SemanticSearchService (HEURISTIC · TF-IDF)
| Πεδίο | Τιμή |
|---|---|
| 1/2 | `ML/SemanticSearch/SemanticSearchService.cs` (`ISemanticSearchService`) |
| 3 | Αναζήτηση/κατηγοριοποίηση συστατικών (keywords → κατηγορίες: Preservatives/UV/Emollients/…) |
| 4 | **TF-IDF + cosine similarity** (κλασικό IR) + rule-based κατηγορίες· σχόλιο για πιθανή αναβάθμιση σε embeddings (OpenAI/Ollama) — **δεν υλοποιημένη** |
| 5 | DB: INCIs/CosIng |
| 6 | Tests: `MlServicesTests.SemanticSearch_RebuildAndSearch_Run` |
| 7 | Δεν είναι trained ML μοντέλο |

### M6 — DocumentIntelligenceService (HEURISTIC · regex)
| Πεδίο | Τιμή |
|---|---|
| 1/2 | `ML/DocumentIntelligence/DocumentIntelligenceService.cs` (`IDocumentIntelligenceService`) |
| 3 | Εξαγωγή pH/Density/ShelfLife/CAS/H-statements από έγγραφα (TDS) |
| 4 | **Regex parsing** (`PHPattern`, `CASPattern`, …)· σχόλιο «would use OCR/PDF library» (`:215`) → **δεν** χρησιμοποιείται μοντέλο |
| 5 | Τοπικά αρχεία |
| 6 | Tests: κάλυψη `ExecuteAsync` = 0 hits στο coverage (**ΜΗ ΕΠΑΛΗΘΕΥΜΕΝΟ**) |
| 7 | Δεν είναι ML |

## 2. Σύνοψη
- **Πραγματικό ενεργό ML:** **Μ1 FormulaSuccessModel** (+ **M2** orchestrator) — ML.NET SDCA logistic regression, trained in-app, persisted (`formula_success.zip`), weekly/on-demand retrain, καλυμμένο από tests.
- **Non-ML (heuristic/placeholder):** M3 (CV placeholder), M4 (placeholder), M5 (TF-IDF), M6 (regex).
- **Καμία εξάρτηση** από εξωτερικά ML/AI services.
- **Χρήση σε κοσμητολογία:** αξιολόγηση/πρόβλεψη επιτυχίας φόρμουλας (M1/M2)· τα άλλα είναι υποστηρικτικά (αναζήτηση/κανονιστικά/έγγραφα).

## 3. Κενά επαλήθευσης
- M3/M4/M6: δεν υπάρχουν στοχευμένα tests → **NOT VERIFIED**.
- Δεν εντοπίστηκαν datasets/weights πέραν του `formula_success.zip` και των DB tables εκπαίδευσης.
