# PHASE 0 — Deployment & Rollback Package (log-only, enforcement OFF)

Κατάσταση: **ΠΡΟΣ ΕΛΕΓΧΟ — ΔΕΝ έχει εγκριθεί ούτε εκτελεστεί deployment.**

## 0. Topology — τοπικός υπολογιστής (PC) · 4 tenants
Αρχική παραγωγική φάση σε **έναν τοπικό PC** (ο «production node» = ο συγκεκριμένος υπολογιστής):
- 1 instance εφαρμογής (Kestrel ή IIS + ASP.NET Core Hosting Bundle)· **SQL Server Express** τοπικά (χωρίς COMPRESSION — Msg 1844).
- Ένα DB `INCIS_SaaP` · 4 tenants · διαχείριση tenants/providers τοπικά.
- Δίκτυο: τοπικό (LAN/localhost)· TLS προαιρετικό για την αρχική τοπική λειτουργία.
- Logs: τοπικά Serilog files (`logs/incis-*.log`) — **αυτά** συλλέγονται για το Phase 0.
- Οι ρόλοι DevOps/DBA ενδέχεται να εκτελούνται από τον **ίδιο τοπικό operator**.

Σκοπός: 14ήμερο παρακολούθηση των δύο security filters σε **log-only** λειτουργία, ώστε να τροφοδοτηθεί το E2b, **χωρίς** ενεργοποίηση enforcement.

> **Απαγορεύεται** η ενεργοποίηση `Api__Csrf__Enforce` / `Api__WritePolicy__Enforce` σε αυτή τη φάση. Απαιτείται ξεχωριστή έγκριση D1/D2.

---

## 1. Artifact identity (commit / hash)

| Στοιχείο | Τιμή |
|---|---|
| Πακέτο | `dist/v18_RC2_20261111.zip` (καθαρό, deterministic, buildable) |
| SHA256 (content identity) | **βλ. `dist/v18_RC2_20261111.zip.sha256`** (εξωτερικό αρχείο — δεν ενσωματώνεται στο ίδιο το πακέτο, ώστε να μην υπάρχει self-reference) |
| Manifest | `dist/v18_RC2_20261111.MANIFEST.csv` (path+size+SHA256 ανά αρχείο) |
| Git commit | **Δεν υπάρχει ακόμη** (repo μη αρχικοποιημένο). Μέχρι το E3, ως ταυτότητα χρησιμοποιείται το ZIP SHA256 + το MANIFEST. Μετά το E3, εγγράφεται εδώ το commit SHA. |
| SDK/runtime | dotnet SDK 10.0.404 · target `net9.0` (`DOTNET_ROLL_FORWARD=Major`) |

Αναπαραγωγή πακέτου: `.\tools\build-release.ps1 -Version v18_RC2_20261111 -OutputDir .\dist -Scanner`
Ανεξάρτητος έλεγχος: `.\tools\scan-release-artifacts.ps1 -Path .\dist\v18_RC2_20261111.zip -FailOnFindings` → **0 FAIL**.

## 2. Enforcement flags — υποχρεωτικά `false`

| Ρύθμιση | Τιμή | Σκοπός |
|---|---|---|
| `Api__Csrf__Enforce` | **false** | CSRF filter σε log-only (OBSERVATION) |
| `Api__WritePolicy__Enforce` | **false** | WritePolicy filter σε log-only (OBSERVATION) |
| `Api__DemoToken__Enabled` | **false** | demo token ανενεργό |
| `Security__EnforceHttps` | true | HTTPS/HSTS/secure cookies |

Οι τιμές τίθενται ως environment variables (ΠΟΤΕ σε appsettings committed).

## 3. Deploy steps (εκτέλεση ΜΟΝΟ μετά την έγκρισή σου)

1. Provision/επιβεβαίωση staging→production target + TLS + secrets (βλ. `docs/deployment/COMMERCIAL_DEPLOYMENT_PACKAGE.md` §2).
2. Ανάπτυξη artifact (RC2)· βεβαιώσου ότι τα env vars του §2 είναι **ακριβώς** αυτά.
3. EF migrations αυτόματα στο startup· `/health` = 200.
4. **Μην** αλλάξεις flags σε `true` σε κανένα σημείο.

## 4. Post-deployment verification (αποδεικνύει enforcement OFF + filters present)

