// Author: Damianos Pythagoras Ougkrenidis
//
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace INCIS_SaaP.Controllers;

/// <summary>
/// Νομικές σελίδες (Όροι Χρήσης, Πολιτική Απορρήτου) — δημόσια προσβάσιμες.
/// </summary>
[AllowAnonymous]
public class LegalController : Controller
{
    public IActionResult Terms() => View();

    public IActionResult Privacy() => View();
}
