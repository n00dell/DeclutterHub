using DeclutterHub.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeclutterHub.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    [Area("Admin")]
    public class SalesController : Controller
    {
        private readonly DeclutterHubContext _context;

        public IActionResult Index()
        {
            return View();
        }
    }
}
