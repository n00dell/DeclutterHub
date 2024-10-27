using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DeclutterHub.Data;
using DeclutterHub.Models;
using Microsoft.AspNetCore.Http;

namespace DeclutterHub.Controllers
{
    public class CreateCategoryController : Controller
    {
        private readonly DeclutterHubContext _context;

        public CreateCategoryController(DeclutterHubContext context)
        {
            _context = context;
        }

        // GET: CreateCategory/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: CreateCategory/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryViewModel model)
        {
            if (ModelState.IsValid)
            {
                string imagePath = null;

                // Handle image file upload
                if (model.ImageFile != null)
                {
                    // Create a unique file name and save the image in a specific folder
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/categories");
                    Directory.CreateDirectory(uploadsFolder);  // Ensure directory exists

                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + model.ImageFile.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ImageFile.CopyToAsync(stream);
                    }

                    imagePath = $"/images/categories/{uniqueFileName}";
                }
                
                // Create the category object and save it to the database
                var category = new Category
                {
                    Name = model.Name,
                    Description = model.Description,
                    ImageUrl = imagePath,  // Save the image path to the database
                    ClickCount = 0  // Initialize ClickCount
                };

                _context.Add(category);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        // GET: CreateCategory/Index (If needed for listing categories)
        public async Task<IActionResult> Index()
        {
            return View(await _context.Category.ToListAsync());
        }
    }
}
