using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DeclutterHub.Data;
using DeclutterHub.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace DeclutterHub.Controllers
{
    [Authorize]
    public class ItemsController : Controller
    {
        private readonly DeclutterHubContext _context;

        public ItemsController(DeclutterHubContext context)
        {
            _context = context;
        }

        // GET: Items
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var declutterHubContext = _context.Item
                .Where(i => i.UserId == int.Parse(userId))
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
                        UserId = int.Parse(userId) //use the logged-in user ID
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
            ViewData["CategoryId"] = new SelectList(_context.Category.Where(c => c.IsApproved), "Id", "Id", item.CategoryId);
            ViewData["UserId"] = new SelectList(_context.User, "Id", "Id", item.UserId);
            return View(item);
        }

        // POST: Items/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,  EditItemViewModel item)
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

                    // Update item properties
                    itemToUpdate.Name = item.Name;
                    itemToUpdate.Description = item.Description;
                    itemToUpdate.Price = item.Price;
                    itemToUpdate.Location = item.Location;
                    itemToUpdate.PhoneNumber = item.PhoneNumber;
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
            // Start building the query for items
            var itemsQuery = _context.Item
                .Include(i => i.Images)
                .Include(i => i.Category)
                .Include(i => i.User)
                .AsQueryable();

            // Filter by category if provided
            if (categoryId.HasValue)
            {
                itemsQuery = itemsQuery.Where(i => i.CategoryId == categoryId.Value);
            }

            // Filter by search query if provided
            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                // Convert searchQuery to lower case for case-insensitive matching
                var lowerCaseQuery = searchQuery.ToLower();

                itemsQuery = itemsQuery.Where(i =>
                    i.Name.ToLower().Contains(lowerCaseQuery) ||       // Match item name
                    i.Description.ToLower().Contains(lowerCaseQuery) || // Match item description
                    i.Category.Name.ToLower().Contains(lowerCaseQuery));  // Match category name
            }

            // Execute the query and fetch the items
            var items = await itemsQuery.ToListAsync();

            // If no items found, return a not found result
            if (!items.Any())
            {
                return NotFound("No items matching that criteria have been found.");
            }

            // Fetch the category separately to access its name
            var category = await _context.Category.FindAsync(categoryId);
            if (category != null)
            {
                category.ClickCount += 1;
                _context.Update(category);
                await _context.SaveChangesAsync();
            }

            // If category doesn't exist, you can either return NotFound or set a default value
            ViewBag.CategoryName = category?.Name ?? "Items";
            ViewBag.CategoryName = categoryId.HasValue ?
            await _context.Category.Where(c => c.Id == categoryId.Value)
                               .Select(c => c.Name)
                               .FirstOrDefaultAsync()
                               : "All items";
            // Return the items list to the view
            return View("ItemsbyCategory",items);
        }
        [HttpPost]
        public async Task<IActionResult> SaveItem(int id)
        {
            if (ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                {
                    return BadRequest("User not authenticated");
                }

                var savedItem = new SavedItem { UserId = int.Parse(userId), ItemId = id, SavedAt = DateTime.UtcNow };
                _context.SavedItem.Add(savedItem);
                try
                {
                    await _context.SaveChangesAsync();
                    
                }
                catch (Exception ex)
                {
                    return StatusCode(500, "Internal server error");
                }
                return BadRequest(ModelState);
            }
            return Ok();
        }
        private bool ItemExists(int id)
        {
            return _context.Item.Any(e => e.Id == id);
        }
        [HttpPost]
        public async Task<IActionResult> MarkAsSold(int id)
        {
            var item = await _context.Item.FindAsync(id);
            if (item == null)
            {
                return NotFound();
            }

            item.IsSold = true;
            _context.Update(item);
            var sale = new Sale
            {
                ItemId = item.Id,
                SaleDate = DateTime.UtcNow,
                
            };
            _context.Sale.Add(sale);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
