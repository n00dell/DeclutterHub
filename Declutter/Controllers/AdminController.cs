using DeclutterHub.Data;
using DeclutterHub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace DeclutterHub.Controllers
{
    [Authorize(Roles ="Admin")]
    public class AdminController : Controller
    {
        
            private readonly DeclutterHubContext _context;
        public AdminController(DeclutterHubContext context)
        {
            _context = context;
        }
        [Authorize(Roles ="Admin")]
        public async Task<IActionResult> Dashboard() { 
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
            return RedirectToAction("Users");
        }

       
    }
}
