# CSRF — Τελική Αξιολόγηση & Υλοποιημένη Λύση

Ημερομηνία: 2026-10-20 · Σχετικό εύρημα: R2 (`RISK_REGISTER.md`).
Κατάσταση: **Phase 0 υλοποιήθηκε (log-only, default off)** — production συμπεριφορά αμετάβλητη· Phase 1 με έγκριση.

## 1. Δεδομένα συστήματος
- Αυθεντικοποίηση: **cookie** (ASP.NET Identity)· **δεν** υπάρχει ενεργό JWT bearer.
- Cookie: **SameSite=Lax** (`Program.cs:461`), `LoginPath=/Account/Login`, `AccessDeniedPath=/Account/AccessDenied`.
- API write endpoints (`ApiExtendedController`): **χωρίς** `[ValidateAntiForgeryToken]` (API-style).
- MVC POST actions (π.χ. Import): **με** `[ValidateAntiForgeryToken]`.
- CSP ορίζεται (`Program.cs:859-861`).

## 2. Τι καλύπτει και τι ΟΧΙ το SameSite=Lax
| Σενάριο | Αποτέλεσμα |
|---|---|
| Cross-site `<form method=POST>` προς `/api/v1/*` | **Μπλοκάρεται** (το Lax δεν στέλνει cookie σε cross-site POST) |
| Cross-site fetch/XHR με credentials | **Μπλοκάρεται** (no cookie) |
| Top-level cross-site **GET** πλοήγηση | Στέλνεται cookie (ασφαλές για idempotent GETs) |
| **Same-site** (subdomain) κακόβουλη σελίδα | **Δεν μπλοκάρεται** (Lax ≠ Strict) |
| **XSS** στο ίδιο origin | **Δεν μπλοκάρεται** (τα CSRF tokens επίσης δεν προστατεύουν από XSS) |
| Παλαιότεροι browsers χωρίς SameSite | Εξαρτάται από defaults |

**Συμπέρασμα:** το `SameSite=Lax` είναι **σημαντικός μετριασμός**, **όχι πλήρης απόδειξη προστασίας** για όλες τις διαδρομές/σενάρια.

## 3. Εκτίμηση κινδύνου
- **Πιθανότητα:** Χαμηλή (απαιτεί same-site/XSS ή bypass SameSite).
- **Επίδραση:** Μέτρια–Υψηλή (state-changing API writes: clients/suppliers/incis/rawmaterials/restrictions/formulas).
- **Τρέχουσα κατάσταση:** **OPEN (Medium)** — τεκμηριωμένο, δεν αποκρύπτεται.

## 4. Απαιτούμενη αντιμετώπιση (πριν/μετά το H-2)
1. **Επιβεβαίωση cookie flags:** `HttpOnly`, `Secure` (σε HTTPS), `SameSite=Lax` — ελέγχεται στο deployment.
2. **Επιλογή μηχανισμού για API writes:**
   - (α) antiforgery token (double-submit) για cookie-based API, **ή**
   - (β) μετάβαση API auth σε **bearer/JWT** (τότε CSRF δεν εφαρμόζεται, αλλά απαιτείται θωράκιση token).
3. **CSP/XSS:** διατήρηση CSP· έλεγχος output encoding (XSS προϋπόθεση για same-origin CSRF).
4. **Τεκμηρίωση αποδοχής κινδύνου** αν επιλεγεί προσωρινή παραμονή σε Lax-only, με χρονικό όριο.

## 5. Tests/αποδεικτικά
- `ApiWriteStatusCodesTests.ApiWrites_DoNotUseAntiforgeryToken_MvcImportDoes` — επιβεβαιώνει ότι τα API writes είναι χωρίς antiforgery και τα MVC Import με.
- `ApiWriteStatusCodesTests.Anonymous_ApiWrite_Returns401` — ανώνυμος αποκλείεται.
- Cookie ρύθμιση: `Program.cs:461` (SameSite=Lax).

