using DeclutterHub.Models;
using Microsoft.AspNetCore.Mvc;

namespace DeclutterHub.Views.Shared.Components.CategoryCarousel
{
    public class CategoryCarouselViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(IEnumerable<Category> categories)
        {
            return View(categories);
        }

    }
}
