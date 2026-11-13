# H-2 — Αξιολόγηση Πολιτικής Πρόσβασης στα API Write Endpoints

Ημερομηνία: 2026-10-20 · Κατάσταση: **PHASE 0 ΥΛΟΠΟΙΗΘΗΚΕ (log-only, χωρίς επιβολή)** — **Phase 1/2 ΕΚΚΡΕΜΟΥΝ** (δεν ενεργοποιήθηκε στην παραγωγή).

> **ΣΗΜΑΝΤΙΚΟ:** Η λειτουργία Phase 0 (log-only) **ΔΕΝ είναι προστασία πρόσβασης** — δεν αποκλείει μη εξουσιοδοτημένες εγγραφές. Είναι μηχανισμός παρατήρησης για τη λήψη απόφασης.

## 1. Πλαίσιο αυθεντικοποίησης (αποδεικτικά)
- Η εφαρμογή ρυθμίζει **ASP.NET Identity (cookie)**· `builder.Services.AddIdentity<ApplicationUser, IdentityRole>(...)` (`Program.cs:421`). **Δεν** υπάρχει ενεργό `AddJwtBearer` στο pipeline.
- Το `JwtService.GenerateToken(...)` παράγει tokens **με role claims**, αλλά **δεν** είναι συνδεδεμένο ως scheme αυθεντικοποίησης. Το `POST /api/v1/Api/token` (`ApiController.cs:262-285`) επιστρέφει **demo token** (δεν επικυρώνεται) — δηλ. στην πράξη τα API αυθεντικοποιούνται με **cookie συνεδρίας**.
- Συνέπεια: **οποιοσδήποτε συνδεδεμένος χρήστης, ανεξαρτήτως ρόλου**, μπορεί σήμερα να καλέσει τα API writes (μόνο `[Authorize]` = authenticated).

## 2. Απογραφή write endpoints (`ApiExtendedController`, `[Route("api/v1")]`, class `[Authorize]`)

| # | Μέθοδος & Route | Λειτουργία | Σημερινή εξουσιοδότηση | Ποιοι μπορούν σήμερα | Προτεινόμενη πολιτική |
|---|---|---|---|---|---|
| 1 | POST `/api/v1/clients` | Δημιουργία πελάτη | authenticated | όλοι οι ρόλοι | Admin,SuperUser |
| 2 | PUT `/api/v1/clients/{id}` | Επεξεργασία πελάτη | authenticated | όλοι οι ρόλοι | Admin,SuperUser |
| 3 | DELETE `/api/v1/clients/{id}` | Διαγραφή πελάτη | authenticated | όλοι οι ρόλοι | Admin,SuperUser |
| 4 | POST `/api/v1/suppliers` | Δημιουργία προμηθευτή | authenticated | όλοι | Admin,SuperUser |
| 5 | PUT `/api/v1/suppliers/{id}` | Επεξεργασία προμηθευτή | authenticated | όλοι | Admin,SuperUser |
| 6 | DELETE `/api/v1/suppliers/{id}` | Διαγραφή προμηθευτή | authenticated | όλοι | Admin,SuperUser |
| 7 | POST `/api/v1/incis` | Δημιουργία INCI | authenticated | όλοι | Admin,SuperUser |
| 8 | PUT `/api/v1/incis/{id}` | Επεξεργασία INCI | authenticated | όλοι | Admin,SuperUser |
| 9 | DELETE `/api/v1/incis/{id}` | Διαγραφή INCI | authenticated | όλοι | Admin,SuperUser |
| 10 | POST `/api/v1/rawmaterials` | Δημιουργία Α΄ ύλης | authenticated | όλοι | Admin,SuperUser |
| 11 | PUT `/api/v1/rawmaterials/{id}` | Επεξεργασία Α΄ ύλης | authenticated | όλοι | Admin,SuperUser |
| 12 | DELETE `/api/v1/rawmaterials/{id}` | Διαγραφή Α΄ ύλης | authenticated | όλοι | Admin,SuperUser |
| 13 | POST `/api/v1/rawmaterials/copy` | Αντιγραφή Α΄ ύλης | authenticated | όλοι | Admin,SuperUser |
| 14 | POST `/api/v1/rawmaterials/{id}/samples` | Προσθήκη δείγματος | authenticated | όλοι | Admin,SuperUser |
| 15 | PUT `/api/v1/rawmaterials/{id}/samples/{sampleId}` | Επεξεργασία δείγματος | authenticated | όλοι | Admin,SuperUser |
| 16 | DELETE `/api/v1/rawmaterials/{id}/samples/{sampleId}` | Διαγραφή δείγματος | authenticated | όλοι | Admin,SuperUser |
| 17 | POST `/api/v1/rawmaterials/{id}/change-supplier` | Αλλαγή προμηθευτή | authenticated | όλοι | Admin,SuperUser |
| 18 | POST `/api/v1/restrictions` | Δημιουργία περιορισμού | authenticated | όλοι | Admin,SuperUser |
| 19 | PUT `/api/v1/restrictions/{id}` | Επεξεργασία περιορισμού | authenticated | όλοι | Admin,SuperUser |
| 20 | DELETE `/api/v1/restrictions/{id}` | Διαγραφή περιορισμού | authenticated | όλοι | Admin,SuperUser |
| 21 | POST `/api/v1/formulas/{id}/validate` | Έγκριση φόρμουλας | authenticated | όλοι | Admin,SuperUser |
| 22 | POST `/api/v1/formulas/{id}/invalidate` | Απόσυρση έγκρισης | authenticated | όλοι | Admin,SuperUser |
| 23 | POST `/api/v1/formulas/{id}/wip` | Toggle WIP | authenticated | όλοι | Admin,SuperUser |

