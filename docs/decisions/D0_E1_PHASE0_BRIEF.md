# Decision Brief — Αποφάσεις 1–3 (D0 · E1 · Phase 0)

Για: **Ιδιοκτήτη (PO+SEC)** · Ημερομηνία: 2026-11-11 · Βάση: υποψήφιο **RC2** `dist/v18_RC2_20261111.zip` (SHA256: `dist/v18_RC2_20261111.zip.sha256`).
Πεδίο: **τοπικός υπολογιστής (PC)** ως αρχικός production node για **4 tenants**.

> Το RC2 είναι **υποψήφιο** release package — **όχι** εγκεκριμένη παραγωγική έκδοση. Καμία ενέργεια δεν εκτελείται χωρίς ξεχωριστή υπογραφή σου ανά απόφαση.
> Οι `Api__Csrf__Enforce` και `Api__WritePolicy__Enforce` παραμένουν **`false`**.

---

## ΑΠΟΦΑΣΗ 1 — D0: Εξουσιοδότηση αποστολής αιτημάτων
**Τι αποφασίζεις:** να δοθεί το «πράσινο φως» ώστε να σταλούν (email/ticketing) τα 3 εξωτερικά αιτήματα προς **DevOps / Repo Owner / DBA** και να οριστεί **D0** (ημερομηνία έναρξης). Σε τοπικό PC, οι ρόλοι ενδέχεται να ασκούνται από τον **ίδιο operator** (επιτρέπεται).

**Έγγραφο/σημείο υπογραφής:** `docs/requests/DISPATCH_PACKAGE.md` → συμπλήρωση στήλης «Sent (ημερομηνία)» + ορισμός **D0**.
**Αποδεικτικό ολοκλήρωσης:** καταγραφή αποστολής (ημερομηνία/παραλήπτης) στον πίνακα του DISPATCH_PACKAGE + `docs/requests/STATUS_LEDGER.md`.

**Τι ξεκλειδώνει:** παράλληλη έναρξη E2b (DevOps), E3 (Repo Owner, μετά D5), E4–E6 (DBA).
**Δεν** συνεπάγεται άδεια deploy/push/flag.

---

## ΑΠΟΦΑΣΗ 2 — E1: Δήλωση PO/SEC για εξωτερικούς API consumers
**Τι αποφασίζεις:** επιλογή **Α** ή **Β** + εύρος ελέγχου + υπογραφή. **Δεν επιλέγεται από εμάς.**

**Έγγραφο/σημείο υπογραφής:** `docs/requests/PO_API_CONSUMERS_DECLARATION.md`
- §3 «Επιλογή Α» ή «Επιλογή Β» (συμπλήρωση + πεδία εύρους),
- τελικές γραμμές: **Υπογραφή PO** + **Υπογραφή SEC** + ημερομηνία.

### Επιλογή Α — «Δεν υπάρχουν εξωτερικοί API consumers»
**Τι πιστοποιείς:** ότι, μετά από έλεγχο, **δεν** υπάρχει εξωτερικό σύστημα που καλεί τα *write* endpoints `/api/v1/{clients|suppliers|incis|rawmaterials|restrictions|formulas}` (POST/PUT/DELETE).
**Αποδεικτικά που απαιτούνται:**
- **Εύρος ελέγχου:** ποια συστήματα/δίκτυα/ομάδες ελέγχθηκαν· **API gateway/WAF logs**· έλεγχος egress/inbound· CMDB/integration registry.
- Επιβεβαίωση ότι δεν υπάρχουν self-calls (από την ίδια την εφαρμογή) προς τα endpoints.
- **Ονοματεπώνυμο/ρόλος** αυτού που το επιβεβαιώνει + ημερομηνία.
> Χωρίς τεκμηριωμένο εύρος, η Επιλογή Α **δεν** συνιστάται (δεν αποδεικνύεται από τον κώδικα).

### Επιλογή Β — «Υπάρχουν consumers»
**Τι πιστοποιείς:** ότι υπάρχουν εξωτερικά συστήματα και τα δηλώνεις.
**Αποδεικτικά που απαιτούνται:** συμπληρωμένος πίνακας για **κάθε** consumer: `Σύστημα · Ιδιοκτήτης · Endpoints · HTTP methods · Auth (cookie/token/άλλο) · Συχνότητα · Περιβάλλον (prod/test)`.

