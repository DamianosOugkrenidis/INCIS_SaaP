# Αίτημα προς DevOps — Phase 0 Log Analysis (E2b, 14 ημέρες)

**Προθεσμία (προτεινόμενη): D+14** από την έναρξη της περιόδου παρατήρησης (D0=αποστολή· προς επιβεβαίωση πραγματικής ημερομηνίας έναρξης).
**Κριτήρια αποδοχής:** 14 συνεχόμενες ημέρες logs · εκτέλεση analyzer · παράδοση των 3 report αρχείων · δήλωση πληρότητας (πρώτο/τελευταίο timestamp).

**Σκοπός:** συλλογή/ανάλυση logs ώστε να αποφασιστεί αν μπορεί να ενεργοποιηθεί το H-2/CSRF enforcement.

## 1. Προϋπόθεση — deployed build
Πριν ξεκινήσει το 14ήμερο, **επιβεβαιώστε ότι το deployed production build περιέχει** τους filters:
- `ApiWritePolicyFilter` → logs `API-write PHASE-0 OBSERVATION ...`
- `ApiCsrfFilter` → logs `API-CSRF PHASE-0 OBSERVATION ...`
Αποδεικτικό: commit/tag του deployed build + ένα δείγμα log line (χωρίς ευαίσθητα). Αν τα filters δεν υπάρχουν, το 14ήμερο **δεν ξεκινά** (θα ήταν άκυρο).

## 2. Ενέργειες
1. **Επιβεβαιώστε** την έκδοση (commit/tag) που παράγει τα δύο log markers.
2. **Διατηρήστε** τα logs για **≥14 συνεχόμενες ημέρες** (Serilog file sink: `logs/incis-*.log`).
3. Εκτελέστε το εργαλείο (αντίγραφο στο `tools/`):
```powershell
.\tools\analyze-phase0-logs.ps1 -LogPath <logs_dir> -SinceDays 14 -OutDir .\phase0-report
```
4. Επιστρέψτε τα: `phase0-report\phase0_summary.csv`, `phase0_by_endpoint.csv`, `phase0_report.md`.
   Το report περιλαμβάνει **data quality** (files, parsed, malformed, duplicates, out-of-window), **days covered / complete** και **limitations**.

## 3. Ασφάλεια/ιδιωτικότητα
- Το εργαλείο κάνει **πλήρη redaction ταυτοτήτων** (`user-<hash>`) εξ ορισμού — **δεν εξάγονται emails**. Το `-ShowMaskedEmails` χρησιμοποιείται μόνο με ρητή έγκριση.
- Token-like values μασκάρoνται· **δεν** εξάγονται tokens/passwords/σώματα.
- Περιορίστε την πρόσβαση σε logs/reports· μην αποστέλλετε raw logs εκτός εγκεκριμένου καναλιού.
- **Κενά:** αν υπάρχουν ημέρες χωρίς logs, δηλώστε τις· το report το εμφανίζει ως completeness=false.

## 4. Κριτήρια απόφασης (στο report)
- **Cleared:** 0 `PHASE-0 OBSERVATION` από ρόλους εκτός Admin/SuperUser για **όλο** το 14ήμερο **και** γραπτή επιβεβαίωση PO.
- **Συμβατότητα πολιτικής:** το κριτήριο ευθυγραμμίζεται με την πολιτική `WriteRoles=Admin,SuperUser`· οποιοσδήποτε άλλος ρόλος που εμφανίζεται σε OBSERVATION εξετάζεται πριν την ενεργοποίηση.
- **Not cleared:** οποιαδήποτε OBSERVATION ή ημιτελές/μη συνεχές 14ήμερο → παράταση.
- **Η απουσία κλήσεων δεν αποδεικνύει απουσία consumers.**

## 5. Αποδεικτικό που κλείνει το E2b
Τα 3 αρχεία + δήλωση κάλυψης (πρώτο/τελευταίο timestamp, ημέρες, κενά) + deployed commit/tag.
