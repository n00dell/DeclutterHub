using DeclutterHub.Data;

using DeclutterHub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.Blazor;
using Org.BouncyCastle.Utilities;
using System.Text.Json;

namespace DeclutterHub.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {

        private readonly DeclutterHubContext _context;
        public AdminController(DeclutterHubContext context)
        {
            _context = context;
        }
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Dashboard()
        {
            ViewBag.TotalUsers = await _context.User.CountAsync();
            ViewBag.TotalItems = await _context.Item.CountAsync();
            ViewBag.TotalSales = await _context.Sale.CountAsync();
            ViewBag.PendingApprovals = await _context.Category.CountAsync(c => !c.IsApproved);

            ViewBag.RecentUsers = await _context.User
                .OrderByDescending(u => u.CreatedAt)
                .Take(5)
                .ToListAsync();

            //example data for charts
            ViewBag.SalesDataJson = JsonSerializer.Serialize(new
            {
                label = new[] { "Jan", "Feb", "Mar", "Apr" },
                values = new[] { 10, 20, 15, 30 }
            });
            ViewBag.CategoryDataJson = JsonSerializer.Serialize(new
            {
                label = new[] { "Electronics", "Furniture", "Clothing", "Others" },
                value = new[] { 40, 20, 25, 15 }
            });

            ViewData["Layout"] = "_AdminLayout";

            return View();
        }
        public async Task<IActionResult> Users()
        {
            var users = await _context.User.ToListAsync();
            foreach (var user in users)
            {
                TempData[$"User_{user.Id}"] = $"Username: {user.Username}, Email: {user.Email}";
            }
            return View(users);
        }

        public async Task<IActionResult> EditUser(int? id)
        {
            if (id == null) return NotFound();
            var user = await _context.User.FindAsync(id);
            if (user == null) return NotFound();

            var viewModel = new EditUserViewModel
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(int id, [Bind("Id,Username,Email")] EditUserViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var user = await _context.User.FindAsync(id);
                    if (user == null)
                    {
                        return NotFound();
                    }

                    // Update only the fields we want to change
                    user.Username = viewModel.Username;
                    user.Email = viewModel.Email;
                    // Password remains unchanged

                    _context.Update(user);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Users));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(viewModel.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return View(viewModel);
        }

        private bool UserExists(int id)
        {
            return _context.User.Any(e => e.Id == id);
        }

        // POST: Admin/DeleteUser/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.User.FindAsync(id);
            if (user == null) return NotFound();

            _context.User.Remove(user);
            await _context.SaveChangesAsync();
            return RedirectToAction();
        }

        public async Task<IActionResult> Items()
        {
            var items = await _context.Item.Include(i => i.Category).Include(i => i.User).ToListAsync();
            return View(items);
        }

        public async Task<IActionResult> EditItem(int? id)
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
        public async Task<IActionResult> EditItem(int id, EditItemViewModel viewModel)
        {
            // Add debugging
            System.Diagnostics.Debug.WriteLine($"Received POST request for item ID: {id}");
            System.Diagnostics.Debug.WriteLine($"ModelState.IsValid: {ModelState.IsValid}");

            // Log all model state errors
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

                    // Add debugging
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
                    // Handle image deletions
                    if (viewModel.ImagesToDelete != null && viewModel.ImagesToDelete.Any())
                    {
                        foreach (var imageId in viewModel.ImagesToDelete)
                        {
                            var imageToDelete = existingItem.Images.FirstOrDefault(i => i.Id == imageId);
                            if (imageToDelete != null)
                            {
                                // Delete physical file
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
                    // Handle new image uploads
                    if (viewModel.NewImages != null && viewModel.NewImages.Any())
                    {
                        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/items");
                        Directory.CreateDirectory(uploadsFolder); // Ensure folder exists

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

                    // Add debugging
                    System.Diagnostics.Debug.WriteLine("Item updated successfully");

                    return RedirectToAction(nameof(Items));
                }
                catch (Exception ex)
                {
                    // Add exception logging
                    System.Diagnostics.Debug.WriteLine($"Error updating item: {ex.Message}");
                    throw;
                }
            }

            // If we got this far, something failed
            ViewBag.Categories = await _context.Category.ToListAsync();
            return View(viewModel);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyItem(int id)
        {
            var item = await _context.Item.FindAsync(id);
            if (item == null)
            {
                return NotFound();
            }

            // Verify the item
            item.IsVerified = true;
            await _context.SaveChangesAsync();

            return RedirectToAction("Items");
        }

        private bool ItemExists(int id)
        {
            return _context.Item.Any(e => e.Id == id);
        }

        public async Task<IActionResult> Categories()
        {
            var categories = await _context.Category.ToListAsync();
            return View(categories);
        }
        public async Task<IActionResult> ApproveCategories(int id)
        {
            var category = await _context.Category.FindAsync(id);
            if (category == null) return NotFound();

            category.IsApproved = true;
            await _context.SaveChangesAsync();
            return Ok();
        }
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Category.FindAsync(id);
            if (category == null) return NotFound();

            _context.Category.Remove(category);
            await _context.SaveChangesAsync();
            return RedirectToAction("Categories");
        }
        public async Task<IActionResult> CreateCategory()
        {
            return View("CreateCategory");
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
                return RedirectToAction("Categories");
            }

            return View(model);
        }
    }
}
