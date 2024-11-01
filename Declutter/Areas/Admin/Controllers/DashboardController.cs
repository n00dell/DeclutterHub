using DeclutterHub.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace DeclutterHub.Areas.Admin.Controllers

{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly DeclutterHubContext _context;

        public DashboardController(DeclutterHubContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.TotalUsers = await _context.User.CountAsync();
            ViewBag.TotalItems = await _context.Item.CountAsync();
            ViewBag.TotalSales = await _context.Sale.CountAsync();
            ViewBag.PendingApprovals = await _context.Category.CountAsync(c => !c.IsApproved);

            ViewBag.RecentUsers = await _context.User
                .OrderByDescending(u => u.CreatedAt)
                .Take(5)
                .ToListAsync();

            ViewBag.SalesDataJson = JsonSerializer.Serialize(new
            {
                label = new[] { "Jan", "Feb", "Mar", "Apr" },
                values = new[] { 10, 20, 15, 30 }
            });
            ViewBag.CategoryDataJson = JsonSerializer.Serialize(new
            {
                label = new[] { "Electronics", "Furniture", "Clothing", "Others" },
                value = new[] { 40, 20, 25, 15 }
            });

            ViewData["Layout"] = "_AdminLayout";
            return View("/Areas/Admin/Views/Dashboard/Index.cshtml");
        }
    }
}
