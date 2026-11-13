# Τεκμηρίωση Απόφασης H-1 / H-2 — Πολιτική Πρόσβασης Εγγραφών

Ημερομηνία: 2026-10-20 · Κατάσταση: **H-1 = ΕΦΑΡΜΟΣΤΗΚΕ** · **H-2 = ΑΝΟΙΚΤΟ (απαιτεί απόφαση)**

## ΜΕΡΟΣ Α — H-1 (MVC POST Create/Edit/Delete) — ✅ ΕΦΑΡΜΟΣΤΗΚΕ

### Επαλήθευση προϋποθέσεων (από τον κώδικα)
| Προϋπόθεση | Εύρημα | Απόδειξη |
|---|---|---|
| `TenantAdmin` δεν χρειάζεται δικαιώματα εγγραφής στο υποσύστημα | Ο `TenantAdmin` χρησιμοποιείται **μόνο** για διαχείριση χρηστών/εταιρείας, **πουθενά** σε master data | `UsersController.cs:17,49,124,160`, `AccountController.cs:307,332`, `ProfileController.cs:130`, `_ERPLayout.cshtml:184` |
| Ο `User` δεν βασίζεται σε υφιστάμενη ροή δημιουργίας/τροποποίησης INCI | Απονέμεται στην εγγραφή ως μέλος εταιρείας· **καμία** controller/view ροή δεν του δίνει εγγραφή· τα κουμπιά στα Details views είναι gated σε Admin/SuperUser | `AccountController.cs:339`, `Views/*/Details.cshtml:IsInRole("Admin"/"SuperUser")` |
| Οι περιορισμοί εφαρμόζονται server-side | Τα GET ήταν ήδη `[Authorize(Roles=...)]` server-side· το patch προσθέτει τον ίδιο περιορισμό στα POST | βλ. «Αλλαγές» |

### Ενέργεια
Προστέθηκε `[Authorize(Roles = AppRoles.Admin + "," + AppRoles.SuperUser)]` στα POST Create/Edit/Delete (server-side):

| Controller | POST actions |
|---|---|
| INCIsController | Create, Edit, DeleteConfirmed |
| ClientsController | Create, Edit, DeleteConfirmed |
| SuppliersController | Create, Edit, DeleteConfirmed |
| RawMaterialsController | Create, Edit, DeleteConfirmed, AddSample |
| RestrictionsController | Create, Edit, DeleteConfirmed |
| FormulasController | Create, Edit, DeleteConfirmed |

**Σύνολο: 19 POST actions.** Ευθυγράμμιση με `WriteRoles = Admin,SuperUser` και με τα αντίστοιχα GET.

### Επίπτωση
- `Admin`/`SuperUser`: ανεπηρέαστοι (και στο UI ήταν οι μόνοι που έβλεπαν τα κουμπιά).
- `User`: απορρίπτεται server-side (δεν υπήρχε υποστηριζόμενη ροή UI).
- `TenantAdmin`: απορρίπτεται (δεν είχε ποτέ ρόλο στο υποσύστημα).
- Καμία αλλαγή σε GET/αναγνώσεις, αναφορές, εξαγωγές, Public APIs που δεν γράφουν.

### Tests
`AccessPolicyTests` — **11/11** (0 skipped): policy constant, User denied (GET pages + POST create, με έλεγχο μη-αποθήκευσης), Admin allowed (control), anonymous denied. Το προηγούμενο skipped target-policy test **ενεργοποιήθηκε**.

---

## ΜΕΡΟΣ Β — H-2 (API write endpoints) — 🔶 ΑΝΟΙΚΤΟ

### Εύρημα
Το `ApiExtendedController` εκθέτει εγγραφές **με μόνο `[Authorize]`** (χωρίς role), σε αντίθεση με το `ApiProductManagementController` που χρησιμοποιεί `WriteRoles`:

| Endpoint group | Μέθοδοι |
|---|---|
| `/api/v1/clients` | POST, PUT, DELETE |
| `/api/v1/suppliers` | POST, PUT, DELETE |
| `/api/v1/incis` | POST, PUT, DELETE |
| `/api/v1/rawmaterials` (+ copy, change-supplier, samples) | POST, PUT, DELETE |
| `/api/v1/restrictions` | POST, PUT, DELETE |
| `/api/v1/formulas/{id}` (validate/invalidate/wip) | POST |

### Κίνδυνος
Ίδιος με H-1 (ρόλος `User`/οποιοσδήποτε authenticated μπορεί να γράψει master data μέσω API). **Επιπρόσθετος κίνδυνος:** ενδέχεται υπάρχοντες API clients (integrations) να χρησιμοποιούν μη-admin tokens· η εφαρμογή του περιορισμού **θα μπορούσε να σπάσει** ενσωματώσεις.

### Προτεινόμενη ενέργεια (προς απόφαση)
```csharp
[Authorize(Roles = AppRoles.Admin + "," + AppRoles.SuperUser)]
```
σε κάθε write action του `ApiExtendedController`. Απαιτείται επιβεβαίωση ότι δεν υπάρχει εξωτερικός/μη-admin consumer των API writes.

### Κατάσταση
ΔΕΝ εφαρμόστηκε (απουσία επιβεβαίωσης χρήσης). Απαιτεί απόφαση/αποδοχή κινδύνου πριν το Go.
