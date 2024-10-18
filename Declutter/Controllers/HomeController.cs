using Declutter.Models;
using DeclutterHub.Data;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Declutter.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly DeclutterHubContext _context;

        // Single constructor with all dependencies
        public HomeController(ILogger<HomeController> logger, DeclutterHubContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            var categories = _context.Category
                                     .OrderByDescending(c => c.ClickCount)
                                     .Take(10) // Adjust the number of categories shown
                                     .ToList();
            return View(categories);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }

}
