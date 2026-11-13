# Πλάνο Ολοκλήρωσης Εκκρεμοτήτων & Κριτήρια GO

Ημερομηνία σύνταξης: 2026-10-20 · Σχετικοί ρόλοι: **PO** = Υπεύθυνος INCIS · **DEV** = IT/DevOps με GitHub · **DBA** = Διαχειριστής SQL · **SEC** = Security Officer · **RM** = Release Manager.
> Οι ημερομηνίες είναι **σχετικές (D0 = έναρξη)**. Η ανάθεση ονομάτων/ημερολογίου είναι ευθύνη του PO.

## Σύνοψη εκκρεμοτήτων → υπεύθυνος → διάρκεια
| # | Εκκρεμότητα | Υπεύθυνος | Διάρκεια | Εξαρτάται από | Αποδεικτικό |
|---|---|---|---|---|---|
| E1 | Επιβεβαίωση εξωτερικών API consumers | **PO** | D0–D2 | — | Συμπληρωμένο `EXTERNAL_API_CONSUMERS_QUESTIONNAIRE.md` (λίστα ή ρητή «δεν υπάρχουν») |
| E2 | Phase 0 logs (συλλογή & ανάλυση) | **DEV** | D0–D14 (παρατήρηση) | E1 ξεκίνησε | Πίνακας κλήσεων ανά endpoint/ρόλο/ημέρα (χωρίς secrets) |
| E3 | Επίσημο CI run | **DEV** | D1–D3 | Git repo | Run URL + artifacts (`coverage-report`, `playwright-results`, TRX) |
| E4 | Production backup + `VERIFYONLY` | **DBA** | D1–D2 | — | .bak + SHA256 + «The backup set … is valid» |
| E5 | Restore rehearsal | **DBA** | D2–D3 | E4 | Μετρητές/fingerprints + `DBCC CHECKDB 0 errors` (σε rehearsal) |
| E6 | Rollback rehearsal | **DBA** | D3 | E5 | Migration down + restore rehearsal + επανέλεγχος μετρητών |
| E7 | Αξιολόγηση & απόφαση enforcement H-2 | **SEC + PO** | D14–D15 | E1, E2 | Υπογεγραμμένη απόφαση + σχέδιο (`H2_ACTIVATION_PLAN.md`) |
| E8 | **Production enforcement** (Phase 1) | **DEV (με έγκριση SEC/PO)** | D15+ | E7, E3, E6 | `Enforce=true` + logs 403 + επαλήθευση Admin/SuperUser |
| E9 | Phase 2 οριστικοποίηση (αφαίρεση flag) | **DEV** | D22+ | E8 σταθερό | Commit + πράσινο CI |

## Αλληλουχία (gantt-σειρά)
```
D0  E1 (PO)  ─────────►
D0  E2 (DEV) ────────────────────────► (14 ημέρες παρατήρησης)
D1  E3 (DEV) ──► D3
D1  E4 (DBA) ──► D2 ──► E5 (D2–D3) ──► E6 (D3)
D14 E7 (SEC+PO) ◄── E1,E2
D15 E8 (DEV+έγκριση) ◄── E7,E3,E6
D22 E9 (DEV) ◄── E8 σταθερό 24–48h
```

## Κριτήρια GO (όλα απαιτούνται)
- [ ] **E1:** Γραπτή επιβεβαίωση consumers. Αν υπάρχουν → λίστα με ρόλο/endpoints/ιδιοκτήτη + απόφαση εξαίρεσης (χρονικά περιορισμένη/εγκεκριμένη).
- [ ] **E2:** ≥14 ημέρες καταγραφής, μηδενικές (ή εξηγημένες) μη-εξουσιοδοτημένες κλήσεις.
- [ ] **E3:** Πράσινο workflow run + artifacts (coverage gate ≥70%).
- [ ] **E4–E6:** Backup valid + rehearsal OK + rollback OK με αποδεικτικά.
- [ ] **E7:** Υπογεγραμμένη απόφαση SEC/PO.
- [ ] Καμία Critical/High ανοικτή χωρίς αποδοχή κινδύνου.
- [ ] 705 tests (673 xUnit + 32 Playwright) πράσινα μετά τις αλλαγές.

## BLOCKED λόγω μη-πρόσβασης (τι ακριβώς χρειάζεται)
| Εκκρεμότητα | Γιατί blocked εδώ | Τι να κάνει ο υπεύθυνος | Τι να παραδώσει |
|---|---|---|---|
| E3 Official CI | Δεν υπάρχει Git repo/runner· δεν γίνεται `git init/push` χωρίς συνεννόηση | `CI_SETUP_INSTRUCTIONS.md` (init/remote/push) | Run URL + artifacts |
| E4–E6 Backup/Rehearsal | Χωρίς πρόσβαση στην παραγωγή | `DBA_BACKUP_REHEARSAL_RUNBOOK.md` | Outputs §7 του runbook |
| E1 External consumers | Γνώση εκτός repo | Ερωτηματολόγιο | Λίστα ή δήλωση |
| E2 Phase 0 logs | Logs παραγωγής | `PHASE0_LOG_ANALYSIS_INSTRUCTIONS.md` | Πίνακας κλήσεων |

## Σημείωση
Το production `Api:WritePolicy:Enforce` παραμένει **false**. Καμία ενέργεια σε production πριν από E7 + E3 + E6.

---

## Πρόοδος (ενημερωμένη 2026-10-20)
| Στοιχείο | Κατάσταση | Αποδεικτικό |
|---|---|---|
| E2a Phase 0 logging mechanism | **DONE** | `ApiWritePolicyLoggingTests` 3/3 (πραγματική εκπομπή logs) |
| E4a Backup procedure (local) | **DONE** | `docs/reports/local_rehearsal_validation.log` — 68.210 pages, SHA256 |
| E5a Restore rehearsal (local) | **DONE** | restore OK, μετρητές (INCIs 19.724, CosIng 24.094/2.353), `CHECKDB 0 errors` |
| E6a Rollback rehearsal (local) | **DONE με εύρημα** | restore-based rollback ✅· **migration-down αποτυγχάνει με Name>200 (SQL 2628)** → rollback = restore-based |
| E1, E3, E4b–E6b, E2b, E7–E9 | BLOCKED / NOT STARTED | βλ. `EXECUTION_STATUS.md` |

**Ευρήματα που ενσωματώθηκαν σε script/runbook:**
1. SQL Express: `WITH COMPRESSION` αποτυγχάνει (Msg 1844) → αφαιρέθηκε.
2. Migration-down μη ασφαλές με δεδομένα `Name>200` → τεκμηριωμένη προτίμηση restore-based rollback.