> Σύγκριση: το `ApiProductManagementController` χρησιμοποιεί ήδη `[Authorize(Roles = WriteRoles)]` (WriteRoles = Admin,SuperUser) σε όλα τα writes → **η προβλεπόμενη πολιτική είναι σαφής**.

## 3. Υφιστάμενες διασυνδέσεις / clients
- **Εντός repo:** **καμία** χρήση των API writes (αναζήτηση σε `.cs/.cshtml/.md/.ps1/.json/.yml` → μόνο τα δικά μας docs).
- **Εκτός repo:** **μη επαληθεύσιμο** από εδώ. Δεν υπάρχει τεκμηριωμένος consumer. Το Swagger τεκμηριώνει Bearer, αλλά δεν λειτουργεί bearer auth (cookie μόνο) → δεν υπάρχει πραγματικό μη-cookie integration.
- **Συμπέρασμα κινδύνου:** η σημερινή επιφάνεια είναι προσβάσιμη από **cookie-authenticated χρήστες** (άνθρωποι/scripts μέσω browser session), όχι από machine tokens.

## 4. Επιχειρησιακό αποτέλεσμα κάθε επιλογής
| Επιλογή | Αποτέλεσμα |
|---|---|
| **A. Εφαρμογή `Admin,SuperUser` σε όλα τα 23** | Κλείνει το κενό· `User`/`TenantAdmin` χάνουν API writes. Κίνδυνος: τυχόν μη-admin API consumer. Καμία εσωτερική ροή δεν βασίζεται σε αυτά. |
| **B. Διαφοροποιημένη πολιτική** (π.χ. `TenantAdmin` σε restrictions; anonymous read-only) | Δεν προκύπτει ανάγκη από τον κώδικα· όλα είναι master-data writes → ίδια πολιτική. |
| **C. Διατήρηση ως έχει (μόνο authenticated)** | **Απορρίπτεται** — ευάλωτη πρόσβαση για λόγους συμβατότητας (ρητά μη αποδεκτό). |

**Σύσταση:** Επιλογή **A**, με **ελεγχόμενη μετάβαση**.

