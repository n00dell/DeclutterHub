using DeclutterHub.Data;
using DeclutterHub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeclutterHub.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    [Area("Admin")]
    public class CategoriesController : Controller
    {
        private readonly DeclutterHubContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(DeclutterHubContext context, IWebHostEnvironment webHostEnvironment, ILogger<CategoriesController> logger)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment; 
            _logger = logger;
        }
        
        public async Task<IActionResult> Index()
        {
            var categories = await _context.Category.ToListAsync();
            return View(categories);
        }
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryViewModel model)
        {
            if (ModelState.IsValid)
            {
                string imagePath = null;

                if (model.ImageFile != null)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/categories");
                    Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + model.ImageFile.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ImageFile.CopyToAsync(stream);
                    }

                    imagePath = $"/images/categories/{uniqueFileName}";
                }

                var category = new Category
                {
                    Name = model.Name,
                    Description = model.Description,
                    ImageUrl = imagePath,
                    ClickCount = 0
                };

                _context.Add(category);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Approve(int id)
        {
            try
            {
                var category = await _context.Category.FindAsync(id);
                if (category == null) return NotFound();

                category.IsApproved = true;
                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving category with ID {CategoryId}",id);
                // Log the exception (use your logging framework here)
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.Category.FindAsync(id);
            if (category == null) return NotFound();
            try
            {
                if (!string.IsNullOrEmpty(category.ImageUrl))
                {
                    DeleteImage(category.ImageUrl);
                }
                _context.Category.Remove(category);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
                TempData["Success"] = "Category deleted successfully!";
            }catch (Exception ex)
            {
                TempData["Error"] = "An error occured while deleting the category";
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task<string> SaveImage (IFormFile imageFile)
        {
            if (imageFile == null) return null;
            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "categories");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }
            var uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
            using(var filestream = new FileStream(filePath,FileMode.Create))
            {
                await imageFile.CopyToAsync(filestream);
            }
            return $"/images/categories/{uniqueFileName}";

        }
        private void DeleteImage(string imageUrl)
        {
            if(!string.IsNullOrEmpty(imageUrl)) return;
            var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, imageUrl.TrimStart('/'));
            if (System.IO.File.Exists(imagePath))
            {
                System.IO.File.Delete(imagePath);
            }
        }
    }
}
