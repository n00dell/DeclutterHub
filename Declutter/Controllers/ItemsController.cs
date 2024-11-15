using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DeclutterHub.Data;
using DeclutterHub.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using DeclutterHub.Migrations;
using DeclutterHub.Models.ViewModels;

namespace DeclutterHub.Controllers
{
    [Authorize]
    public class ItemsController : Controller
    {
        private readonly DeclutterHubContext _context;
        private readonly UserManager<User> _userManager;

        public ItemsController(DeclutterHubContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Items
        public async Task<IActionResult> Index()
        {
            var user =await _userManager.GetUserAsync(User);

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
        [Authorize]
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
                        PhoneNumber =fullPhoneNumber,
                        IsNegotiable = model.IsNegotiable,
                        Condition = model.Condition,
                        CategoryId = model.CategoryId,
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

        // GET: Items/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var item = await _context.Item.FindAsync(id);
            if (item == null)
            {
                return NotFound();
            }

            // Map Item model to EditItemViewModel
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
                IsSold = item.IsSold,
                CountryCode = item.PhoneNumber?.StartsWith("+") == true ? item.PhoneNumber.Substring(0, 4) : "",  // Extract the country code if available
                CountryCodes = new List<SelectListItem>
        {
            new SelectListItem { Value = "+1", Text = "US (+1)" },
            new SelectListItem { Value = "+44", Text = "UK (+44)" },
            new SelectListItem { Value = "+254", Text = "KE (+254)" },
            new SelectListItem { Value = "+91", Text = "IN (+91)" },
            // Add more country codes as needed
        }
            };

            // Populate category dropdown
            ViewData["CategoryId"] = new SelectList(_context.Category.Where(c => c.IsApproved), "Id", "Name", item.CategoryId);

            return View(viewModel);  // Return the EditItemViewModel to the view
        }


        // POST: Items/Edit/5
        //    [HttpPost]
        //    [ValidateAntiForgeryToken]
        //    public async Task<IActionResult> Edit(int id, EditItemViewModel item)
        //    {
        //        if (id != item.Id)
        //        {
        //            return NotFound();
        //        }

        //        if (ModelState.IsValid)
        //        {
        //            var selectedCategory = await _context.Category.FindAsync(item.CategoryId);
        //            if (selectedCategory == null || !selectedCategory.IsApproved)
        //            {
        //                ModelState.AddModelError("CategoryId", "The selected category is not approved.");
        //            }
        //            else
        //            {
        //                var itemToUpdate = await _context.Item.FindAsync(id);
        //                if (itemToUpdate == null)
        //                {
        //                    return NotFound();
        //                }
        //                string phoneNumber = item.PhoneNumber;


        //                // Check if the phone number starts with "0", remove it
        //                if (phoneNumber.StartsWith("0"))
        //                {
        //                    phoneNumber = phoneNumber.Substring(1); // Remove leading zero
        //                }
        //                string fullPhoneNumber = item.CountryCode + phoneNumber;
        //                // Update item properties
        //                var model = new EditItemViewModel
        //                {
        //                    Id = item.Id,
        //                    Name = item.Name,
        //                    Description = item.Description,
        //                    Price = item.Price,
        //                    Location = item.Location,
        //                    PhoneNumber = item.PhoneNumber,
        //                    IsNegotiable = item.IsNegotiable,
        //                    Condition = item.Condition,
        //                    IsVerified = item.IsVerified,
        //                    CategoryId = item.CategoryId,
        //                    IsSold = item.IsSold,
        //                    CountryCode = item.CountryCode,  // Pass the country code from the Item model
        //                    Images = item.Images, // Pass any existing images
        //                };

        //                // Include the country codes in the model
        //                model.CountryCodes = new List<SelectListItem>
        //{
        //    new SelectListItem { Value = "+1", Text = "US (+1)" },
        //    new SelectListItem { Value = "+44", Text = "UK (+44)" },
        //    new SelectListItem { Value = "+254", Text = "KE (+254)" },
        //    new SelectListItem { Value = "+91", Text = "IN (+91)" },
        //                        // Add more country codes as needed
        //                  };

        //                try
        //                {
        //                    await _context.SaveChangesAsync();
        //                }
        //                catch (DbUpdateConcurrencyException)
        //                {
        //                    if (!ItemExists(itemToUpdate.Id))
        //                    {
        //                        return NotFound();
        //                    }
        //                    else
        //                    {
        //                        throw;
        //                    }
        //                }
        //                return RedirectToAction(nameof(Index));
        //            }
        //        }

        //        // Populate approved categories again if model state is invalid
        //        ViewData["CategoryId"] = new SelectList(await _context.Category.Where(c => c.IsApproved).ToListAsync(), "Id", "Name", item.CategoryId);

        //        return View(item);
        //    }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPost(int id, EditItemViewModel item)
        {
            if (id != item.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var selectedCategory = await _context.Category.FindAsync(item.CategoryId);
                if (selectedCategory == null || !selectedCategory.IsApproved)
                {
                    ModelState.AddModelError("CategoryId", "The selected category is not approved.");
                }
                else
                {
                    var itemToUpdate = await _context.Item.FindAsync(id);
                    if (itemToUpdate == null)
                    {
                        return NotFound();
                    }

                    // Handle phone number
                    var phoneNumber = item.PhoneNumber.StartsWith("0")
                        ? item.PhoneNumber.Substring(1) // Remove leading zero
                        : item.PhoneNumber;

                    var fullPhoneNumber = item.CountryCode + phoneNumber;

                    // Update item properties
                    itemToUpdate.Name = item.Name;
                    itemToUpdate.Description = item.Description;
                    itemToUpdate.Price = item.Price;
                    itemToUpdate.Location = item.Location;
                    itemToUpdate.PhoneNumber = fullPhoneNumber;
                    itemToUpdate.IsNegotiable = item.IsNegotiable;
                    itemToUpdate.Condition = item.Condition;
                    itemToUpdate.CategoryId = item.CategoryId;

                    try
                    {
                        await _context.SaveChangesAsync();
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!ItemExists(itemToUpdate.Id))
                        {
                            return NotFound();
                        }
                        else
                        {
                            throw;
                        }
                    }

                    return RedirectToAction(nameof(Index));
                }
            }

            // Populate approved categories again if model state is invalid
            ViewData["CategoryId"] = new SelectList(await _context.Category.Where(c => c.IsApproved).ToListAsync(), "Id", "Name", item.CategoryId);
            return View(item);
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
                    //return BadRequest("User not authenticated");
                    return Challenge();
                }

                var existingSavedItem = await _context.SavedItem.FirstOrDefaultAsync(s=> s.UserId == user.Id && s.ItemId == id);
                if (existingSavedItem != null)
                {
                    return BadRequest("Item is already saved");
                }

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

        private bool ItemExists(int id)
        {
            return _context.Item.Any(e => e.Id == id);
        }
    }
}