## 5. Σχέδιο ελεγχόμενης μετάβασης (προς έγκριση)
1. **Phase 0 (παρατηρητικότητα):** προσθήκη `[Authorize(Roles = WriteRoles)]` **πίσω από config flag** `Api:EnforceWriteRoles` (default `false`). Με `false`: καταγράφεται warning (log) σε κάθε μη-admin write, χωρίς block — συλλογή απόδειξης ύπαρξης/απουσίας νόμιμων μη-admin calls για 1–2 εβδομάδες.
2. **Phase 1 (εφαρμογή):** μετά την παρατήρηση, `Api:EnforceWriteRoles=true` → ενεργός server-side περιορισμός.
3. **Phase 2 (καθαρισμός):** αφαίρεση του flag, σταθερό `WriteRoles` (συνεπές με `ApiProductManagementController`).
- **Rollback:** `Api:EnforceWriteRoles=false` (άμεσο, χωρίς redeploy schema).
- **Εναλλακτικό (άμεσο):** εφαρμογή σταθερού `[Authorize(Roles=WriteRoles)]` — αποδεκτό εφόσον επιβεβαιωθεί ότι δεν υπάρχει μη-admin consumer.

## 6. Tests (αποδεικτικά)
`Security/ApiWriteAccessTests.cs`:
- **Anonymous denied** (POST/PUT/DELETE, αντιπροσωπευτικό δείγμα): 401/302 → **αποδεικνύει** ότι μη-συνδεδεμένοι δεν μπορούν να γράψουν.
- **Inventory lock (reflection):** καταγράφει τα 23 write actions και ότι **δεν** έχουν role restriction σήμερα (θα ενημερωθεί μετά το patch).
- **Characterization (H-2 gap):** `User` μπορεί σήμερα να POST `/api/v1/incis` (καταγράφεται· **θα αντιστραφεί** μετά το patch).
- **Target-policy (skipped):** `User` denied μετά το patch — ρητά καταγεγραμμένο ως skipped μέχρι την απόφαση.

## 7. Απόφαση που ζητείται
- (α) Εφαρμογή **A** άμεσα, ή (β) **ελεγχόμενη μετάβαση** (§5), ή (γ) διαφοροποιημένη πολιτική με τεκμηρίωση.
- Επιβεβαίωση ότι **δεν** υπάρχει μη-admin API consumer (ο υπεύθυνος συστημάτων).

---

## 8. Phase 0 — ΥΛΟΠΟΙΗΘΗΚΕ (προετοιμασία, χωρίς ενεργοποίηση)

| Στοιχείο | Υλοποίηση |
|---|---|
| Ρυθμίσεις | `Configuration/ApiWritePolicyOptions.cs` — `Api:WritePolicy:Enforce` (default **false**), `Api:WritePolicy:Roles` (default `Admin,SuperUser`) |
| Filter | `Filters/ApiWritePolicyFilter.cs` (global) — ενεργεί **μόνο** σε `ApiExtendedController` POST/PUT/PATCH/DELETE |
| Phase 0 (`Enforce=false`) | **Επιτρέπει** αλλά καταγράφει warning «PHASE-0 OBSERVATION (no enforcement) … θα αποκλειόταν μετά το enforce». Ρητά **όχι** προστασία. |
| Phase 1 (`Enforce=true`) | 403 με μήνυμα· καταγράφει DENY. |
| Ασφαλές logging | endpoint, method, `user` (identity.Name), `id` (NameIdentifier), matched role, decision. **Χωρίς** passwords/tokens/ευαίσθητα. |
| appsettings | `Api:WritePolicy:{Enforce:false, Roles:Admin,SuperUser}`, `Api:DemoToken:{Enabled:false}` |
| Demo token | `POST /api/v1/Api/token` → default **501 Not Implemented** με σαφές μήνυμα ότι **δεν** είναι πραγματικός μηχανισμός· μόνο με `Api:DemoToken:Enabled=true` εκτελεί (με warning). |
| Tests | `ApiWritePolicyTests` (8): Phase 0 allow+log, Phase 1 deny User, allow Admin/SuperUser, anonymous deny, GET unaffected, demo 501 default / enabled. |

