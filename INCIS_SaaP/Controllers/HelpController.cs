using Microsoft.AspNetCore.Mvc;
using INCIS_SaaP.Data;
using INCIS_SaaP.Models;

namespace INCIS_SaaP.Controllers
{
    public class HelpController : Controller
    {
        private readonly INCISDbContext _context;

        public HelpController(INCISDbContext context)
        {
            _context = context;
        }

        // GET: Help
        public IActionResult Index()
        {
            return View();
        }
    }
}