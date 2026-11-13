# Εξωτερικά Αποδεικτικά — Tracker (E1, E2b, E3, E4–E6)

Ημερομηνία: 2026-11-01 · Κατάσταση: **ΚΑΝΕΝΑ ΑΠΟΔΕΙΚΤΙΚΟ ΔΕΝ ΕΧΕΙ ΠΑΡΑΛΗΦΘΕΙ.**
> Παρακολούθηση μόνο. **Απαγορεύεται** να χαρακτηριστεί ολοκληρωμένο χωρίς πραγματικό, ελεγμένο αποδεικτικό.

| ID | Απαιτούμενο αποδεικτικό (ακριβώς) | Τεχνική πρόσβαση / φορέας | Έτοιμο εργαλείο/αίτημα | Κατάσταση |
|---|---|---|---|---|
| **E1** | Υπογεγραμμένη λίστα consumers **ή** δήλωση «δεν υπάρχουν» + εύρος ελέγχου | **Ο Ιδιοκτήτης (PO)** — αυτο-δήλωση (όχι τρίτος) | `requests/PO_API_CONSUMERS_DECLARATION.md` (official), `requests/PO_REQUEST.md` (questionnaire) | **NOT RECEIVED** |
| **E2b** | `phase0_summary.csv`, `phase0_by_endpoint.csv`, `phase0_report.md` + δήλωση πληρότητας (πρώτο/τελευταίο timestamp, ημέρες, κενά) + deployed commit/tag | **DevOps** — πρόσβαση production logs | `requests/DEVOPS_REQUEST.md`, `tools/analyze-phase0-logs.ps1` | **NOT RECEIVED** |
| **E3** | Run URL + commit SHA + artifacts (`coverage-report`, `playwright-results`, TRX) + `verify-ci-artifacts.ps1 -ExpectedCommitSha <sha> -Strict` = PASS | **Repo Owner / GitHub** — repo + runner + secrets | `requests/GITHUB_CI_REQUEST.md`, `tools/verify-ci-artifacts.ps1` | **NOT RECEIVED** |
| **E4** | `.bak` + SHA256 + `RESTORE VERIFYONLY` valid | **DBA** — production DB access | `requests/DBA_REQUEST.md`, `Database/Scripts/06_…sql` | **NOT RECEIVED** |
| **E5** | `RESTORE FILELISTONLY` + rehearsal restore + μετρητές/fingerprints (9 πίνακες) + `DBCC CHECKDB` 0 errors + migrations στην rehearsal | **DBA** | `requests/DBA_REQUEST.md` | **NOT RECEIVED** |
| **E6** | Restore-based rollback rehearsal + επανέλεγχος μετρητών + χρόνοι/χώρος | **DBA** | `requests/DBA_REQUEST.md` | **NOT RECEIVED** |

## Τοπική προετοιμασία που έχει ολοκληρωθεί (χωρίς να αντικαθιστά τα παραπάνω)
- Αιτήματα redacted & έτοιμα προς αποστολή (`docs/requests/*`).
- Εργαλεία δοκιμασμένα (`tools/tests/test-tools.ps1` 14/14).
- Τοπική προσομοίωση CI & rehearsal (δεν αντικαθιστούν official CI / production DBA).
- Hard guards στο DBA script (production vs rehearsal· DROP σχολιασμένο).

## Κανόνας κλεισίματος
Κάθε γραμμή κλείνει **μόνο** με το αντίστοιχο αποδεικτικό. Μέχρι τότε: **NO-GO** για enforcement/δημοσίευση.
