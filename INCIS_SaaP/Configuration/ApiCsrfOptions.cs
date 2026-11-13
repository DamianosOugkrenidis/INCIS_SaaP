namespace INCIS_SaaP.Configuration;

/// <summary>
/// CSRF ρυθμίσεις για τα cookie-authenticated API write endpoints (ApiExtendedController).
/// <para><b>Enforce=false</b> (default): Phase 0 — καταγραφή (log-only) μη έγκυρων/απόντων tokens.
/// ΔΕΝ αποτελεί προστασία.</para>
/// <para><b>Enforce=true</b>: επιβολή — απαιτείται έγκυρο antiforgery token (header X-CSRF-TOKEN).</para>
/// </summary>
public class ApiCsrfOptions
{
    public const string SectionName = "Api:Csrf";

    /// <summary>Ενεργοποίηση επιβολής. Default false (Phase 0: log-only).</summary>
    public bool Enforce { get; set; } = false;

    /// <summary>Όνομα header που μεταφέρει το token (πληροφοριακό/logging· η επικύρωση γίνεται από το IAntiforgery).</summary>
    public string HeaderName { get; set; } = "X-CSRF-TOKEN";
}
