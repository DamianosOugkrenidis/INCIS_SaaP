// Author: Damianos Pythagoras Ougkrenidis
//
namespace INCIS_SaaP.Models;

/// <summary>
/// Οντότητα που ανήκει σε συγκεκριμένο χρήστη (per-user data isolation).
/// Το OwnerId αντιστοιχεί στο ApplicationUser.Id του δημιουργού.
/// Εφαρμόζεται global query filter ώστε κάθε χρήστης να βλέπει μόνο τα δικά του
/// δεδομένα (οι Admin βλέπουν τα πάντα).
/// </summary>
public interface IOwnedEntity
{
    string? OwnerId { get; set; }
}

/// <summary>
/// Οντότητα που ανήκει σε συγκεκριμένο Tenant (εταιρεία) — multi-tenant SaaS isolation.
/// Όλοι οι χρήστες του ίδιου tenant μοιράζονται τα δεδομένα· διαφορετικοί tenants
/// είναι πλήρως απομονωμένοι. Οι Admin βλέπουν τα πάντα. TenantId == null = κοινό/legacy.
/// </summary>
public interface ITenantOwned
{
    int? TenantId { get; set; }
}
