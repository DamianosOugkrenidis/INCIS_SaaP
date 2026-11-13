# Τεχνική Αναφορά — Διασυνδέσεις με Τρίτα Συστήματα

Ημερομηνία: 2026-10-20 · Μέθοδος: στατική ανάλυση πηγαίου κώδικα, εξαρτήσεων και ρυθμίσεων.
Εύρος ελέγχου: `INCIS_SaaP/` (κώδικας εφαρμογής), `INCIS_SaaP.csproj`, `appsettings.json`, `Dockerfile`, `docker-compose*`, scripts.
Καταστάσεις: **CONFIRMED** · **NOT FOUND IN REPOSITORY** · **PARTIALLY VERIFIED** · **REQUIRES ENVIRONMENT ACCESS**

## 1. Εξερχόμενες κλήσεις (outbound)
| Στοιχείο | Κατάσταση | Απόδειξη | Σημείωση |
|---|---|---|---|
| Webhook δοκιμαστική αποστολή (HTTP POST) | **CONFIRMED** | `Controllers/ApiWebhooksController.cs:199` (`new HttpClient`) — μόνο για το `test.ping` | Ο προορισμός είναι **URL που ορίζει ο χρήστης**, όχι hard-coded τρίτος |
| Email (SMTP) | **CONFIRMED** | `Services/EmailService.cs:44` (`SmtpClient`, `Email:SmtpHost`) | Εξωτερικός SMTP· graceful skip αν δεν ρυθμιστεί |
| Άλλα outbound HTTP | **NOT FOUND IN REPOSITORY** | grep `HttpClient/WebRequest/AddHttpClient` → μόνο webhook + OTEL instrumentation | — |
| Payment gateway (Stripe/PayPal κ.λπ.) | **NOT FOUND IN REPOSITORY** | Μόνο *πεδία* `Tenant.PaymentMethod`/`ExternalPaymentReference` (`Models/Tenant.cs:207,213`) | Δεν υπάρχει client/ένταξη |
| Εξωτερικά AI/ML APIs (OpenAI/Azure AI/ONNX/Python) | **NOT FOUND IN REPOSITORY** | grep μόνο σχόλιο στο `SemanticSearchService.cs:118` («can be upgraded to…») | Δεν υπάρχει κλήση |
| Payment/CRM/ERP άλλα τρίτα | **NOT FOUND IN REPOSITORY** | — | — |

## 2. Εισερχόμενες κλήσεις (inbound API consumers)
| Στοιχείο | Κατάσταση | Απόδειξη |
|---|---|---|
| Εσωτερική χρήση των δικών του API endpoints (self-calls) | **NOT FOUND IN REPOSITORY** | grep `/api/v1/` σε κώδικα → μόνο docs· καμία `HttpClient` προς τοπικά endpoints |
| Εξωτερικοί consumers (εκτός repo) | **REQUIRES ENVIRONMENT ACCESS** | Δεν μπορεί να αποδειχθεί/αποκλειστεί από τον κώδικα· βλ. ερωτηματολόγιο |
| Swagger/OpenAPI που διαφημίζει Bearer | **CONFIRMED (τεκμηρίωση)** | `Program.cs` security definition `Bearer`· **αλλά** δεν υπάρχει `AddJwtBearer` → μη λειτουργικό |

## 3. Background jobs / αυτοματισμοί
| Στοιχείο | Κατάσταση | Απόδειξη | Εξωτερική επικοινωνία |
|---|---|---|---|
| Hangfire recurring jobs (6) | **CONFIRMED** | `Services/BackgroundJobs.cs:170-203` | Όχι (DB-internal) |
| ML retrain job (εβδομαδιαίο + on-demand) | **CONFIRMED** | `BackgroundJobs.cs:31-45,200`; `FormulaAnalysisController.cs:100` | Όχι (τοπικό training) |
| OutboxPattern → MessageBus | **CONFIRMED (in-process)** | `Program.cs:274` `InProcessMessageBus`; `Architecture/OutboxPattern/OutboxMessage.cs:132` | **In-process**, όχι broker |
| Πραγματική αποστολή webhooks για events | **PARTIALLY VERIFIED** | Δημιουργούνται `WebhookDelivery` rows· outbound HTTP εντοπίστηκε μόνο στο test endpoint | Χρειάζεται επιβεβαίωση/υλοποίηση dispatcher |
| Scheduled OS tasks/scripts | **NOT FOUND IN REPOSITORY** | — | — |

## 4. Εξαρτήσεις υποδομής (όχι τρίτα συστήματα με την έννοια integration)
- **SQL Server** (EF Core) — κύρια βάση.
- **Azure.Extensions.DataProtection.Blobs/Keys + Azure.Identity** — *προαιρετική* αποθήκευση DataProtection keys (Blob/KeyVault). Χωρίς ρύθμιση → τοπικός φάκελος.
- **OpenTelemetry** — Console exporter μόνο (δεν αποστέλλει σε εξωτερικό collector).
- **CSP** (`Program.cs:859-861`) — CDN για front-end assets (jsdelivr/cdnjs/jquery/datatables/google fonts): **client-side** εξάρτηση, όχι server integration.
- **CosIng δεδομένα** — τοπικά CSV/JSON· η αναφορά `ec.europa.eu` είναι τεκμηρίωση πηγής (`Models/CosIngEntry.cs:8`), όχι runtime κλήση.

## 5. Συμπέρασμα (τεκμηριωμένο)
- **Δεν εντοπίστηκε** στον κώδικα καμία διασύνδεση με τρίτες εφαρμογές/υπηρεσίες πέραν: (α) **SMTP email**, (β) **webhooks προς URL του χρήστη**, (γ) προαιρετικού **Azure DataProtection storage**.
- **Δεν εντοπίστηκε** εσωτερική χρήση των API endpoints από την ίδια την εφαρμογή.
- Η ύπαρξη controllers/endpoints **δεν** αποδεικνύει εξωτερική χρήση.
- **Δεν μπορεί να αποκλειστεί** ύπαρξη εξωτερικών API consumers στο πραγματικό περιβάλλον → **REQUIRES ENVIRONMENT ACCESS** (ερωτηματολόγιο + Phase 0 logs).

## 6. Επόμενο βήμα
Αποστολή `docs/EXTERNAL_API_CONSUMERS_QUESTIONNAIRE.md` στον υπεύθυνο INCIS + ανάλυση Phase 0 logs (`docs/PHASE0_LOG_ANALYSIS_INSTRUCTIONS.md`).
