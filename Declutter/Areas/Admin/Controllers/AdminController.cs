using DeclutterHub.Data;
using DeclutterHub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.IO;

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

        public async Task<IActionResult> Dashboard()
        {
            // Fetch summary data
            ViewBag.TotalUsers = await _context.User.CountAsync();
            ViewBag.TotalItems = await _context.Item.CountAsync();
            ViewBag.TotalSales = await _context.Sale.CountAsync();
            ViewBag.PendingApprovals = await _context.Category.CountAsync(c => !c.IsApproved);

            // Fetch recent users
            ViewBag.RecentUsers = await _context.User
                .OrderByDescending(u => u.CreatedAt)
                .Take(5)
                .ToListAsync();

            // Example data for charts
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
                TempData[$"User_{user.Id}"] = $"Username: {user.UserName}, Email: {user.Email}";
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
                UserName = user.UserName,
                Email = user.Email
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(string id, [Bind("Id,Username,Email")] EditUserViewModel viewModel)
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

                    user.UserName = viewModel.UserName;
                    user.Email = viewModel.Email;

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

        private bool UserExists(string id) => _context.User.Any(e => e.Id == id);

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.User.FindAsync(id);
            if (user == null) return NotFound();

            _context.User.Remove(user);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Users));
        }

        public async Task<IActionResult> Items()
        {
            var items = await _context.Item
                .Include(i => i.Category)
                .Include(i => i.User)
                .ToListAsync();

            return View(items);
        }

        public async Task<IActionResult> EditItem(int? id)
        {
            if (id == null) return NotFound();

            var item = await _context.Item
                .Include(i => i.Images)
                .FirstOrDefaultAsync(i => i.Id == id);

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
            if (id != viewModel.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existingItem = await _context.Item
                        .Include(i => i.Images)
                        .FirstOrDefaultAsync(i => i.Id == id);

                    if (existingItem == null) return NotFound();

                    existingItem.Name = viewModel.Name;
                    existingItem.Description = viewModel.Description;
                    existingItem.Price = viewModel.Price;
                    existingItem.Location = viewModel.Location;
                    existingItem.PhoneNumber = viewModel.PhoneNumber;
                    existingItem.IsNegotiable = viewModel.IsNegotiable;
                    existingItem.Condition = viewModel.Condition;
                    existingItem.IsVerified = viewModel.IsVerified;
                    existingItem.CategoryId = viewModel.CategoryId;

                    // Handle image deletions and uploads
                    await HandleImageUploads(existingItem, viewModel);

                    _context.Update(existingItem);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Items));
                }
                catch (Exception ex)
                {
                    // Log exception
                    System.Diagnostics.Debug.WriteLine($"Error updating item: {ex.Message}");
                    throw;
                }
            }

            ViewBag.Categories = await _context.Category.ToListAsync();
            return View(viewModel);
        }

        private async Task HandleImageUploads(Item existingItem, EditItemViewModel viewModel)
        {
            // Delete images if any
            if (viewModel.ImagesToDelete != null && viewModel.ImagesToDelete.Any())
            {
                foreach (var imageId in viewModel.ImagesToDelete)
                {
                    var imageToDelete = existingItem.Images.FirstOrDefault(i => i.Id == imageId);
                    if (imageToDelete != null)
                    {
                        DeleteImageFile(imageToDelete.Url);
                        existingItem.Images.Remove(imageToDelete);
                        _context.Image.Remove(imageToDelete);
                    }
                }
            }

            // Upload new images
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
        }

        private void DeleteImageFile(string imageUrl)
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", imageUrl.TrimStart('/'));
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyItem(int id)
        {
            var item = await _context.Item.FindAsync(id);
            if (item == null) return NotFound();

            item.IsVerified = true;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Items));
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
            return RedirectToAction(nameof(Categories));
        }

        public IActionResult CreateCategory() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryViewModel model)
        {
            if (ModelState.IsValid)
            {
                string imagePath = await SaveCategoryImageAsync(model.ImageFile);
                var category = new Category
                {
                    Name = model.Name,
                    Description = model.Description,
                    ImageUrl = imagePath,
                    ClickCount = 0
                };

                _context.Add(category);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Categories));
            }

            return View(model);
        }

        private async Task<string> SaveCategoryImageAsync(IFormFile imageFile)
        {
            if (imageFile == null) return null;

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/categories");
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }

            return $"/images/categories/{uniqueFileName}";
        }
    }
}
