﻿using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using DeclutterHub.Data;
using DeclutterHub.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using DeclutterHub.Models.ViewModels;

namespace DeclutterHub.Controllers
{
    public class CategoriesController : Controller
    {
        private readonly DeclutterHubContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png" };
        private const int MaxFileSize = 5 * 1024 * 1024; // 5MB
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ILogger<CategoriesController> _logger;
       

        public CategoriesController(DeclutterHubContext context, IWebHostEnvironment webHostEnvironment, UserManager<User> userManager, SignInManager<User> signInManager, ILogger<CategoriesController> logger)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            
        }
     
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = user?.Id;
            var userName = user?.UserName;

            // Add debug information to ViewBag
            ViewBag.CurrentUserId = userId;
            ViewBag.CurrentUserName = userName;
            ViewBag.IsAuthenticated = _signInManager.IsSignedIn(User);

            // Get all categories first for debugging
            var allCategories = await _context.Category.ToListAsync();
            ViewBag.TotalCategoriesInDb = allCategories.Count;

            // Get the user's suggestions
            var suggestions = await _context.Category
                .Where(c => c.CreatedBy == userName)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            // Add statistics
            ViewBag.TotalSuggestions = suggestions.Count;
            ViewBag.ApprovedSuggestions = suggestions.Count(s => s.IsApproved);
            ViewBag.PendingSuggestions = suggestions.Count(s => !s.IsApproved);

            // Add raw query results to ViewBag for debugging
            //ViewBag.AllCategoriesDebug = allCategories.Select(c => new
            //{
            //    c.Name,
            //    c.CreatedBy,
            //    c.IsApproved
            //}).ToList();

            return View(suggestions);
        }
    

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Category
                .Include(c => c.Items)
                .FirstOrDefaultAsync(m => m.Id == id && m.IsApproved && m.IsActive);

            if (category == null)
            {
                return NotFound();
            }

            try
            {
                category.ClickCount += 1;
                _context.Update(category);
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                // If updating click count fails, continue showing the category
                // but log the error in a production environment
            }

            return View(category);
        }

        [Authorize]
        public IActionResult Suggest()
        {
            var viewModel = new CategoryViewModel();
            if (_signInManager.IsSignedIn(User))
            {
                viewModel.CreatedBy = User.Identity.Name;
            }
            return View(viewModel);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Suggest(CategoryViewModel model)
        {
            if (!_signInManager.IsSignedIn(User))
            {
                return Challenge();
            }
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }
            model.CreatedBy = user.UserName;
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string imagePath = null;

            if (model.ImageFile != null)
            {
                // Validate file extension
                var extension = Path.GetExtension(model.ImageFile.FileName).ToLowerInvariant();
                if (!_allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("ImageFile", "Only .jpg, .jpeg and .png files are allowed.");
                    return View(model);
                }

                // Validate file size
                if (model.ImageFile.Length > MaxFileSize)
                {
                    ModelState.AddModelError("ImageFile", "File size cannot exceed 5MB.");
                    return View(model);
                }

                try
                {
                    var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "categories");
                    Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + model.ImageFile.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ImageFile.CopyToAsync(stream);
                    }

                    imagePath = $"/images/categories/{uniqueFileName}";
                }
                catch (Exception)
                {
                    ModelState.AddModelError("ImageFile", "Failed to upload image. Please try again.");
                    return View(model);
                }
            }
            var userName = User.Identity?.Name;
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Debugging
            Console.WriteLine($"UserName: {userName}, UserId: {userId}");

            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(userId))
            {
                ModelState.AddModelError("", "User is not authenticated properly.");
                return View(model);
            }

            var category = new Category
            {
                Name = model.Name,
                Description = model.Description,
                ImageUrl = imagePath,
                IsApproved = false,
                IsActive = false,
                ClickCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = user.UserName
            };

            try
            {
                _context.Add(category);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Thank you for suggesting a category! It will be reviewed by our administrators.";
                return RedirectToAction("MySuggestions","Categories");
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Failed to save the category. Please try again.");
                return View(model);
            }
        }

        [Authorize]
        public async Task<IActionResult> MySuggestions()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }
            var suggestions = await _context.Category
                .Where(c => c.CreatedBy == user.UserName)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return View(suggestions);
        }

        public async Task<IActionResult> Popular()
        {
            var popularCategories = await _context.Category
                .Where(c => c.IsApproved)
                .OrderByDescending(c => c.ClickCount)
                .Take(5)
                .ToListAsync();

            return View(popularCategories);
        }
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var user = _userManager.GetUserAsync(User);
            
            var suggestion = _context.Category.FirstOrDefault(c => c.Id == id && !c.IsApproved); // Only allow editing pending approval suggestions
            if (suggestion == null)
            {
                return NotFound(); // If the suggestion is not found or already approved, return NotFound
            }
            if(suggestion.CreatedBy!= user.Id.ToString())
            {
                return BadRequest();
            }
            return View(suggestion);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Category suggestion)
        {
            if (id != suggestion.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Update suggestion details
                    _context.Update(suggestion);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index)); // Redirect back to the list of categories or suggestions
                }
                catch (Exception ex)
                {
                    
                    ModelState.AddModelError(string.Empty, "Error updating category suggestion.");
                }
            }

            return View(suggestion);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.Category.FindAsync(id);
            if (category == null)
            {
                return Json(new { success = false, message = "Category not found" });
            }

            try
            {
                // Delete associated image if it exists
                if (!string.IsNullOrEmpty(category.ImageUrl))
                {
                    DeleteImage(category.ImageUrl);
                }

                // Remove the category from the database
                _context.Category.Remove(category);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred while deleting the category: " + ex.Message });
            }

            return RedirectToAction("MySuggestions", "Categories");
        }
        private void DeleteImage(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl)) return; // Fix: Check if it is empty
            var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, imageUrl.TrimStart('/'));
            if (System.IO.File.Exists(imagePath))
            {
                System.IO.File.Delete(imagePath);
            }
        }

    }

}

