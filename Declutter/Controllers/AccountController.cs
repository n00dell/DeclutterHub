using DeclutterHub.Data;
using DeclutterHub.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Org.BouncyCastle.Crypto.Generators;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DeclutterHub.Controllers
{
    public class AccountController : Controller
    {
        private readonly DeclutterHubContext _context;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public AccountController(DeclutterHubContext context, UserManager<User> userManager, SignInManager<User> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
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

        // POST: /Account/SignUp
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SignUp(SignUpViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if the email is already taken
                if (await _userManager.FindByEmailAsync(model.Email) != null)
                {
                    ModelState.AddModelError("Email", "Email is already taken.");
                    return View(model);
                }

                // Check if the username is already taken
                if (await _userManager.FindByNameAsync(model.UserName) != null)
                {
                    ModelState.AddModelError("UserName", "Username is already taken.");
                    return View(model);
                }

                // Create a new User
                var user = new User
                {
                    UserName = model.UserName,
                    Email = model.Email,
                    EmailConfirmed = false, // Assuming email verification is pending
                    IsAdmin = false, // Assuming the user is not an admin by default
                    CreatedAt = DateTime.UtcNow, // Set the account creation date
                    Avatar = null, // Set a default value for Avatar, assuming it's nullable
                };

                // Set the password hash
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // You can also assign the user to a default role (e.g., "User")
                    await _userManager.AddToRoleAsync(user, "User");

                    // Redirect to login or home page after successful registration
                    return RedirectToAction("Login", "Account");
                }

                // If registration fails, show errors
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
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
                // Find the user by email
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    // Sign in using SignInManager
                    var result = await _signInManager.PasswordSignInAsync(
                        user,
                        model.Password,
                        model.RememberMe,  // Assuming you have this in your model
                        lockoutOnFailure: false);

                    if (result.Succeeded)
                    {
                        // Add custom claims if needed
                        if (user.IsAdmin)
                        {
                            await _userManager.AddClaimAsync(user, new Claim(ClaimTypes.Role, "Admin"));
                        }

                        if (user.EmailConfirmed)
                        {
                            await _userManager.AddClaimAsync(user, new Claim("EmailConfirmed", "true"));
                        }

                        // Redirect based on user role
                        if (user.IsAdmin)
                        {
                            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
                        }
                        else
                        {
                            return RedirectToAction("Index", "Home");
                        }
                    }
                    else if (result.RequiresTwoFactor)
                    {
                        return RedirectToAction("LoginWith2fa", new { RememberMe = model.RememberMe });
                    }
                    else if (result.IsLockedOut)
                    {
                        return RedirectToAction("Lockout");
                    }
                }

                // Invalid login attempt
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }

            // Return the view with model errors if login fails
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