> Το CSRF **δεν** θεωρείται «κλειστό» μόνο λόγω SameSite· απαιτείται ρητή απόφαση §4 πριν το GO.

---

## 6. Υλοποιημένη λύση (Phase 0) — αποδεικτικά
| Στοιχείο | Υλοποίηση |
|---|---|
| Ρυθμίσεις | `Configuration/ApiCsrfOptions.cs` — `Api:Csrf:Enforce` (default **false**), `HeaderName` (`X-CSRF-TOKEN`) |
| Filter | `Filters/ApiCsrfFilter.cs` (global, μόνο `ApiExtended` POST/PUT/PATCH/DELETE) |
| Phase 0 (`Enforce=false`) | **Log-only** (`API-CSRF PHASE-0 OBSERVATION`) — **δεν** αποκλείει |
| Phase 1 (`Enforce=true`) | **400** για απόν/άκυρο token (`IAntiforgery.ValidateRequestAsync`) |
| Production | `Api:Csrf:Enforce=false` (αμετάβλητη συμπεριφορά) |
| Tests | `ApiCsrfTests` **8/8**: Phase 0 allow+log, Phase 1 400, valid token allow, HTTP default allow, HTTP Phase-1 400, cookie SameSite=Lax/HttpOnly/Secure, antiforgery header/HttpOnly, CSP header |

## 7. Σύγκριση λύσεων (για την τελική απόφαση)
| Κριτήριο | Antiforgery (cookie + token) | Bearer/JWT |
|---|---|---|
| Ωρίμανση/κόστος | Μικρό (υπάρχει `IAntiforgery`, header έτοιμο) | Μεγάλο (πραγματική έκδοση/ανανέωση/ανάκληση· σήμερα demo μόνο) |
| Συμβατότητα υφιστάμενων consumers | Απαιτεί αποστολή token (breaking για API clients) | Breaking (νέο auth) |
| CSRF | Εξαλείφεται (token) | Δεν εφαρμόζεται |
| XSS | Δεν προστατεύει (CSP) | Δεν προστατεύει (CSP) |
| Σύσταση | **Phase 1 μετά E1/E2** (staged, config flag) | Μακροπρόθεσμα, με ξεχωριστό έργο token lifecycle |

**Πρόταση:** 유지 `SameSite=Lax` + `HttpOnly` + `Secure` + CSP **και** ενεργοποίηση `Api:Csrf:Enforce=true` (Phase 1) μετά την επιβεβαίωση consumers, με παράλληλο σχέδιο μετάβασης σε bearer για machine clients. Καμία επιλογή bearer χωρίς lifecycle (issue/refresh/revoke).

## 8. Συνδυασμός CSRF + H-2 (tests)
`CsrfH2CombinationTests` (7/7):
| Σενάριο | Αποτέλεσμα |
|---|---|
| Both enforced, Admin, missing CSRF | **400** (CSRF πριν το policy) |
| Both enforced, User, missing CSRF | **400** (όχι 403 — σειρά filters) |
| CSRF off, policy on, User | **403** |
| Both enforced, valid token (simulated), Admin | **2xx** |
| Both enforced, invalid token | **400** |
| Both enforced, **GET/HEAD** | **δεν απορρίπτονται** (HEAD 405 = routing, όχι security) |
| Both enforced, anonymous | **401** |

## 9. Migration plan για API clients (πριν το Phase 1)
1. Καταγραφή clients (E1) και ποιοι χρησιμοποιούν cookie auth.
2. Για cookie clients: παροχή `X-CSRF-TOKEN` (double-submit) πριν `Csrf.Enforce=true`.
3. Machine clients: μετάβαση σε bearer (lifecycle) ή εξαίρεση με χρονικό όριο/έγκριση.
4. **Rollback:** `Api__Csrf__Enforce=false` (και `Api__WritePolicy__Enforce=false`) + restart — άμεσο, χωρίς schema αλλαγή· χρονικά περιορισμένο/εγκεκριμένο.
5. Παρακολούθηση: `API-CSRF DENY` / `PHASE-0 OBSERVATION` logs.
