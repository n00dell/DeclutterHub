using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using DeclutterHub.Data;
using DeclutterHub.Models;

namespace DeclutterHub.Views.Shared.Components.CategoryDropdown
{
    public class CategoryDropdownViewComponent : ViewComponent
    {
        private readonly DeclutterHubContext _context;

        public CategoryDropdownViewComponent(DeclutterHubContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // Fetch categories from the database, selecting only the necessary fields
            var currentCategoryId = HttpContext.Request.Query["categoryId"].ToString();

            var categories = await _context.Category
                                       .OrderBy(c => c.Name)
                                       .Where(c => c.IsApproved)
                                       .Select(c => new
                                       {
                                           c.Id,
                                           c.Name
                                       })
                                       .ToListAsync();

            ViewBag.SelectedCategoryId = currentCategoryId;

            return View(categories);
        }


    }
}
