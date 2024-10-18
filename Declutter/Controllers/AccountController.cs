using DeclutterHub.Data;
using DeclutterHub.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Scripting;
using Org.BouncyCastle.Crypto.Generators;

namespace DeclutterHub.Controllers
{
    public class AccountController : Controller
    {
        private readonly DeclutterHubContext _context;

        public AccountController(DeclutterHubContext context)
        {
            _context = context;
        }

        // GET: /Account/SignUp
        [HttpGet]
        public IActionResult SignUp()
        {
            return View();
        }

        // POST: /Account/SignUp
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SignUp(SignUpViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Create a new User
                var user = new User
                {
                    Username = model.Username,
                    Email = model.Email,
                    Password = HashPassword(model.Password)  // Hash the password before saving
                };

                // Save the user to the database
                _context.User.Add(user);
                _context.SaveChanges();

                // Redirect to home or login after successful registration
                return RedirectToAction("Index", "Home");
            }

            // If validation fails, redisplay the sign-up form with error messages
            return View(model);
        }

        // Utility method to hash the password
        private string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password); // Example using BCrypt
        }
    }

}
