using DeclutterHub.Data;
using DeclutterHub.Models;
using DeclutterHub.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DeclutterHub.Controllers
{
    [Authorize]
    public class ItemsController : Controller
    {
        private readonly DeclutterHubContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<ItemsController> _logger;

        public ItemsController(DeclutterHubContext context, UserManager<User> userManager, ILogger<ItemsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: Items
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }
            var declutterHubContext = _context.Item
                .Where(i => i.UserId == user.Id)
                .Include(i => i.Images)
                .Include(i => i.Category)
                .Include(i => i.User);
            return View(await declutterHubContext.ToListAsync());
        }

        // GET: Items/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var item = await _context.Item
                .Include(i => i.Category)
                .Include(i => i.User)
                .Include(i => i.Images)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (item == null)
            {
                return NotFound();
            }
            var otherItems = await _context.Item
                .Where(i => i.UserId == item.UserId && i.Id != item.Id && !i.IsSold)
                .Include(i => i.Images)
                .ToListAsync();

            ViewBag.OtherItems = await _context.Item
       .Where(i => i.UserId == item.UserId && i.Id != id)
       .ToListAsync();

            return View(item);
        }

        // GET: Items/Create
        [Authorize(Policy = "EmailVerified")]
        public IActionResult Create()
        {
            var user = _userManager.GetUserAsync(User).Result; // Get the currently authenticated user
            if (user == null)
            {
                return Challenge(); // Ensure user is authenticated
            }

            var viewModel = new ItemViewModel
            {
                UserId = user.Id // Assign the UserId to the ViewModel
            };
            ViewData["CategoryId"] = new SelectList(_context.Category.Where(c => c.IsApproved), "Id", "Name");
            return View(viewModel);
        }

        // POST: Items/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ItemViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Challenge(); // Ensure the user is authenticated
                }

                var selectedCategory = await _context.Category.FindAsync(model.CategoryId);
                if (selectedCategory == null || !selectedCategory.IsApproved)
                {
                    ModelState.AddModelError("CategoryId", "The selected category is not approved.");
                }
                else
                {
                    var phoneNumber = model.PhoneNumber.StartsWith("0")
                        ? model.PhoneNumber.Substring(1) // Remove leading zero
                        : model.PhoneNumber;

                    var fullPhoneNumber = model.CountryCode + phoneNumber;

                    var item = new Item
                    {
                        Name = model.Name,
                        Description = model.Description,
                        Price = model.Price,
                        IsSold = false,
                        CreatedAt = DateTime.UtcNow,
                        Location = model.Location,
                        PhoneNumber = fullPhoneNumber,
                        IsNegotiable = model.IsNegotiable,
                        Condition = model.Condition,
                        CategoryId = model.CategoryId,
                        CountryCode = model.CountryCode,
                        UserId = user.Id // Ensure the logged-in user's ID is properly parsed
                    };
                    _context.Add(item);
                    await _context.SaveChangesAsync();

                    // Handle image uploads
                    if (model.ImageFiles != null && model.ImageFiles.Count > 0)
                    {
                        foreach (var ImageFile in model.ImageFiles)
                        {
                            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/items");
                            Directory.CreateDirectory(uploadsFolder); // Ensure folder exists

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
            if (id == null)
            {
                _logger.LogWarning("Edit action: id is null");
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("Edit action: User not found");
                return Unauthorized(); // Or handle differently based on your needs
            }

            var item = await _context.Item
                .Include(i => i.Images)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (item == null)
            {
                _logger.LogWarning("Edit action: Item with id {ItemId} not found", id);
                return NotFound();
            }

            // Check if the current user is the owner of the item
            if (item.UserId != user.Id)
            {
                _logger.LogWarning("Edit action: User {UserId} tried to edit an item they don't own", user.Id);
                return Forbid(); // Return 403 Forbidden if not the item owner
            }
            if (item.IsSold)
            {
                // Optionally, add a warning or message to inform the user
                TempData["ErrorMessage"] = "This item has already been sold and cannot be edited.";
                return RedirectToAction(nameof(Index)); // Redirect to the index page (or another appropriate page)
            }

            // Parse phone number to separate country code and number
            string phoneNumber = item.PhoneNumber;
            string countryCode = "";
            string localPhoneNumber = phoneNumber;

            foreach (var code in new[] { "+1", "+44", "+254", "+91" }) // Match your predefined codes
            {
                if (phoneNumber.StartsWith(code))
                {
                    countryCode = code;
                    localPhoneNumber = phoneNumber.Substring(code.Length);
                    break;
                }
            }

            var viewModel = new EditItemViewModel
            {
                Id = item.Id,
                Name = item.Name,
                Description = item.Description,
                Price = item.Price,
                Location = item.Location,
                PhoneNumber = localPhoneNumber,
                CountryCode = countryCode,
                IsNegotiable = item.IsNegotiable,
                IsSold = item.IsSold,
                Condition = item.Condition,
                CategoryId = item.CategoryId,
                Images = item.Images
            };

            // Log categories being fetched
            _logger.LogInformation("Edit action: Fetching categories for dropdown");
            ViewBag.Categories = await _context.Category.ToListAsync();

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditItemViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                _logger.LogWarning("Edit action: Item ID mismatch. Expected {ExpectedId}, but received {ReceivedId}.", id, viewModel.Id);
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                _logger.LogInformation("Edit action: Model validation failed for item ID {ItemId}.", id);
                ViewBag.Categories = await _context.Category.ToListAsync();
                return View(viewModel);
            }

            try
            {
                var existingItem = await _context.Item
                    .Include(i => i.Images)
                    .FirstOrDefaultAsync(i => i.Id == id);
                if (existingItem == null || existingItem.IsSold)
                {
                    TempData["ErrorMessage"] = "This item has already been sold and cannot be edited.";
                    return RedirectToAction(nameof(Index));
                }

                if (existingItem == null)
                {
                    _logger.LogWarning("Edit action: Item with ID {ItemId} not found.", id);
                    return NotFound();
                }

                // Ensure the current user is authorized to edit this item
                var currentUser = await _userManager.GetUserAsync(User);
                if (existingItem.UserId != currentUser.Id)
                {
                    _logger.LogWarning("Edit action: User {UserId} attempted to edit an item they don't own.", currentUser.Id);
                    return Forbid();
                }

                // Update item properties
                _logger.LogInformation("Edit action: Updating properties for item ID {ItemId}.", id);
                UpdateItemProperties(existingItem, viewModel);

                // Handle image uploads and deletions
                await HandleImageUploads(existingItem, viewModel);

                // Save changes to the database
                _context.Update(existingItem);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Edit action: Successfully updated item ID {ItemId}.", id);

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Edit action: An error occurred while updating item ID {ItemId}.", id);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
            }

            // Repopulate categories if an error occurs
            ViewBag.Categories = await _context.Category.ToListAsync();
            return View(viewModel);
        }

        private void UpdateItemProperties(Item existingItem, EditItemViewModel viewModel)
        {
            existingItem.Name = viewModel.Name;
            existingItem.Description = viewModel.Description;
            existingItem.Price = viewModel.Price;
            existingItem.Location = viewModel.Location;
            var phoneNumber = viewModel.PhoneNumber.StartsWith("0")
        ? viewModel.PhoneNumber.Substring(1)
        : viewModel.PhoneNumber;
            existingItem.PhoneNumber = viewModel.CountryCode + phoneNumber;
            existingItem.IsNegotiable = viewModel.IsNegotiable;
            existingItem.IsSold = viewModel.IsSold;
            existingItem.Condition = viewModel.Condition;
            existingItem.IsVerified = viewModel.IsVerified;
            existingItem.CategoryId = viewModel.CategoryId;
        }

        private async Task HandleImageUploads(Item existingItem, EditItemViewModel viewModel)
        {
            // Delete selected images
            if (viewModel.ImagesToDelete != null && viewModel.ImagesToDelete.Any())
            {
                foreach (var imageId in viewModel.ImagesToDelete)
                {
                    var imageToDelete = existingItem.Images.FirstOrDefault(i => i.Id == imageId);
                    if (imageToDelete != null)
                    {
                        DeleteImageFile(imageToDelete.Url); // Remove physical file
                        _context.Image.Remove(imageToDelete); // Remove from DB
                    }
                }
            }

            // Add new images
            if (viewModel.NewImages != null && viewModel.NewImages.Any())
            {
                var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/items");
                Directory.CreateDirectory(uploadFolder);

                foreach (var file in viewModel.NewImages)
                {
                    if (file.Length > 0)
                    {
                        var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                        var filePath = Path.Combine(uploadFolder, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        var newImage = new Image
                        {
                            Url = $"/images/items/{fileName}",
                            ItemId = existingItem.Id
                        };

                        existingItem.Images.Add(newImage);
                    }
                }
            }
        }


        private void DeleteImageFile(string imageUrl)
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", imageUrl.TrimStart('/'));
            _logger.LogInformation("DeleteImageFile: Attempting to delete image file at {FilePath}", filePath);

            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath); // Delete the image file from the server
                _logger.LogInformation("DeleteImageFile: Successfully deleted image file at {FilePath}", filePath);
            }
            else
            {
                _logger.LogWarning("DeleteImageFile: Image file at {FilePath} not found", filePath);
            }
        }

        private async Task<bool> ItemExistsAsync(int id)
        {
            return await _context.Item.AnyAsync(e => e.Id == id);
        }

        // GET: Items/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var item = await _context.Item
                .Include(i => i.Category)
                .Include(i => i.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (item == null)
            {
                return NotFound();
            }

            return View(item);
        }

        // POST: Items/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _context.Item.FindAsync(id);
            if (item != null)
            {
                _context.Item.Remove(item);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> ItemsByCategory(int? categoryId, string searchQuery)
        {
            var itemsQuery = _context.Item
                .Include(i => i.Images)
                .Include(i => i.Category)
                .Include(i => i.User)
                .AsQueryable();

            if (categoryId.HasValue)
            {
                itemsQuery = itemsQuery.Where(i => i.CategoryId == categoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                var lowerCaseQuery = searchQuery.ToLower();

                itemsQuery = itemsQuery.Where(i =>
                    i.Name.ToLower().Contains(lowerCaseQuery) ||
                    i.Description.ToLower().Contains(lowerCaseQuery) ||
                    i.Category.Name.ToLower().Contains(lowerCaseQuery));
            }

            var items = await itemsQuery.ToListAsync();

            if (!items.Any())
            {
                return NotFound("No items matching that criteria have been found.");
            }

            var category = await _context.Category.FindAsync(categoryId);
            if (category != null)
            {
                category.ClickCount += 1;
                _context.Update(category);
                await _context.SaveChangesAsync();
            }

            ViewBag.CategoryName = category?.Name ?? "Items";
            return View("ItemsByCategory", items);
        }

        [HttpPost]
        public async Task<IActionResult> SaveItem(int id)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Challenge(); // User not authenticated
                }

                // Find the item being saved
                var item = await _context.Item.FirstOrDefaultAsync(i => i.Id == id);
                if (item == null)
                {
                    return NotFound("Item not found");
                }

                // Check if the user is the owner of the item
                if (item.UserId == user.Id)
                {
                    return BadRequest("You cannot save your own item.");
                }

                // Check if the item is already saved by the user
                var existingSavedItem = await _context.SavedItem.FirstOrDefaultAsync(s => s.UserId == user.Id && s.ItemId == id);
                if (existingSavedItem != null)
                {
                    return BadRequest("Item is already saved.");
                }

                // Add the item to the saved list
                var savedItem = new SavedItem
                {
                    UserId = user.Id,
                    ItemId = id,
                    SavedAt = DateTime.UtcNow,
                };
                _context.SavedItem.Add(savedItem);
                await _context.SaveChangesAsync();

                return Ok("Item saved successfully");
            }

            return BadRequest("Invalid data");
        }
        [HttpPost]
        public async Task<IActionResult> MarkAsSold(int id)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Challenge(); // User not authenticated
                }

                // Find the item being marked as sold
                var item = await _context.Item.FirstOrDefaultAsync(i => i.Id == id);
                if (item == null)
                {
                    return NotFound("Item not found");
                }

                // Check if the logged-in user is the owner of the item
                if (item.UserId != user.Id)
                {
                    return Forbid("You do not have permission to mark this item as sold.");
                }

                // If the item is already marked as sold, return a message
                if (item.IsSold)
                {
                    return BadRequest("Item is already marked as sold.");
                }

                // Update the IsSold status of the item
                item.IsSold = true;
                _context.Item.Update(item);
                await _context.SaveChangesAsync();

                return Ok("Item marked as sold successfully");
            }

            return BadRequest("Invalid data");
        }

    }

}
