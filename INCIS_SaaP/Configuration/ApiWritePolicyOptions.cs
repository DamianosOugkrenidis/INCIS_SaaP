namespace INCIS_SaaP.Configuration;

/// <summary>
/// Ρυθμίσεις πολιτικής εγγραφής για τα API write endpoints (H-2).
/// <para><b>Enforce=false</b>: Phase 0 — καταγραφή (log-only) των κλήσεων που ΘΑ αποκλείονταν
/// από την τελική πολιτική. ΔΕΝ αποτελεί προστασία πρόσβασης.</para>
/// <para><b>Enforce=true</b>: Phase 1 — πραγματική επιβολή (403 για μη εξουσιοδοτημένους).</para>
/// </summary>
public class ApiWritePolicyOptions
{
    public const string SectionName = "Api:WritePolicy";

    /// <summary>Ενεργοποίηση επιβολής. Default false (Phase 0: μόνο παρατήρηση).</summary>
    public bool Enforce { get; set; } = false;

    /// <summary>Ρόλοι με δικαίωμα εγγραφής (comma-separated), σύμφωνα με το WriteRoles.</summary>
    public string Roles { get; set; } = "Admin,SuperUser,TenantAdmin";
}