**Συνέπεια (και για τις δύο):**
- Οι δηλώσεις είναι **απαραίτητες αλλά όχι μόνες** προϋποθέσεις για D1/D2 (απαιτούνται και E2b/E3/E4–E6).
- Επιλογή Β με **cookie clients** → μετά D1 θα χρειάζονται `X-CSRF-TOKEN`· **machine clients** → απαιτούν μηχανισμό auth (D3, bearer/JWT).

**Τι έχει επιβεβαιωθεί από κώδικα (όχι από περιβάλλον):** outbound = SMTP email, user-defined webhook `test.ping`, προαιρετικό Azure DataProtection· auth API = cookie (bearer/JWT ανενεργό)· **καμία** εσωτερική χρήση των `/api/v1/` από τον κώδικα. **Εξωτερικοί consumers: ΑΠΑΙΤΕΙΤΑΙ ΕΞΩΤΕΡΙΚΗ ΠΡΟΣΒΑΣΗ.**

---

## ΑΠΟΦΑΣΗ 3 — Phase 0: Έγκριση log-only deployment (τοπικό PC)
**Τι αποφασίζεις:** να εγκρίνεις **deploy του RC2 στον τοπικό PC** με τα δύο security filters σε **log-only** (enforcement **OFF**) και να ορίσεις **παράθυρο εκτέλεσης**. **Δεν** εγκρίνεις enforcement.

**Έγγραφο/σημείο υπογραφής:** `docs/operations/PHASE0_DEPLOYMENT_PACKAGE.md` → **§8 «Έγκριση»**:
`☐ Εγκρίνω το Phase 0 deploy ως έχει · ☐ Ζητώ αλλαγές` + Ονοματεπώνυμο/ρόλος + Ημερομηνία + **Παράθυρο**.

**Scope (ακριβές):**
- Target: **τοπικός PC** (1 instance· SQL Express· DB `INCIS_SaaP`· LAN/localhost).
- Flags (υποχρεωτικά): `Api__Csrf__Enforce=false`, `Api__WritePolicy__Enforce=false`, `Api__DemoToken__Enabled=false`, `Security__EnforceHttps=true`.
- **Καμία** ενεργοποίηση enforcement· **καμία** παραγωγική αλλαγή δεδομένων.

**Verification (αποδεικνύει OFF + presence):** V1 `/health`=200 · V3 flags=false/false/false · V4 cookie POST χωρίς token **δεν** δίνει 400 + log `OBSERVATION` · V5 write ως `User` **δεν** δίνει 403 + log `OBSERVATION` · V7 markers παρόντες · V8 0 αύξηση 5xx.

**Rollback (άμεσο):** `Enforce=false` + restart (<2′)· ή redeploy προηγούμενου artifact· **χωρίς** restore DB.

**Τι ξεκλειδώνει:** την έναρξη του **14ημέρου E2b** (κρίσιμος πόλος χρόνου) — τα logs συλλέγονται από τα τοπικά Serilog files του PC.

**Απαγορεύσεις που ισχύουν καθ' όλη τη φάση:** κανένα deploy χωρίς την υπογραφή σου· κανένα push/publish· flags `false`· καμία διαγραφή των 9 source logs ή αλλαγή σε backups/datasets/v16/προηγούμενα releases· **όχι** τελικό GO.

---

## Σειρά & σημεία υπογραφής (σύνοψη)

| # | Απόφαση | Έγγραφο | Σημείο υπογραφής | Άμεσα δυνατό |
|---|---|---|---|---|
| 1 | D0 — αποστολή αιτημάτων | `docs/requests/DISPATCH_PACKAGE.md` | στήλη «Sent» + D0 | ✅ |
| 2 | E1 — Α ή Β | `docs/requests/PO_API_CONSUMERS_DECLARATION.md` | §3 + υπογραφές PO/SEC | ✅ |
| 3 | Phase 0 — log-only deploy | `docs/operations/PHASE0_DEPLOYMENT_PACKAGE.md` | §8 | ✅ (μετά έλεγχο) |

> Οι αποφάσεις **1–3** είναι ανεξάρτητες και μπορούν να ληφθούν **άμεσα**. Οι αποφάσεις 4–11 (`docs/decisions/OWNER_DECISION_SEQUENCE.md`) έπονται.
