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
            // Other ViewBag data
            ViewBag.TotalUsers = await _context.User.CountAsync();
            ViewBag.TotalItems = await _context.Item.CountAsync();
            ViewBag.TotalSales = await _context.Sale.CountAsync();
            ViewBag.PendingApprovals = await _context.Category.CountAsync(c => !c.IsApproved);

            ViewBag.RecentUsers = await _context.User
                .OrderByDescending(u => u.CreatedAt)
                .Take(5)
                .ToListAsync();

            // Retrieve actual category data
            var categoryData = await _context.Category
                .Where(c => c.IsApproved)  // Assuming only approved categories should be displayed
                .Select(c => new
                {
                    Name = c.Name,
                    ItemCount = c.Items.Count()
                })
                .ToListAsync();

            ViewBag.CategoryDataJson = JsonSerializer.Serialize(new
            {
                labels = categoryData.Select(c => c.Name).ToArray(),
                values = categoryData.Select(c => c.ItemCount).ToArray()
            });
            var salesData = await _context.Sale
        .GroupBy(s => s.SaleDate.Month)
        .OrderBy(g => g.Key)
        .Select(g => new
        {
            Month = g.Key, // Month number (1 for Jan, 2 for Feb, etc.)
            SalesCount = g.Count(),
            
        })
        .ToListAsync();

            ViewBag.SalesDataJson = JsonSerializer.Serialize(new
            {
                labels = salesData.Select(s => System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(s.Month)).ToArray(),
                salesCount = salesData.Select(s => s.SalesCount).ToArray(),
                
            });
            ViewData["Layout"] = "_AdminLayout";
            return View("/Areas/Admin/Views/Dashboard/Index.cshtml");
        }
    }
}
