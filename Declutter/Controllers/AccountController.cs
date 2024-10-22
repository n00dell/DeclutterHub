using DeclutterHub.Data;
using DeclutterHub.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Org.BouncyCastle.Crypto.Generators;
using System.Security.Claims;

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
        public IActionResult Login()
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = _context.User.SingleOrDefault(u => u.Email == model.Email);
                if(user!= null && BCrypt.Net.BCrypt.Verify(model.Password, user.Password))
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new Claim(ClaimTypes.Name, user.Username),
                        new Claim(ClaimTypes.Email, user.Email)
                    };
                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));
                    return RedirectToAction("Index", "Items");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt");
                }
                
            }
            return View(model);
        }
        public async Task<IActionResult> Logout()
        {
            //sign out the user
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        // Utility method to hash the password
        private string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password); // Example using BCrypt
        }
    }

}
