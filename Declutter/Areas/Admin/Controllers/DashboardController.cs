using DeclutterHub.Data;
using DeclutterHub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Text.Json;

namespace DeclutterHub.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly DeclutterHubContext _context;
        private readonly UserManager<User> _userManager;
       

        public DashboardController(DeclutterHubContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
            
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // Get user count using Identity UserManager
                ViewBag.TotalUsers = await _userManager.Users.CountAsync();

                // Other counts remain the same
                ViewBag.TotalItems = await _context.Item.CountAsync();
                ViewBag.TotalSales = await _context.Sale.CountAsync();
                ViewBag.PendingApprovals = await _context.Category.CountAsync(c => !c.IsApproved);

                // Get recent users using Identity UserManager
                ViewBag.RecentUsers = await _userManager.Users
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
                var monthlySalesData = _context.Sale
                    .GroupBy(s => s.SaleDate.Month)
                .Select(g => new MonthlySalesData
                   {
                  Month = g.Key.ToString(),
                   Count = g.Count(),
                   Value = g.Sum(s => s.Item.Price) // Adjust this according to your price calculation logic
                 }).ToList();


                ViewBag.CategoryDataJson = Newtonsoft.Json.JsonConvert.SerializeObject(new
                {
                    labels = categoryData.Select(c => c.Name).ToArray(),
                    values = categoryData.Select(c => c.ItemCount).ToArray()
                });

                // Sales data
                var salesData = new
                {
                    labels = monthlySalesData.Select(m => m.Month).ToArray(),
                    values = monthlySalesData.Select(m => m.Count).ToArray() // or use m.Value for total sales value
                };

                // Pass the sales data as JSON
                ViewBag.SalesDataJson = JsonConvert.SerializeObject(salesData);

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