| # | Έλεγχος | Αναμενόμενο |
|---|---|---|
| V1 | `GET /health` | **200** |
| V2 | Login ως Admin | επιτυχές |
| V3 | Επιβεβαίωση ρυθμίσεων στο runtime (env/`appsettings`) για τα 3 flags | **false/false/false** |
| V4 | Cookie-auth **POST** write endpoint **χωρίς** `X-CSRF-TOKEN` | **δεν** επιστρέφει 400 (CSRF log-only) + γραμμή log `API-CSRF … OBSERVATION` |
| V5 | Write endpoint ως ρόλος **User** | **δεν** επιστρέφει 403 (WritePolicy log-only) + γραμμή log `API-write … OBSERVATION` |
| V6 | `GET`/`HEAD` endpoints | ανεπηρέαστα (2xx/3xx ως συνήθως) |
| V7 | Παρουσία markers στους production logs | `ApiCsrfFilter`/`ApiWritePolicyFilter` σε Phase 0 emit |
| V8 | 5xx rate μετά το deploy | **0 αύξηση** έναντι baseline |

> Αν V4/V5 επιστρέψουν **400/403** → το enforcement είναι **ΕΝΕΡΓΟ** κατά λάθος → **άμεσο rollback (§5)** και ειδοποίηση SEC. Αν V7 αποτύχει (λείπουν markers) → το 14ήμερο **δεν ξεκινά**.

## 5. Rollback (άμεσο, μη καταστροφικό)

1. Θέσε `Api__Csrf__Enforce=false` και `Api__WritePolicy__Enforce=false` (επαναφορά) → restart.
2. Αν χρειαστεί πλήρης επιστροφή: redeploy προηγούμενου artifact (RC1/v16) — καμία αλλαγή δεδομένων.
3. Επιβεβαίωση: V1–V3 ξανά.
Χρόνος rollback: μόνο restart (~<2′). Δεν απαιτείται restore DB για αυτή τη φάση (μόνο log-only αλλαγή).

## 6. Οδηγίες προς DevOps — συλλογή 14ημέρου (E2b)

Προϋπόθεση έναρξης: επιβεβαίωση **commit/tag/hash** του deployed build + **δείγμα log line** που αποδεικνύει presence των δύο filters.
1. Συλλογή production logs **≥14 συνεχόμενες ημέρες** (χωρίς κενά).
2. Ανάλυση: `.\tools\analyze-phase0-logs.ps1 -LogPath <logs_dir> -SinceDays 14 -OutDir .\phase0-report`
3. Παράδοση: `phase0_summary.csv`, `phase0_by_endpoint.csv`, `phase0_report.md` + **δήλωση πληρότητας** (πρώτο/τελευταίο timestamp, ημέρες, κενά).
4. Ιδιωτικότητα: πλήρες redaction ταυτοτήτων (`user-<hash>`)· ποτέ emails/tokens.
5. Αποδοχή E2b: **0 OBSERVATION** από ρόλους εκτός **Admin/SuperUser/TenantAdmin** · πλήρες 14ήμερο.

## 7. Απόδειξη ότι δεν ενεργοποιείται enforcement (checklist SEC)

- [ ] §2 flags = false/false/false (runtime).
- [ ] V4 & V5: κανένα 400/403 από τα filters (log-only).
- [ ] V7: markers παρόντες (filters φορτωμένα).
- [ ] Καμία εγγραφή σε config/flags που να θέτει `Enforce=true`.
- [ ] Dashboard/alerts παρακολουθούν 400/403/5xx (baseline).

## 8. Έγκριση (Ιδιοκτήτης PO+SEC)

☐ Εγκρίνω το Phase 0 deploy **ως έχει** (log-only, flags false) · ☐ Ζητώ αλλαγές
Ονοματεπώνυμο/ρόλος: ____________________ · Ημερομηνία: __________ · Παράθυρο: __________

> Χωρίς την παραπάνω υπογραφή **δεν** εκτελείται deploy. Η έγκριση αφορά **μόνο** Phase 0 (log-only) — **όχι** enforcement.

## Παραπομπές
`docs/security/ENFORCEMENT_ACTIVATION_RUNBOOK.md` · `docs/deployment/COMMERCIAL_DEPLOYMENT_PACKAGE.md` · `docs/requests/DEVOPS_REQUEST.md` · `tools/analyze-phase0-logs.ps1` · `docs/reports/release_hygiene/STRAY_LOGS_INVENTORY.md`.
