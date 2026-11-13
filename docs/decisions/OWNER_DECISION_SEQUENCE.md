# Owner Decision Sequence — INCIS SaaP (τελικό κλείσιμο)

Σύντομη, με σωστή σειρά λίστα των αποφάσεων που λαμβάνει **μόνο ο Ιδιοκτήτης (PO+SEC)**.
Δεν επαναλαμβάνονται ολοκληρωμένες εργασίες. Κάθε απόφαση αναφέρει: **τι ξεκλειδώνει**, **πού υπογράφεται**, **προϋπόθεση**.

---

## Τι έχει ήδη ολοκληρωθεί (ΜΗΝ το ξαναζητήσεις)
- Καθαρό, deterministic, buildable **RC2** πακέτο: `dist/v18_RC2_20261111.zip` (SHA256: βλ. `dist/v18_RC2_20261111.zip.sha256`· scan FAIL=0).
- Release-hygiene inventory 9 stray logs (`docs/reports/release_hygiene/STRAY_LOGS_INVENTORY.md`) — **rotation δεν απαιτείται**.
- Πραγματικά test evidence + ενοποίηση αποτελεσμάτων (693 xUnit + 54 Playwright, `docs/reports/rc1_2026-11-11/`).
- Έτοιμα εξωτερικά πακέτα: `docs/operations/PHASE0_DEPLOYMENT_PACKAGE.md`, `docs/requests/E3_REPO_OWNER_PACKET.md`, `docs/requests/E4_E6_DBA_PACKET.md`.
- Εργαλεία: `tools/build-release.ps1`, `tools/scan-release-artifacts.ps1`, `tools/analyze-phase0-logs.ps1`, `tools/verify-repo-publish.ps1`, `tools/verify-ci-artifacts.ps1`.

---

## Αποφάσεις με τη σωστή σειρά

| # | Απόφαση | Τι ξεκλειδώνει | Πού υπογράφεται | Προϋπόθεση |
|---|---|---|---|---|
| **1** | Εξουσιοδότηση αποστολής (ορισμός D0) των 4 αιτημάτων προς DevOps / Repo Owner / DBA (+ ποιος στέλνει) | Έναρξη παράλληλων λωρίδων E2b/E3/E4–E6 | `docs/requests/DISPATCH_PACKAGE.md` (καταγραφή ημερομηνίας) | — |
| **2** | **E1** — επιλογή **Α** ή **Β** + εύρος ελέγχου + υπογραφή PO & SEC | Βάση για D1/D2· κλείνει το E1 | `docs/requests/PO_API_CONSUMERS_DECLARATION.md` | Εξωτερικός έλεγχος gateway/WAF/CMDB για επιλογή Α |
| **3** | Έγκριση **Phase 0 deploy** (log-only, flags false) + παράθυρο | **Ξεκινά το 14ήμερο E2b** (κρίσιμος πόλος χρόνου) | `docs/operations/PHASE0_DEPLOYMENT_PACKAGE.md` §8 | Έλεγχος πακέτου/flags/rollback (έτοιμο) |
| **4** | **D5** — εξουσιοδότηση δημοσίευσης repo | Ξεκλειδώνει **E3** (push + official CI) | `docs/decisions/PO_SEC_DECISION_PACKAGE.md` (D5) | Review `publish_candidates.csv` + tracked/secrets check |
| **5** | **D4/D6/D3** — αποδοχή κινδύνων M/L + labeling ML + κατεύθυνση API auth | Κλείνει D3/D4/D6· απαραίτητα πριν D1 | `docs/decisions/PO_SEC_DECISION_PACKAGE.md` (D3/D4/D6) | Δικό σου review |
| **6** | Αποδοχή αποδεικτικών **E2b + E3 + E4–E6** (όπως παραλαμβάνονται) | Πληρότητα για D1 | `docs/requests/EXTERNAL_EVIDENCE_TRACKER.md` (κλείσιμο γραμμών) | 3 reports· CI PASS· 11 DBA outputs |
| **7** | **D1** — έγκριση `Api__Csrf__Enforce=true` + επαλήθευση | CSRF enforcement (κλείνει R2) | `docs/decisions/PO_SEC_DECISION_PACKAGE.md` (D1) + runbook | E1–E6 + δοκιμή σε staging |
| **8** | **D2** — έγκριση `Api__WritePolicy__Enforce=true` (μετά D1 σταθερό 24–48h) + επαλήθευση | WritePolicy enforcement (κλείνει H-2) | `docs/decisions/PO_SEC_DECISION_PACKAGE.md` (D2) + runbook | D1 σταθερό + monitoring |
| **9** | Έγκριση **παραθύρου εμπορικής διάθεσης** RC2 + migrations + 4 tenants | Deploy + onboarding 4 πελατών | `docs/operations/COMMERCIAL_LAUNCH_RUNBOOK.md` (έγκριση) | E4–E6 + D1/D2 (ή συνειδητή σειρά) |
| **10** | Υπογραφή go-live checklist **ανά tenant** | Παράδοση σε κάθε πελάτη | `docs/operations/COMMERCIAL_LAUNCH_RUNBOOK.md` §10 | Deploy + smoke ανά tenant |
| **11** | Τελικό **GO sign-off** | Εμπορική διάθεση | `FINAL_COMMERCIAL_GO_NO_GO.md` / το παρόν | Όλα τα παραπάνω |

---

## Προαιρετικές αποφάσεις (χαμηλής προτεραιότητας)
- **Quarantine/διαγραφή** των 9 stray logs από το source: **προαιρετικό** (το RC2 τα αποκλείει ήδη). Απαιτεί έγκριση· προτείνεται η μη-παρέμβαση.
- Αποδοχή του **RC2** ως τελικού πακέτου (packaging-only αλλαγή έναντι RC1: αφαίρεση stray logs/launchSettings + σωστό layout).

---

## Κρίσιμη παρατήρηση χρονοδιαγράμματος
Οι αποφάσεις **2, 3** μπορούν να ληφθούν **άμεσα** και ανεξάρτητα. Η απόφαση **3** ξεκινά τον μεγαλύτερο πόλο (14ήμερο) — γι' αυτό προηγείται των υπολοίπων. Οι αποφάσεις **7, 8** έπονται υποχρεωτικά των αποδεικτικών (6).

## Παραπομπές
`FINAL_CLOSURE_ACTION_PLAN.md` · `FINAL_APPROVALS_AND_ACTIONS.md` · `docs/decisions/PO_SEC_DECISION_PACKAGE.md` · `docs/security/ENFORCEMENT_ACTIVATION_RUNBOOK.md`.
