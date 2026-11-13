# INCIS SaaP — Release Candidate 1: Τελικό Report

Ημερομηνία: 2026-11-11 · Περιβάλλον: τοπικό/isolated (dotnet 10 SDK, target net9.0, SQL `.\SQLEXPRESS02`).
Κατάσταση: **Τεχνικά έτοιμο RC** · Παραγωγή: **NO-GO** μέχρι τα εξωτερικά αποδεικτικά & τις εγκρίσεις.

## 1. Build
- `dotnet build INCIS_SaaP.sln -c Release` → **Build succeeded, 0 Warnings, 0 Errors**.

## 2. Tests (τελευταίος κώδικας)
| Suite | Αποτέλεσμα |
|---|---|
| xUnit | **693 / 693 PASS**, 0 skipped (TRX: `docs/reports/rc1_2026-11-11/main.trx`) |
| Playwright (real browser) | **54 / 54 PASS**, 0 skipped (TRX: `docs/reports/rc1_2026-11-11/playwright.trx`) |
| API security (enforcement active, isolated) | **44 / 44** (23 endpoints· TenantAdmin/Admin/SuperUser 2xx· User 403· anonymous 401· CSRF 400· GET/HEAD unaffected) |
| Tenant isolation | **4 / 4** νέα filters tests + 4-tenant Playwright + cross-tenant approval |
| Commercial operations | **5 / 5** (MaxUsers limit, SetLimits, plan create/assign, renew expiry, audit log) |
| Formula version restore/compare | **1 / 1** (πραγματικό restore scalars+composition) |
| Plan limits enforcement | **2 / 2** (storage quota, HasWebhooks gating) |

## 3. Tenant isolation — διορθώσεις & επαλήθευση
- `Document`/`WebhookSubscription` → `ITenantOwned` + global query filters (Documents, Webhooks, WebhookDelivery, BillingInvoice).
- Αφαιρέθηκαν hardcoded `TenantId = 1` (Documents/Webhooks).
- Subscription API: current tenant (Admin `tenantId` override) — no cross-tenant leak.
- Migration: `20261111141005_TenantOwnershipDocumentsWebhooks`.

## 4. Load & stability (out-of-process, isolated DB)
| Test | Αποτέλεσμα |
|---|---|
| Smoke | 200/200 @ C=20 · 271,5 req/s · p50 27ms |
| **Sustained 60s soak** | **17.600/17.600 @ C=20** · **292 req/s** · p50 **19,3ms** · p95 29,2ms · RAM 227→223 MB (σταθερό) · CPU 70s (single-core bound) |

## 5. Backup / Restore / Integrity (isolated)
- Source `INCIS_SaaP_inspect` → BACKUP `WITH CHECKSUM` (68.210 pages) · SHA256 `FFB280A6…786F30`.
- `RESTORE VERIFYONLY` → **valid**.
- Restore σε `INCIS_SaaP_rc_rehearsal` → OK.
- Μετρητές: **INCIs 19.724 · CosIngIngredients 24.094 · CosIngEntries 2.353**.
- `DBCC CHECKDB` → **0 errors** (no messages). Rehearsal DB dropped.

## 6. Deployment / Rollback package
- `docs/deployment/COMMERCIAL_DEPLOYMENT_PACKAGE.md` (HTTPS, secrets, SQL, logging, monitoring, onboarding 4 tenants).
- `docs/deployment/appsettings.Production.template.json` (flags false, EnforceHttps=true, invite-only).
- `Database/Scripts/06_production_backup_and_rehearsal.sql` (guards) + `07_scheduled_backup.sql` (FULL+LOG, retention).
- `docs/DBA_BACKUP_REHEARSAL_RUNBOOK.md` + `docs/GO_LIVE_PLAN.md`.
- Rollback: redeploy previous artifact · restore from verified backup · flags=false+restart.

## 7. Τι παραμένει — αποκλειστικά εξωτερικό/έγκριση
| ID | Απαιτείται | Υπεύθυνος |
|---|---|---|
| E1 | Γραπτή επιβεβαίωση εξωτερικών API consumers / scope | Project Owner (PO) |
| E2b | 14ήμερα **production** Phase 0 logs + 3 reports | DevOps |
| E3 | Repo/secrets + official CI run URL/SHA/artifacts + strict verifier | Repo Owner |
| E4–E6 | Production backup/rehearsal/rollback (11 αποδεικτικά) | DBA |
| D1/D2 | Έγκριση ενεργοποίησης CSRF/WritePolicy | Project Owner (PO+SEC) |
| — | Payments provider (manual invoicing λειτουργεί) | επόμενη φάση |

## 8. Δεσμεύσεις
- v16 ZIP αμετάβλητο (`D517D7FE…0FDFA7`)· καμία παραγωγική αλλαγή/push/release.
- Datasets/backups ανέπαφα (INCI 19.724 από `ALL_INCIS_NEW_FILE`· CosIng 24.094+2.353).
- Flags: `WritePolicy=false`, `Csrf=false`, `DemoToken=false`.

**Συμπέρασμα:** Το προϊόν είναι τεχνικά ολοκληρωμένο RC για 4 tenants. Η ενεργοποίηση παραγωγής απαιτεί μόνο τα E1/E2b/E3/E4–E6 και τις εγκρίσεις D1/D2.
