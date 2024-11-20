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
            var now = DateTime.Now;
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
            var currentMonthSales = monthlySales.FirstOrDefault(m => m.Year == now.Year && m.Month == now.Month);
            var lastMonthSales = monthlySales.FirstOrDefault(m => m.Year == (now.Month == 1 ? now.Year - 1 : now.Year)
                                                                  && m.Month == (now.Month == 1 ? 12 : now.Month - 1));
            // Calculate daily sales data
            var dailySales = await _context.Item
                .Where(i => i.IsSold && i.CreatedAt != default)
                .GroupBy(i => new { i.CreatedAt.Date })
                .Select(g => new
                {
                    Date = g.Key.Date,
                    Count = g.Count(),
                    Value = g.Sum(i => i.Price)
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            // Calculate yearly sales data
            var yearlySales = await _context.Item
                .Where(i => i.IsSold && i.CreatedAt != default)
                .GroupBy(i => i.CreatedAt.Year)
                .Select(g => new
                {
                    Year = g.Key,
                    Count = g.Count(),
                    Value = g.Sum(i => i.Price)
                })
                .OrderBy(x => x.Year)
                .ToListAsync();

            // Format the data for Chart.js
            var formattedMonthlySales = monthlySales.Select(m => new MonthlySalesData
            {
                Month = $"{m.Year}-{m.Month:D2}",
                Count = m.Count,
                Value = m.Value
            }).ToList();

            var formattedDailySales = dailySales.Select(d => new DailySalesData
            {
                Date = d.Date.ToString("yyyy-MM-dd"),
                Count = d.Count,
                Value = d.Value
            }).ToList();

            var formattedYearlySales = yearlySales.Select(y => new YearlySalesData
            {
                Year = y.Year,
                Count = y.Count,
                Value = y.Value
            }).ToList();

            // Add data to ViewBag
            ViewBag.MonthlySales = formattedMonthlySales;
            ViewBag.DailySales = formattedDailySales;
            ViewBag.YearlySales = formattedYearlySales;

            // Calculate total listings
            var totalListings = await _context.Item.CountAsync();
           
            ViewBag.TotalListings = totalListings;
            var totalSales = soldItems.Sum(i => i.Price);
            var averageSalePrice = soldItems.Any() ? soldItems.Average(i => i.Price) : 0;

            ViewBag.TotalSalesPercentageIncrease = lastMonthSales != null && lastMonthSales.Count > 0
        ? ((double)(currentMonthSales?.Count ?? 0) - lastMonthSales.Count) / lastMonthSales.Count * 100
        : 0;

            ViewBag.TotalRevenuePercentageIncrease = lastMonthSales != null && lastMonthSales.Value > 0
        ? ((currentMonthSales?.Value ?? 0) - lastMonthSales.Value) / lastMonthSales.Value * 100
        : 0;
            var currentMonthAvgPrice = currentMonthSales != null && currentMonthSales.Count > 0
        ? currentMonthSales.Value / currentMonthSales.Count
        : 0;

            var lastMonthAvgPrice = lastMonthSales != null && lastMonthSales.Count > 0
                ? lastMonthSales.Value / lastMonthSales.Count
                : 0;

            ViewBag.AverageSalePricePercentageChange = lastMonthAvgPrice > 0
                ? (currentMonthAvgPrice - lastMonthAvgPrice) / lastMonthAvgPrice * 100
                : 0;
            
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
