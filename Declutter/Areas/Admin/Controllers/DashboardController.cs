using DeclutterHub.Data;
using DeclutterHub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
            try
            {
                // Get user count using Identity UserManager
                ViewBag.TotalUsers = await _context.User.CountAsync();

                // Other counts remain the same
                ViewBag.TotalItems = await _context.Item.CountAsync();
                ViewBag.TotalSales = await _context.Sale.CountAsync();
                ViewBag.PendingApprovals = await _context.Category.CountAsync(c => !c.IsApproved);

                // Get recent users using Identity UserManager
                ViewBag.RecentUsers = await _context.User
                    .OrderByDescending(u => u.CreatedAt)
                    .Take(5)
                    .ToListAsync();

                // Category data
                var categoryData = await _context.Category
                    .Where(c => c.IsApproved)
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

                // Sales data
                var salesData = await _context.Sale
                    .GroupBy(s => s.SaleDate.Month)
                    .OrderBy(g => g.Key)
                    .Select(g => new
                    {
                        Month = g.Key,
                        SalesCount = g.Count(),
                    })
                    .ToListAsync();

                ViewBag.SalesDataJson = JsonSerializer.Serialize(new
                {
                    labels = salesData.Select(s => System.Globalization.CultureInfo.CurrentCulture
                        .DateTimeFormat.GetMonthName(s.Month)).ToArray(),
                    salesCount = salesData.Select(s => s.SalesCount).ToArray(),
                });

                ViewData["Layout"] = "_AdminLayout";
                return View("/Areas/Admin/Views/Dashboard/Index.cshtml");
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Error in Dashboard Index: {ex.Message}");
                // You might want to add proper logging here

                // Return an error view or redirect
                return RedirectToAction("Error", "Home");
            }
        }
    }
}