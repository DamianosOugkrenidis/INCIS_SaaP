# Risk Register — INCIS SaaP (φάση προ-παραγωγής)

Ημερομηνία: 2026-10-20 · Likelihood/Impact: Χαμηλό/Μέτριο/Υψηλό.

## Ασφάλεια API
| ID | Κίνδυνος | Πιθαν. | Επίδραση | Υφιστάμενος έλεγχος | Απομένουσα έκθεση | Ενέργεια | Κατάσταση |
|---|---|---|---|---|---|---|---|
| R1 | **API write authorization:** 23 endpoints (`ApiExtendedController`) μόνο authenticated (Phase 0) | Μέτρια | Υψηλή | cookie auth· `ApiWritePolicyFilter` (log-only) | Ρόλος `User`/οποιοσδήποτε authenticated μπορεί να γράψει **έως** Phase 1 | Ενεργοποίηση Phase 1 μετά E1/E2 (βλ. `COMPLETION_PLAN.md`) | **OPEN (High)** |
| R2 | **CSRF σε cookie-based API writes:** χωρίς antiforgery token | Χαμηλή | Μέτρια | cookies **SameSite=Lax** (`Program.cs:461`) → όχι cross-site POST· CSP | Same-site/XSS CSRF παραμένει θεωρητικά | Εξέταση antiforgery/bearer για API writes· επανεξέταση αν προστεθεί JWT | **OPEN (Medium)** — τεκμηριωμένο, `ApiWriteStatusCodesTests` |
| R3 | Bearer/JWT διαφημίζεται στο Swagger αλλά **δεν** λειτουργεί (μόνο demo token) | Χαμηλή | Χαμηλή (παραπλάνηση) | demo token endpoint → **501 by default** | — | Ενημέρωση Swagger/τεκμηρίωσης | **MITIGATED** |
| R4 | Unknown external API consumers | Άγνωστη | Υψηλή | — | Δεν αποδεικνύεται/αποκλείεται από κώδικα | E1 ερωτηματολόγιο + E2 logs | **BLOCKED** |

## ML/AI
| ID | Κίνδυνος | Πιθαν. | Επίδραση | Έλεγχος | Απομένουσα έκθεση | Ενέργεια | Κατάσταση |
|---|---|---|---|---|---|---|---|
| R5 | Heuristics/placeholders να εκληφθούν ως ML/κανονιστική αξιολόγηση (Quality/Regulatory/Document/Semantic) | Μέτρια | Μέτρια (λήψη απόφασης) | Τεκμηρίωση `ML_AI_INVENTORY.md`/`ML_VERIFICATION_REPORT.md`· ρητή δήλωση | Πιθανή παραπλάνηση χρήστη αν δεν επισημανθεί στο UI | Επισήμανση στο UI + labels | **OPEN (Medium)** |
| R6 | ML εκπαίδευση με **synthetic** δεδομένα όταν ελάχιστα real labels | Μέτρια | Μέτρια | Flagged στο metadata (`TrainingSource`)· δεν επινοεί negatives | Ποιότητα πρόβλεψης περιορισμένη | Συσσώρευση real labels· hold-out set | **OPEN (Medium)** |
| R7 | Απουσία ανεξάρτητου evaluation set | Μέτρια | Χαμηλή/Μέτρια | Cross-validation metrics | Δεν υπάρχει εξωτερικό benchmark | Ορισμός hold-out + baseline | **OPEN (Low/Medium)** |

## Λειτουργικά / παραγωγή
| ID | Κίνδυνος | Πιθαν. | Επίδραση | Έλεγχος | Ενέργεια | Κατάσταση |
|---|---|---|---|---|---|---|
| R8 | Απώλεια δεδομένων / μη αναστρέψιμη αλλαγή | Χαμηλή | Υψηλή | Migrations προσθετικά· αποδεδειγμένη ακεραιότητα (9 πίνακες) | Backup+rehearsal (E4–E6) | **BLOCKED** |
| R9 | UI content regressions σε μη καλυμμένες views (M-1) | Μέτρια | Μέτρια | no-500 + 9+6 Playwright σελίδες | Επέκταση Playwright | **OPEN** |
| R10 | Email/2FA flows μη αυτοματοποιημένα (M-2) | Μέτρια | Μέτρια | page-load | Mock `IEmailService` | **OPEN** |

## Χαρτογράφηση σε ευρήματα
- **R1** = H-2 (High) · **R2** = νέο (CSRF, Medium) · **R3** = demo token (mitigated) · **R5/R6** = αναγνώριση ML ορίων · **R9/R10** = M-1/M-2 · **R8** = backup/rollback.

## Δήλωση
Κανένα εύρημα **δεν αποκρύπτεται**. Οι εγγραφές «OPEN» απαιτούν απόφαση/ενέργεια πριν το GO· οι «BLOCKED» απαιτούν πρόσβαση/έγκριση (βλ. `COMPLETION_PLAN.md`).
