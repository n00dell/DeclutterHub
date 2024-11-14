using DeclutterHub.Data;
using DeclutterHub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeclutterHub.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    [Area("Admin")]
    public class UsersController : Controller
    {
        private readonly DeclutterHubContext _context;

        public UsersController(DeclutterHubContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Adjust to use IdentityUser
            var users = await _context.User.ToListAsync();  // Use 'Users' instead of 'User'
            foreach (var user in users)
            {
                TempData[$"User_{user.Id}"] = $"Username: {user.UserName}, Email: {user.Email}"; // Use 'UserName'
            }
            return View(users);
        }

        public async Task<IActionResult> Edit(string id)  // 'id' is now of type string, not int
        {
            if (id == null) return NotFound();
            var user = await _context.User.FindAsync(id); // Use 'Users' instead of 'User'
            if (user == null) return NotFound();

            var viewModel = new EditUserViewModel
            {
                Id = user.Id, // Ensure Id is being correctly mapped
                UserName = user.UserName,  // Use 'UserName' instead of 'Username'
                Email = user.Email
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, EditUserViewModel viewModel)  // 'id' is now string
        {
            if (id != viewModel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var user = await _context.User.FindAsync(id);  // Use 'Users' instead of 'User'
                    if (user == null)
                    {
                        return NotFound();
                    }

                    user.UserName = viewModel.UserName; // Update UserName, if necessary
                    user.Email = viewModel.Email;  // Update Email, if necessary

                    _context.Update(user);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)  // 'id' is now string
        {
            var user = await _context.User.FindAsync(id);  // Use 'Users' instead of 'User'
            if (user == null) return NotFound();

            _context.User.Remove(user);  // Use 'Users' instead of 'User'
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool UserExists(string id)  // 'id' is now string
        {
            return _context.User.Any(e => e.Id == id);  // Use 'Users' instead of 'User'
        }
    }
}
