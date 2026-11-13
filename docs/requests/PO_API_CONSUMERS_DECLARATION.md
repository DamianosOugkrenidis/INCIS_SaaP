# Δήλωση PO — Εξωτερικοί API Consumers (E1)

**Από:** Ιδιοκτήτης Έργου (ρόλος PO) · **Προς:** αρχείο έργου INCIS SaaP · **Ημερομηνία:** ____
**Σκοπός:** τεκμηριωμένη βάση για την απόφαση ενεργοποίησης CSRF/WritePolicy enforcement (D1/D2).

> **Κατάσταση: ΠΡΟΣ ΕΛΕΓΧΟ & ΥΠΟΓΡΑΦΗ — τελική έκδοση 2026-11-11.** Καμία επιλογή (Α/Β) **δεν είναι προεπιλεγμένη**· ο Ιδιοκτήτης επιλέγει και υπογράφει. **Δεν έχει δοθεί έγκριση εκ μέρους του.**

---

## 0. Scope — τι ακριβώς πιστοποιείς με την υπογραφή

Με την υπογραφή δηλώνεις **αποκλειστικά** την ύπαρξη ή μη **εξωτερικών API consumers** των *write* endpoints `/api/v1/{clients|suppliers|incis|rawmaterials|restrictions|formulas}` (POST/PUT/DELETE) και τα **όρια του ελέγχου** που διενεργήθηκε. **Δεν** πιστοποιείς: ασφάλεια/απόδοση των endpoints, ούτε την ενεργοποίηση D1/D2 (αυτά έχουν ξεχωριστές αποφάσεις).

| Στοιχείο | Κατάσταση | Πηγή |
|---|---|---|
| Outbound integrations (SMTP email, user-defined webhook `test.ping`, προαιρετικό Azure DataProtection) | **CONFIRMED (κώδικας)** | στατική ανάλυση `docs/THIRD_PARTY_INTEGRATIONS.md` |
| Εσωτερική χρήση των API από την ίδια την εφαρμογή | **NOT FOUND (κώδικας)** | grep `/api/v1/` |
| Auth των API | cookie (Identity)· bearer/JWT **ανενεργό** (`AddJwtBearer` απών) | κώδικας |
| **Εξωτερικοί consumers** | **ΑΠΑΙΤΕΙΤΑΙ ΕΞΩΤΕΡΙΚΗ ΠΡΟΣΒΑΣΗ** (API gateway/WAF logs, CMDB, συνεντεύξεις) — δεν αποδεικνύεται/αποκλείεται από τον κώδικα | — |

> Το τοπικό εύρημα E1 (`docs/reports/e1_local/`) **δεν** τεκμηριώνει επιλογή Α: IIS logs 0 files, firewall logging disabled, gateway/WAF/cloud/CMDB = REQUIRES ACCESS.

---

## 1. Τι είναι επιβεβαιωμένο από τον κώδικα (ΟΧΙ από το περιβάλλον)

Πηγή: `docs/THIRD_PARTY_INTEGRATIONS.md` (στατική ανάλυση).

**Εξερχόμενες (outbound) διασυνδέσεις — γνωστές:**
| Διασύνδεση | Κατάσταση | Απόδειξη |
|---|---|---|
| Email (SMTP) | **CONFIRMED** | `Services/EmailService.cs:44` (`SmtpClient`, `Email:SmtpHost`)· graceful skip αν δεν ρυθμιστεί |
| Webhook δοκιμαστική αποστολή (HTTP POST) | **CONFIRMED** | `Controllers/ApiWebhooksController.cs:199` — προς **URL που ορίζει ο χρήστης** (`test.ping`), όχι hard-coded τρίτος |
| Azure DataProtection storage (Blob/KeyVault) | **PARTIALLY VERIFIED** | Προαιρετικό· χωρίς ρύθμιση → τοπικός φάκελος |
| Άλλα outbound HTTP | **NOT FOUND IN REPOSITORY** | grep `HttpClient/WebRequest/AddHttpClient` → μόνο webhook + OTEL instrumentation |
| Payment/AI/CRM/ERP τρίτα | **NOT FOUND IN REPOSITORY** | — |

