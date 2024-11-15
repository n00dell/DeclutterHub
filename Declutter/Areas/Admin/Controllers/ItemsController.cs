using DeclutterHub.Data;
using DeclutterHub.Models;
using DeclutterHub.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Utilities;
using System.Security.Claims;

namespace DeclutterHub.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    [Area("Admin")]
    public class ItemsController : Controller
    {
        private readonly DeclutterHubContext _context;

        public ItemsController(DeclutterHubContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var items = await _context.Item.Include(i => i.Category).Include(i => i.User).ToListAsync();
            return View(items);
        }
        [Authorize]
        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(_context.Category.Where(c => c.IsApproved), "Id", "Name");
            ViewData["UserId"] = new SelectList(_context.User, "Id", "Username");
            return View();
        }

        // POST: Items/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ItemViewModel model)
        {
            if (ModelState.IsValid)
            {
                var selectedCategory = await _context.Category.FindAsync(model.CategoryId);
                if (selectedCategory == null || !selectedCategory.IsApproved)
                {
                    ModelState.AddModelError("CategoryId", "The selected category is not approved.");
                }
                else
                {

                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    var item = new Item
                    {
                        Name = model.Name,
                        Description = model.Description,
                        Price = model.Price,
                        IsSold = false,
                        CreatedAt = DateTime.UtcNow,
                        Location = model.Location,
                        PhoneNumber = model.PhoneNumber.ToString(),
                        IsNegotiable = model.IsNegotiable,
                        Condition = model.Condition,
                        CategoryId = model.CategoryId,
                        UserId = userId //use the logged-in user ID
                    };
                    _context.Add(item);
                    await _context.SaveChangesAsync();

                    //Handle image uploads
                    if (model.ImageFiles != null && model.ImageFiles.Count > 0)
                    {
                        foreach (var ImageFile in model.ImageFiles)
                        {
                            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/items");
                            Directory.CreateDirectory(uploadsFolder);//ensure folder exists

                            var uniqueFileName = Guid.NewGuid().ToString() + "_" + ImageFile.FileName;
                            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await ImageFile.CopyToAsync(stream);
                            }
                            var image = new Image
                            {
                                Url = $"/images/items/{uniqueFileName}",
                                ItemId = item.Id
                            };
                            _context.Add(image);
                        }
                        await _context.SaveChangesAsync();
                    }
                    return RedirectToAction(nameof(Index));
                }
            }
            ViewData["CategoryId"] = new SelectList(_context.Category.Where(c => c.IsApproved), "Id", "Name", model.CategoryId);
            return View(model);

        }
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var item = await _context.Item.Include(i => i.Images).FirstOrDefaultAsync(i => i.Id == id);
            if (item == null) return NotFound();

            var viewModel = new EditItemViewModel
            {
                Id = item.Id,
                Name = item.Name,
                Description = item.Description,
                Price = item.Price,
                Location = item.Location,
                PhoneNumber = item.PhoneNumber,
                IsNegotiable = item.IsNegotiable,
                IsSold = item.IsSold,
                Condition = item.Condition,
                IsVerified = item.IsVerified,
                CategoryId = item.CategoryId,
                Images = item.Images
            };
            ViewBag.Categories = await _context.Category.ToListAsync();
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditItemViewModel viewModel)
        {
            System.Diagnostics.Debug.WriteLine($"Received POST request for item ID: {id}");
            System.Diagnostics.Debug.WriteLine($"ModelState.IsValid: {ModelState.IsValid}");

            foreach (var modelStateEntry in ModelState.Values)
            {
                foreach (var error in modelStateEntry.Errors)
                {
                    System.Diagnostics.Debug.WriteLine($"Model Error: {error.ErrorMessage}");
                }
            }

            if (id != viewModel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingItem = await _context.Item
                        .Include(i => i.Images)
                        .FirstOrDefaultAsync(i => i.Id == id);
                    if (existingItem == null)
                    {
                        return NotFound();
                    }

                    System.Diagnostics.Debug.WriteLine("Updating item properties...");

                    existingItem.Name = viewModel.Name;
                    existingItem.Description = viewModel.Description;
                    existingItem.Price = viewModel.Price;
                    existingItem.Location = viewModel.Location;
                    existingItem.PhoneNumber = viewModel.PhoneNumber;
                    existingItem.IsNegotiable = viewModel.IsNegotiable;
                    existingItem.IsSold = viewModel.IsSold;
                    existingItem.Condition = viewModel.Condition;
                    existingItem.IsVerified = viewModel.IsVerified;
                    existingItem.CategoryId = viewModel.CategoryId;

                    if (viewModel.ImagesToDelete != null && viewModel.ImagesToDelete.Any())
                    {
                        foreach (var imageId in viewModel.ImagesToDelete)
                        {
                            var imageToDelete = existingItem.Images.FirstOrDefault(i => i.Id == imageId);
                            if (imageToDelete != null)
                            {
                                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", imageToDelete.Url.TrimStart('/'));
                                if (System.IO.File.Exists(filePath))
                                {
                                    System.IO.File.Delete(filePath);
                                }

                                existingItem.Images.Remove(imageToDelete);
                                _context.Image.Remove(imageToDelete);
                            }
                        }
                    }

                    if (viewModel.NewImages != null && viewModel.NewImages.Any())
                    {
                        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/items");
                        Directory.CreateDirectory(uploadsFolder);

                        foreach (var imageFile in viewModel.NewImages)
                        {
                            if (imageFile.Length > 0)
                            {
                                var uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                                using (var stream = new FileStream(filePath, FileMode.Create))
                                {
                                    await imageFile.CopyToAsync(stream);
                                }

                                var image = new Image
                                {
                                    Url = $"/images/items/{uniqueFileName}",
                                    ItemId = existingItem.Id
                                };

                                existingItem.Images.Add(image);
                            }
                        }
                    }

                    _context.Update(existingItem);
                    await _context.SaveChangesAsync();

                    System.Diagnostics.Debug.WriteLine("Item updated successfully");

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error updating item: {ex.Message}");
                    throw;
                }
            }

            ViewBag.Categories = await _context.Category.ToListAsync();
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Verify(int id)
        {
            var item = await _context.Item.FindAsync(id);
            if (item == null)
            {
                return NotFound();
            }

            item.IsVerified = true;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.Item.FindAsync(id);
            if (item == null)
            {
                return NotFound();
            }

            // Delete associated images from the file system
            if (item.Images != null && item.Images.Any())
            {
                foreach (var image in item.Images)
                {
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", image.Url.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                    _context.Image.Remove(image);
                }
            }

            _context.Item.Remove(item);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ItemExists(int id)
        {
            return _context.Item.Any(e => e.Id == id);
        }
    }
}
