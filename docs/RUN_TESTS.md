# Οδηγίες Εκτέλεσης Δοκιμών — INCIS SaaP

## Προαπαιτούμενα
- .NET SDK 9 (ή νεότερο με `DOTNET_ROLL_FORWARD=Major`)
- SQL Server για τα integration tests (τοπικά `.\SQLEXPRESS02` ή οποιοδήποτε instance)

## 1. Όλα τα tests (unit + integration + functional + regression + security)

PowerShell:
```powershell
$env:DOTNET_ROLL_FORWARD = 'Major'
$env:INCIS_TEST_SQL_CONNECTION = 'Server=.\SQLEXPRESS02;Database=master;Trusted_Connection=True;TrustServerCertificate=True;'
dotnet test INCIS_SaaP.Tests/INCIS_SaaP.Tests.csproj
```

## 2. Ανά κατηγορία (traits)

```powershell
# Μόνο unit + functional (χωρίς SQL)
dotnet test INCIS_SaaP.Tests/INCIS_SaaP.Tests.csproj --filter "Category!=SqlIntegration"

# Μόνο SQL integration (απαιτεί SQL Server)
dotnet test INCIS_SaaP.Tests/INCIS_SaaP.Tests.csproj --filter "Category=SqlIntegration"

# Security
dotnet test INCIS_SaaP.Tests/INCIS_SaaP.Tests.csproj --filter "Category=Security"

# Regression
dotnet test INCIS_SaaP.Tests/INCIS_SaaP.Tests.csproj --filter "Category=Regression"
```

## 3. Με κάλυψη κώδικα

```powershell
dotnet test INCIS_SaaP.Tests/INCIS_SaaP.Tests.csproj `
  --collect:"XPlat Code Coverage" --results-directory ./coverage
# το report: ./coverage/<guid>/coverage.cobertura.xml
```

## 4. Σύντομο script
```powershell
./run-tests.ps1            # όλα τα tests
./run-tests.ps1 -Coverage  # με κάλυψη
./run-tests.ps1 -Category SqlIntegration
```

## 5. Playwright (browser/UI tests)
Πραγματικό Kestrel σε τυχαία θύρα + **απομονωμένη** βάση (`INCIS_SaaP_PW_<guid>`), seeded admin.

```powershell
# Πρώτη φορά: εγκατάσταση Chromium (μία φορά)
./run-playwright-tests.ps1 -Install
# Επόμενες φορές
./run-playwright-tests.ps1
```
Ή χειροκίνητα:
```powershell
dotnet build INCIS_SaaP.PlaywrightTests/INCIS_SaaP.PlaywrightTests.csproj
powershell -File INCIS_SaaP.PlaywrightTests/bin/Debug/net9.0/playwright.ps1 install chromium
dotnet test INCIS_SaaP.PlaywrightTests/INCIS_SaaP.PlaywrightTests.csproj
```

## Σημειώσεις
- Τα SQL integration tests δημιουργούν/διαγράφουν **δική τους** βάση (`INCIS_SaaP_Tests_<guid>`). Δεν αγγίζουν παραγωγή.
- Αν δεν υπάρχει SQL Server, τα SQL tests κάνουν soft-skip (δεν αποτυγχάνουν).
- Σε άλλο instance: όρισε `INCIS_TEST_SQL_CONNECTION`.
