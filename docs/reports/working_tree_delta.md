# Working-tree delta (μετά το v16) — για αξιολόγηση στο επόμενο release

> Το **v16 ZIP παραμένει αμετάβλητο** (`D517D7FEF5C2E51AD0F89B5D98493E8ED806F77E521742E14D194FF9310FDFA7`).
> Οι παρακάτω αλλαγές **δεν** ενσωματώθηκαν σιωπηρά σε αυτό· είναι delta του working tree.

## 1. PII sanitization — `INCIS_SaaP/Services/ImportService.cs`
Μόνο τα **sample rows του import template** (`GetSampleRow`, χρησιμοποιείται στη γραμμή 116 για παραγωγή template). **Δεν** επηρεάζεται η πραγματική εισαγωγή (διαβάζει το ανεβασμένο αρχείο).

Πριν → Μετά:
| Πεδίο | Πριν (demo) | Μετά (placeholder) |
|---|---|---|
| Client name | `ACME Cosmetics AE` | `Demo Cosmetics AE` |
| Address | `Λ. Κηφισίας 1, Αθήνα` | `Demo Address 1, Athens` |
| Email | `info@acme.gr` | `client@example.com` |
| Phone | `2101234567` | `+30 0000000000` |
| ΑΦΜ | `123456789` | `000000000` |
| ΔΟΥ | `ΦΑΕ Αθηνών` | `Demo Tax Office` |
| Contact | `Μαρία Παπαδοπούλου` | `Demo Contact` |
| Mobile | `6900000000` | `+30 0000000000` |
| Email | `maria@acme.gr` | `contact@example.com` |
| Supplier | `Ingredients Hellas ΕΠΕ` / `sales@ingredients.gr` / `2310000000` / `987654321` | `Demo Ingredients Ltd` / `supplier@example.com` / `+30 0000000000` / `000000000` |

**Tests:** `ImportServiceTests` **17/17 PASS**.
**Δήλωση:** τα placeholders είναι αποκλειστικά δοκιμαστικά· η κανονική εισαγωγή πραγματικών δεδομένων δεν αλλάζει.

## 2. Publish safety gate — `tools/verify-repo-publish.ps1` (νέο)
- Exit contract: `0=PASS`, `1=SECURITY_FINDINGS`, `2=NOT_AUTHORIZED`, `3=INVALID_INPUT` (τυπώνει `EXIT=<n> REASON=...`).
- Αποκλείει: `bin/obj/logs/coverage/TestResults`, `*.bak/*.mdf/*.ldf/*.zip/*.rar/*.trx/*.cobertura.xml`, `FILES/`, `.env*`, `appsettings.Development.json`, **`*.log`**, **`*.xls/*.xlsx`**, **`*.pdf/*.docx/*.doc`**.
- Ανιχνεύει: secrets/connection strings με credentials, JWT literals, **PII** (email εκτός allowlisted test domains, Greek phone, IBAN).
- **Authorization ΔΕΝ παρακάμπτει** τον έλεγχο (secret + `-OwnerAuthorized` → exit 1).
- Allowlist περιορίζεται σε `tools/tests/*` (synthetic fixtures) και τον scanner· **το `.github/workflows/ci.yml` ΔΕΝ είναι allowlisted**.
- Tracked-file check μέσω `git ls-files` όταν υπάρχει repo· αλλιώς full-tree scan (ρητά: skipped).

**Τελευταίο scan:** `CANDIDATES 484 / 12,9MB`, `EXCLUDED 3425`, **0 findings**, `EXIT=2 REASON=NOT_AUTHORIZED` (**όχι PASS**).

## 3. CI secrets — `.github/workflows/ci.yml`
Αφαιρέθηκαν τα hardcoded test credentials· χρησιμοποιούνται GitHub secrets:
- `CI_SQL_SA_PASSWORD` (SA password για SQL 2022 service + connection strings + health-cmd)
- `CI_JWT_SECRET`
- `CI_ADMIN_SEED_PASSWORD`
Απαιτείται ορισμός τους στο repo (Settings → Secrets). Χωρίς αυτά το workflow αποτυγχάνει (αναμενόμενο).

