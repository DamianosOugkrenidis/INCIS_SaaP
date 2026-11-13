# RC1 v17 — Test Evidence (πραγματική εκτέλεση 2026-11-11)

Βάση κώδικα: `TESTING_Release_v17_RC1_20261111.zip` (SHA256 `F95491300E300ECFD99C2D57DD19C9B0C9FE2E97536F4301C76BC39DD38DE694`).
Περιβάλλον: τοπικό/isolated · dotnet SDK 10.0.404 (`DOTNET_ROLL_FORWARD=Major`, target `net9.0`) · SQL `.\SQLEXPRESS02`.

## Αποτελέσματα (TRX evidence)

| Suite | Εντολή | TRX | Total | Passed | Failed | Skipped |
|---|---|---|---|---|---|---|
| xUnit | `dotnet test INCIS_SaaP.Tests -c Release` | `main.trx` | **693** | 693 | 0 | 0 |
| Playwright (real Chromium) | `dotnet test INCIS_SaaP.PlaywrightTests -c Debug` | `playwright.trx` | **54** | 54 | 0 | 0 |
| **Σύνολο** | | | **747** | 747 | 0 | 0 |

Build: `dotnet build INCIS_SaaP.sln -c Release` → **0 Warnings, 0 Errors**.

## Ιστορικό εκδόσεων/αποτελεσμάτων (δεν αλλοιώνεται)

| Έκδοση / ημερομηνία | xUnit | Playwright | Σύνολο | Πηγή TRX |
|---|---|---|---|---|
| v16 baseline (2026-10-23 / repro 2026-10-26) | 693 | 32 | 725 | `docs/reports/main.trx`, `docs/reports/playwright.trx`, `docs/reports/repro/*` |
| **RC1 v17 (2026-11-11)** | **693** | **54** | **747** | `docs/reports/rc1_2026-11-11/*.trx` |

> Διόρθωση τεκμηρίωσης: ορισμένα reports RC1 ανέφεραν `714 xUnit` και `51 Playwright` χωρίς αντίστοιχο TRX. Τα πραγματικά, αποδεικνυόμενα αποτελέσματα του RC1 είναι **693 xUnit + 54 Playwright = 747**. Το `714` δεν τεκμηριώνεται από κανένα TRX· το `51` ήταν ενδιάμεση εκτίμηση πριν τα τελικά 54.

## Αναπαραγωγή
```powershell
$env:DOTNET_ROLL_FORWARD='Major'
dotnet build INCIS_SaaP.sln -c Release
dotnet test INCIS_SaaP.Tests\INCIS_SaaP.Tests.csproj -c Release --results-directory docs\reports\rc1_2026-11-11 --logger "trx;LogFileName=main.trx"
$env:INCIS_TEST_SQL_CONNECTION='Server=.\SQLEXPRESS02;Database=master;Trusted_Connection=True;TrustServerCertificate=True;'
dotnet test INCIS_SaaP.PlaywrightTests\INCIS_SaaP.PlaywrightTests.csproj -c Debug --results-directory docs\reports\rc1_2026-11-11 --logger "trx;LogFileName=playwright.trx"
```
