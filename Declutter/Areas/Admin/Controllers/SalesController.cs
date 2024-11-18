using DeclutterHub.Data;
using DeclutterHub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rotativa.AspNetCore;

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

        public async Task<IActionResult> Index()
        {
            // Get all sold items with related data
            var soldItems = await _context.Item
                .Include(i => i.Category)
                .Include(i => i.User)
                .Where(i => i.IsSold)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            // Calculate monthly sales data
            var monthlySales = await _context.Item
                .Where(i => i.IsSold && i.CreatedAt != default)
                .GroupBy(i => new { i.CreatedAt.Year, i.CreatedAt.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Count = g.Count(),
                    Value = g.Sum(i => i.Price)
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToListAsync();

            // Format the data after retrieving it from the database
            var formattedMonthlySales = monthlySales.Select(m => new MonthlySalesData
            {
                Month = $"{m.Year}-{m.Month:D2}",
                Count = m.Count,
                Value = m.Value
            }).ToList();

            // If no data, create an empty list of the correct type
            if (!formattedMonthlySales.Any())
            {
                formattedMonthlySales = new List<MonthlySalesData>();
            }

            // Calculate total listings for conversion rate
            var totalListings = await _context.Item.CountAsync();

            // Add data to ViewBag
            ViewBag.MonthlySales = formattedMonthlySales;
            ViewBag.TotalListings = totalListings;

            return View(soldItems);
        }
        public async Task<IActionResult> DownloadPdf()
        {
            // Get sold items from the database
            var soldItems = await _context.Item
                .Include(i => i.Category)
                .Include(i => i.User)
                .Where(i => i.IsSold)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            // Get monthly sales data from the database
            var monthlySales = await _context.Item
                .Where(i => i.IsSold && i.CreatedAt != default)
                .GroupBy(i => new { i.CreatedAt.Year, i.CreatedAt.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Count = g.Count(),
                    Value = g.Sum(i => i.Price)
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToListAsync();

            // Format monthly sales data for the chart
            var formattedMonthlySales = monthlySales.Select(m => new MonthlySalesData
            {
                Month = $"{m.Year}-{m.Month:D2}",
                Count = m.Count,
                Value = m.Value
            }).ToList();

            // Pass the formatted data to the view for rendering
            ViewBag.MonthlySales = formattedMonthlySales;

            // Generate the PDF using Rotativa
            return new ViewAsPdf("Index", soldItems)
            {
                FileName = "SalesReport.pdf",
                PageSize = Rotativa.AspNetCore.Options.Size.A4,
                PageOrientation = Rotativa.AspNetCore.Options.Orientation.Landscape,
                CustomSwitches = "--print-media-type" // Optional: Use this if your view has print media queries
            };
        }

    }
}
