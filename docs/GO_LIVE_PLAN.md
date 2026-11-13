# Go-Live Plan — H-2 / CSRF (staged, με εγκρίσεις)

Κατάσταση: **production flags `false`** (`Api:WritePolicy:Enforce=false`, `Api:Csrf:Enforce=false`, `Api:DemoToken:Enabled=false`). Καμία ενεργοποίηση χωρίς τα παρακάτω.

## Στάδια
| Στάδιο | Ενέργεια | Υπεύθυνος | Προϋπόθεση | Έγκριση | Αποδεικτικό |
|---|---|---|---|---|---|
| S0 | Phase 0 (log-only) σε παραγωγή | DEV | deployed build με filters | — | logs `PHASE-0 OBSERVATION` |
| S1 | Consumers confirmation | PO | — | PO sign-off | `PO_API_CONSUMERS_DECLARATION.md` υπογεγραμμένο |
| S2 | 14ήμερα Phase 0 logs | DevOps | S0, S1 | — | `phase0_report.md` (0 observations) |
| S3 | Backup + VERIFYONLY | DBA | — | — | .bak+SHA256+valid |
| S4 | Restore rehearsal + rollback | DBA | S3 | — | outputs §2 DBA request |
| S5 | Official CI green | DEV/GitHub | repo | — | run URL + artifacts |
| S6 | Ενεργοποίηση **CSRF** (`Csrf.Enforce=true`) | DEV | S1,S2,S3,S4,S5 | **SEC+PO** | 400/allow logs, metrics |
| S7 | Ενεργοποίηση **WritePolicy** (`WritePolicy.Enforce=true`) | DEV | S6 σταθερό 24–48h | **SEC+PO** | 403/allow logs, metrics |
| S8 | Phase 2: αφαίρεση flags (μόνιμη πολιτική) | DEV | S7 σταθερό | **SEC+PO** | commit + CI green |

## Σειρά CSRF → WritePolicy
Το `ApiCsrfFilter` εκτελείται πριν το `ApiWritePolicyFilter`:
- missing/invalid token → **400**
- εξουσιοδοτημένος έλεγχος → **403**
- Επιβεβαιώνεται από `CsrfH2CombinationTests` (7/7).

## Monitoring (κατά την ενεργοποίηση)
| Μετρική | Όριο προσοχής | Ενέργεια |
|---|---|---|
| `API-CSRF DENY` (400) | > 5 σε 15′ | έλεγχος clients· εξέταση rollback |
| `API-write DENY` (403) | > 5 σε 15′ ή σε νόμιμο integration | rollback flag |
| 5xx σε `/api/v1/*` | οποιοδήποτε αύξηση | rollback |
| Επιτυχείς writes Admin/SuperUser | καμία πτώση | — |

## Rollback (άμεσο, χωρίς schema)
1. `Api__Csrf__Enforce=false` **και** `Api__WritePolicy__Enforce=false`.
2. Restart/rolling deploy.
3. Επαλήθευση: οι ροές επανέρχονται· καταγραφή περιστατικού.
> Χρονικά περιορισμένο, τεκμηριωμένο, εγκεκριμένο· όχι μόνιμη επαναφορά ευάλωτης πρόσβασης.

## Κριτήρια επιτυχίας ανά στάδιο
- S1: γραπτή δήλωση (λίστα ή «δεν υπάρχουν»).
- S2: **0** PHASE-0 observations από μη-Admin/SuperUser σε 14 ημέρες.
- S4: VERIFYONLY valid, CHECKDB 0 errors, μετρητές ταυτόσημοι, restore rollback OK.
- S5: όλα τα jobs green + coverage ≥70% + artifacts.
- S6/S7: 0 αύξηση 5xx, καμία διακοπή νόμιμων ροών για 24–48h.
