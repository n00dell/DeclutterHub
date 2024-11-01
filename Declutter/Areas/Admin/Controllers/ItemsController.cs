using DeclutterHub.Data;
using DeclutterHub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Utilities;

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

        private bool ItemExists(int id)
        {
            return _context.Item.Any(e => e.Id == id);
        }
    }
}
