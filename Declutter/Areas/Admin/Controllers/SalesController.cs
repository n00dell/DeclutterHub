using DeclutterHub.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeclutterHub.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    [Area("Admin")]
    public class SalesController : Controller
    {
        private readonly DeclutterHubContext _context;
        public SalesController(DeclutterHubContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var soldItems = _context.Item
                                         .Include(i => i.Category)
                                         .Include(i => i.User)
                                         .Where(i => i.IsSold)
                                         .ToList();
            return View(soldItems);
        }
    }
}
