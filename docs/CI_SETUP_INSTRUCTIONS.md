# CI Setup Instructions (για τον υπεύθυνο) — δεν εκτελέστηκε αυθαίρετα

> Ο χώρος εργασίας **δεν** είναι Git repository. ΔΕΝ έγινε `git init`/push χωρίς συνεννόηση.

## 1. Δημιουργία/σύνδεση repo
```bash
cd "<solution root>"
git init
git add .
git commit -m "INCIS SaaP — INCI unification + H-1/H-2 policy + CI fixes"
git branch -M main
git remote add origin <REPO_URL>
git push -u origin main
```

## 2. Απαιτούμενα secrets/variables (GitHub → Settings → Secrets and variables → Actions)
| Όνομα | Τύπος | Σκοπός |
|---|---|---|
| *(κανένα υποχρεωτικό για το τρέχον `ci.yml`)* | — | Το workflow χρησιμοποιεί SQL SA test password inline (test-only) και test JWT/AdminSeed |
| `SONAR_TOKEN` (προαιρετικό) | Secret | Αν ενεργοποιηθεί code scanning |

> Σημείωση: το `SA_PASSWORD` του CI είναι **test-only** (container παροδικό). Μην το χρησιμοποιείς σε παραγωγή.

## 3. Εκτέλεση
- **Trigger:** push σε `main`/`develop` ή PR προς `main` (βλ. `on:` στο `ci.yml`).
- **Jobs:** `build-and-test` (SQL 2022 + coverage gate ≥70%) → `security-scan` & `docker-build` (main) & `playwright-tests`.

## 4. Αναμενόμενα artifacts
- `coverage-report` (cobertura).
- `playwright-results` (TRX).
- Test results (TRX) στο job log/artifacts.

## 5. Αποδεικτικό επιστροφής
- URL του workflow run (πράσινο).
- Λήψη artifacts (coverage + trx).
- Οποιαδήποτε αποτυχία → log + ανάλυση (χωρίς bypass).