**Εισερχόμενες (inbound API consumers):**
| Στοιχείο | Κατάσταση | Απόδειξη |
|---|---|---|
| Εσωτερική χρήση των API endpoints από την ίδια την εφαρμογή | **NOT FOUND IN REPOSITORY** | grep `/api/v1/` → μόνο docs· καμία self-call |
| Εξωτερικοί consumers (εκτός repo) | **REQUIRES ENVIRONMENT ACCESS** | **Δεν** μπορεί να αποδειχθεί/αποκλειστεί από τον κώδικα |
| Auth των API | cookie (Identity) | bearer/JWT **δεν** είναι ενεργό (`AddJwtBearer` απών) |

---

## 2. Τι ΔΕΝ μπορεί να συναχθεί από τον κώδικα

- Η ύπαρξη controllers/endpoints **δεν** αποδεικνύει εξωτερική χρήση.
- Η **απουσία** καταγεγραμμένων κλήσεων στον κώδικα **δεν** αποκλείει consumers στο πραγματικό περιβάλλον.
- Άρα η δήλωση «δεν υπάρχουν εξωτερικοί consumers» **απαιτεί** επιβεβαίωση εύρους από τον Ιδιοκτήτη (δίκτυα, συστήματα, ομάδες, API gateway/WAF logs).

---

## 3. Δήλωση (συμπληρώστε ΕΝΑ)

### Επιλογή Α — Δεν υπάρχουν εξωτερικοί consumers
Δηλώνω υπεύθυνα ότι, μετά από έλεγχο, **δεν υπάρχουν** εξωτερικοί API consumers για τα write endpoints `/api/v1/{clients|suppliers|incis|rawmaterials|restrictions|formulas}`.

- **Εύρος ελέγχου** (συστήματα, δίκτυα, ομάδες, API gateway/WAF logs): ______________________
- **Επιβεβαιώνει (ονοματεπώνυμο/ρόλος):** ______________________  **Ημερομηνία:** ______

### Επιλογή Β — Υπάρχουν consumers (επισυνάπτεται λίστα)
| Σύστημα | Ιδιοκτήτης/Υπεύθυνος | Endpoints | HTTP methods | Auth (cookie/token/άλλο) | Συχνότητα | Περιβάλλον (prod/test) |
|---|---|---|---|---|---|---|
| | | | | | | |

---

## 4. Συνέπεια

Χωρίς υπογεγραμμένη δήλωση, τα `Api:Csrf:Enforce` και `Api:WritePolicy:Enforce` παραμένουν **απενεργοποιημένα** στην παραγωγή (Phase 0, log-only). Η δήλωση αυτή είναι **απαραίτητη αλλά όχι μόνη** προϋπόθεση: απαιτούνται επίσης E2b (14ήμερο logs), E3 (official CI) και E4–E6 (DBA).

**Ονοματεπώνυμο / Ρόλος (PO):** ____________________  **Υπογραφή:** ____________  **Ημερομηνία:** __________

---

## 5. Αναγνώριση ρόλου SEC & D1/D2 (Ιδιοκτήτης PO+SEC)

Ο Ιδιοκτήτης καλύπτει και τον ρόλο **Security Owner (SEC)**. Δηλώνω ότι έλαβα γνώση:
- `Api:Csrf:Enforce=false`, `Api:WritePolicy:Enforce=false`, `Api:DemoToken:Enabled=false` (τρέχουσα κατάσταση).
- Η ενεργοποίηση γίνεται σταδιακά **D1 (CSRF) → D2 (WritePolicy)** μετά E2b/E3/E4–E6.
- Ρόλοι εγγραφής: **Admin, SuperUser, TenantAdmin**· ο `User` δεν έχει δικαίωμα εγγραφής.
- Υπάρχει rollback (flags=false + restart) και monitoring 400/403/5xx.

**SEC υπογραφή:** ____________________  **Ημερομηνία:** __________