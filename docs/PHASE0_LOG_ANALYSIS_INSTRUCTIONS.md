# Οδηγίες Συλλογής & Ανάλυσης Phase 0 Logs (H-2)

Σκοπός: καταγραφή κλήσεων προς τα API write endpoints **χωρίς αποκλεισμό**, ώστε να αποφασιστεί με στοιχεία η ενεργοποίηση του enforce.
**Δεν** αποτελεί προστασία πρόσβασης (Phase 0 = log-only).

## 1. Τι καταγράφεται (ασφαλές)
Το `ApiWritePolicyFilter` γράφει (Serilog):
- `API-write PHASE-0 OBSERVATION (no enforcement) {Method} {Path} user={User} id={UserId} requiredRoles={Roles}` — για κλήσεις που **θα αποκλείονταν**.
- `API-write ALLOW ... matchedRole=…` — για εξουσιοδοτημένες.
- **Ποτέ** passwords/tokens/headers/σώματα αιτημάτων. Μόνο identity.Name, NameIdentifier, path, method, ρόλος.

## 2. Πού βρίσκονται
- Serilog File sink: `logs/incis-*.log` (rolling ημερήσιο· `retainedFileCountLimit: 30`).
- Προαιρετικά σε κεντρικό sink αν ρυθμιστεί.

## 3. Συλλογή (14 ημέρες)
```powershell
$since = (Get-Date).AddDays(-14)
$files = Get-ChildItem 'logs\incis-*.log' | Where-Object { $_.LastWriteTime -ge $since }
$phase0 = $files | Select-String -Pattern 'API-write PHASE-0 OBSERVATION'
# Ομαδοποίηση ανά χρήστη/ρόλο/endpoint
$phase0 | ForEach-Object { $_.Line } |
  Group-Object { ($_ -split 'user=')[1] } | Sort-Object Count -Descending
```
**Έξοδος προς παράδοση:** πίνακας (ημερομηνία, endpoint, method, user, id) — χωρίς άλλα δεδομένα.

## 4. Κριτήρια απόφασης
- **Επιτρεπτικό για enforce:** μηδενικές (ή τεκμηριωμένα εγκεκριμένες) Phase 0 κλήσεις από ρόλους εκτός `Admin/SuperUser` για ≥**14 ημέρες** με **επαρκή καταγραφή** για όλο το διάστημα.
- Αν η καταγραφή **δεν καλύπτει** όλο το 14ήμερο → **παράταση** παρατήρησης (δεν συμπεραίνεται έγκριση).
- Αν υπάρχουν κλήσεις → ταυτοποίηση consumer + απόφαση (ενημέρωση/εξαίρεση με χρονικό όριο ή προσαρμογή).

## 5. Προστασία δεδομένων
- Χωρίς PII πέραν του identity (ήδη ελάχιστο). Χωρίς secrets.
- Περιορισμός πρόσβασης στα logs σε εξουσιοδοτημένους.
- Τα αποδεικτικά κοινοποιούνται μόνο σε εγκεκριμένους αποδέκτες.

> **Σημαντικό:** Απουσία Phase 0 κλήσεων στα logs **δεν** αποδεικνύει απουσία εξωτερικών consumers — απαιτείται και το ερωτηματολόγιο (`EXTERNAL_API_CONSUMERS_QUESTIONNAIRE.md`).
