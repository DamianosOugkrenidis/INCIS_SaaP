# E1 — Διαδικασία Συλλογής Αποδεικτικών (για την τελική δήλωση PO/SEC)

Σκοπός: τεκμηριωμένη βάση για την επιλογή **Α** ή **Β** στο `docs/requests/PO_API_CONSUMERS_DECLARATION.md`.
Αρχή: η **απουσία** ευρημάτων στον κώδικα δεν αποδεικνύει απουσία consumers· απαιτείται έλεγχος του πραγματικού περιβάλλοντος.

## 1. Επιβεβαιωμένα από τον κώδικα (όχι από το περιβάλλον)
| Στοιχείο | Κατάσταση | Απόδειξη |
|---|---|---|
| In-repo consumers των `/api/v1/*` writes | **NOT FOUND** | grep `/api/v1/` → μόνο docs· καμία self-call |
| Outbound SMTP email | **CONFIRMED** | `Services/EmailService.cs:44` |
| Outbound webhook (test-ping) | **CONFIRMED** | `Controllers/ApiWebhooksController.cs:199` — URL χρήστη |
| Άλλα outbound HTTP / payment / AI / CRM | **NOT FOUND** | grep `HttpClient/WebRequest/AddHttpClient` |
| API auth | cookie (Identity) | `AddJwtBearer` απών· bearer demo μόνο |
| Phase 0 logging filters | **CONFIRMED (κώδικας)** | `ApiWritePolicyFilter`, `ApiCsrfFilter` |

## 2. Απαιτεί πρόσβαση περιβάλλοντος (πρέπει να ελεγχθεί)
Ελέγξτε και τεκμηριώστε για κάθε πηγή:
1. **API Gateway / Reverse Proxy / WAF** (nginx, IIS ARR, Azure App Gateway/Front Door, Cloudflare, F5): access logs τελευταίων 30 ημερών.
2. **Firewall / egress logs**: outbound κλήσεις προς εξωτερικά APIs.
3. **Cloud activity / audit logs** (Azure/AWS/GCP): Function Apps, Logic Apps, Automation Accounts, service principals.
4. **Scheduled tasks**: Windows Task Scheduler, cron/systemd timers, Power Automate, RPA bots.
5. **Integration registry / CMDB / contracts**: καταγεγραμμένα integrations, owners, SLA.
6. **Service/API credentials inventory**: ποιοι λογαριασμοί/ρόλοι χρησιμοποιούνται για machine κλήσεις.
7. **Webhook subscribers**: πίνακας `WebhookSubscriptions` (URLs, events, tenant) — outbound προς τρίτους.
8. **DNS / certificates / hostnames**: τυχόν dedicated API hostnames.
9. **Ομάδες/άνθρωποι**: συνεντεύξεις με DevOps, IT, RPA owners, εξωτερικούς vendors.
10. **App Phase 0 logs** (E2b) ως cross-check: κλήσεις προς `/api/v1/*` ανά ρόλο.

## 3. Queries ανίχνευσης (προσαρμόστε στα logs σας)
Ζητούμενο: όλες οι κλήσεις **POST/PUT/PATCH/DELETE** προς `/api/v1/{clients|suppliers|incis|rawmaterials|restrictions|formulas}` το τελευταίο 30ήμερο.

Παραδείγματα:
- **nginx/IIS access log:** φίλτρο `RequestMethod IN (POST,PUT,PATCH,DELETE)` και `URL` contains `/api/v1/`.
- **Azure Front Door/WAF:** `HttpRequestMethod` + `RequestUri`.
- **Cloud audit:** `OperationName` contains API/HTTP + resource = INCIS.
- **Task Scheduler:** `Get-ScheduledTask | Where-Object { $_.Actions.Execute -match 'curl|Invoke-RestMethod|python|node|powershell' }`.
- **Linux:** `crontab -l`, `systemctl list-timers`.
- **Power Automate:** flow list με HTTP actions προς το INCIS host.

Πίνακας συλλογής (συμπληρώστε ανά εύρημα):
`Ημερομηνία | Host/URL | Method | Client (IP/User-Agent/Service) | Auth (cookie/token) | Ρόλος | Αποτέλεσμα | Ιδιοκτήτης`

## 4. Κριτήρια απόφασης
- **Επιλογή Α** μόνο αν: όλες οι παραπάνω πηγές ελέγχθηκαν, το εύρος τεκμηριώνεται, και **δεν** βρέθηκαν εξωτερικές κλήσεις.
- **Επιλογή Β** αν βρεθεί έστω ένας consumer — καταγράψτε τον πλήρη πίνακα και ενημερώστε το migration plan (CSRF token / bearer / εξαίρεση με χρονικό όριο).
- Ανεξάρτητα από την επιλογή: το E2b (Phase 0 logs) παραμένει απαραίτητο.

## 5. Αποδεικτικά που επισυνάπτονται στη δήλωση
- Export CSV/JSON των queries (χωρίς tokens/PII).
- Screenshots/τιμοκατάλογος πηγών που ελέγχθηκαν + ημερομηνίες.
- Δήλωση εύρους (συστήματα/δίκτυα/ομάδες) + ονοματεπώνυμο υπευθύνου.
- SHA256 των exports (ακεραιότητα).

## 6. Παραπομπές
`THIRD_PARTY_INTEGRATIONS.md` · `PO_API_CONSUMERS_DECLARATION.md` · `PHASE0_LOG_ANALYSIS_INSTRUCTIONS.md` · `DISPATCH_PACKAGE.md`.