**Ενεργοποίηση (Phase 1)** = μόνο με `Api:WritePolicy:Enforce=true` **μετά** έγκριση §7 + έλεγχο εξωτερικών διασυνδέσεων. **Δεν** έχει ενεργοποιηθεί.

## 9. Έλεγχος εξωτερικών consumers / integrations

| Έλεγχος | Μέθοδος | Αποτέλεσμα |
|---|---|---|
| Χρήση API writes εντός repo | grep σε `.cs/.cshtml/.md/.ps1/.json/.yml` | **Καμία** (μόνο δικά μας docs) |
| Swagger/OpenAPI consumers | έλεγχος ορισμών ασφαλείας | Αναφέρεται `Bearer`, αλλά **δεν** υπάρχει bearer validation → δεν υπάρχει πραγματικό machine-token integration |
| JWT/machine clients | έλεγχος pipeline (`AddJwtBearer` απών) | Δεν εκδίδονται/επικυρώνονται JWT → **μόνο cookie** sessions |
| Webhooks (outbound) | `ApiWebhooksController`, `Webhook` model | Outbound μηχανισμός· **δεν** καταναλώνει τα write endpoints |
| Εξωτερικά integrations (εκτός repo) | **ΔΕΝ ΕΙΝΑΙ ΕΠΑΛΗΘΕΥΣΙΜΟ από εδώ** | **ΑΓΝΩΣΤΟ — απαιτεί επιβεβαίωση υπευθύνου συστημάτων** |

**Καταγεγραμμένη αβεβαιότητα:** η απουσία in-repo χρήσης **δεν αποδεικνύει** απουσία εξωτερικών consumers. Πριν το production enforce απαιτείται: (α) επιβεβαίωση από τον υπεύθυνο, (β) ανάλυση των Phase 0 logs (ποιοι ρόλοι/ταυτότητες καλούν τα writes) για συγκεκριμένο διάστημα.

## 10. Phase 1 — Επαλήθευση σε ΑΠΟΜΟΝΩΜΕΝΟ περιβάλλον δοκιμών ✅

Η επιβολή ενεργοποιήθηκε **μόνο στα tests** (`Api:WritePolicy:Enforce=true` μέσω test settings· το `appsettings.json` παραμένει `false` = Phase 0). Αποτελέσματα:

| Έλεγχος | Test | Αποτέλεσμα |
|---|---|---|
| Μη εξουσιοδοτημένος (`User`) → **403** και μη-αποθήκευση | `H2_TARGET_UserRole_Denied_OnApiWrite` (**ενεργό**, χωρίς Skip) | ✅ |
| `Admin` διατηρεί λειτουργικότητα | `H2_PHASE1_AuthorizedRoles_StillWork(Admin)` | ✅ |
| `SuperUser` διατηρεί λειτουργικότητα | `H2_PHASE1_AuthorizedRoles_StillWork(SuperUser)` | ✅ |
| Anonymous → απορρίπτεται | `H2_PHASE1_Anonymous_IsDenied` | ✅ |
| Phase 0 (default) παραμένει log-only | `Phase0_UserRole_IsAllowed_ButLogged` | ✅ |
| Demo token 501 default / enabled | `DemoToken_DisabledByDefault_Returns501`, `DemoToken_WhenExplicitlyEnabled_ReturnsDemoToken` | ✅ |

**Σύνολο `ApiWriteAccessTests`: 16/16 (0 skipped).** Καμία παράκαμψη/αποδυνάμωση ελέγχων.

> **Σημείωση:** Η επαλήθευση αφορά το **απομονωμένο** test env. **Production enforce παραμένει ανενεργό** (Phase 0) μέχρι την έγκριση §7 και τον έλεγχο consumers §9.