## 4. Docker/config hardening
- `INCIS_SaaP/docker-compose.yml`: `${MSSQL_SA_PASSWORD}`, `${JWT_SECRET}`, `${ADMIN_SEED_PASSWORD}` (κανένα literal)· **επαληθεύτηκε** `docker compose config` exit 0, services `incis-web/sqlserver/redis`. (Είχε αλλοιωθεί προσωρινά από regex και επαναφέρθηκε.)
- `INCIS_SaaP/.env.example`: placeholders.
- `INCIS_SaaP/appsettings.Development.json`: **git-ignored** (περιείχε dev credentials)· `appsettings.Development.json.example` με placeholders.
- `.gitignore`: προσθήκες `*.log`, `*.xls`, `*.xlsx`, `*.pdf`, `*.docx`, `*.doc`, `appsettings.Development.json`, `.env*`.

## 5. Tests/tools
- `tools/tests/test-tools.ps1`: επεκτάθηκε (exit codes, SHA, dedupe, malformed/duplicate/out-of-window, full redaction, PII, exclusions, manifest, ci.yml secret detection) → **failures=0**.
- `docs/reports/publish_candidates.csv`: manifest 484 candidates για review από Repo Owner.

## 5β. S-1 fix — browser user approval (2026-11-11)
| Αρχείο | Αλλαγή |
|---|---|
| `INCIS_SaaP/Views/Users/Index.cshtml` | `data-testid` (approve/toggle), ρητό `formaction`/`formmethod`, class `erp-action-text` |
| `INCIS_SaaP/wwwroot/css/odoo-erp.css` | `.erp-actions .btn.erp-action-text { width:auto; padding:0 .65rem; gap:.35rem; white-space:nowrap; }` |
| `INCIS_SaaP.PlaywrightTests/UsersApprovalTests.cs` | Real-click tests (approve/deactivate/pending/cross-tenant) |

**Root cause:** 32×32px buttons με μακρύ ελληνικό κείμενο → overflow/hit-test → το click στο «Έγκριση» χτυπούσε το «Απενεργοποίηση» (POST σε `/Users/ToggleActive`).
**Verification:** `UsersApprovalTests` **7/7** · full **xUnit 706/706** · full **Playwright 46/46**.

## 5γ. Commercial SaaS hardening (2026-11-11)
| Αρχείο | Αλλαγή |
|---|---|
| `INCIS_SaaP/Configuration/ApiWritePolicyOptions.cs` | `Roles` → `Admin,SuperUser,TenantAdmin` |
| `INCIS_SaaP/appsettings.json`, `docs/deployment/appsettings.Production.template.json` | ίδιο Roles |
| `INCIS_SaaP.Tests/Security/ApiWritePolicyTests.cs` | `TenantAdmin` allowed (Phase 1) |
| `INCIS_SaaP.PlaywrightTests/CommercialOperationsTests.cs` | **νέο**: MaxUsers limit, SetLimits, audit log (3/3) |
| `tools/load-test.ps1` | **νέο**: out-of-process load smoke |
| `docs/deployment/*` | deployment package + production template |
| `Database/Scripts/07_scheduled_backup.sql` | **νέο**: scheduled FULL+LOG backup |

**Επιβεβαίωση:** xUnit **707/707** · Playwright **49/49** · load 200/200 @ C=20 · `TenantAdmin` μπορεί API writes (Phase 1).

## 6. Δεδομένα (καμία αλλαγή)
- `INCIs`: **19.724 / 19.724** από `ALL_INCIS_NEW_FILE` (`DataSource=ALL_INCIS_NEW_FILE`).
- CosIng: 24.094 (CSV) + 2.353 (JSON) — διατηρούνται ως ξεχωριστό dataset.
- Καμία διαγραφή/αλλαγή πηγής/mapping.
