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
using DeclutterHub.Models.ViewModels;
using Mailjet.Client;
using DeclutterHub.Services;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace DeclutterHub.Controllers
{
    public class AccountController : Controller
    {
        private readonly DeclutterHubContext _context;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IMailService _mailService;

        public AccountController(DeclutterHubContext context, UserManager<User> userManager, SignInManager<User> signInManager, RoleManager<IdentityRole> roleManager, IMailService mailService)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _mailService = mailService;
        }
        public IActionResult CheckYourEmail()
        {
            return View();
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
                    EmailConfirmed = false, // Email verification is pending
                    CreatedAt = DateTime.UtcNow, // Set account creation date
                    Avatar = null, // Default Avatar (nullable)
                };

                // Set the password hash
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // You can assign a default role (e.g., "User")
                    var roleExists = await _roleManager.RoleExistsAsync("User");
                    if (!roleExists)
                    {
                        await _roleManager.CreateAsync(new IdentityRole("User"));
                    }

                    // Assign the user to the "User" role
                    await _userManager.AddToRoleAsync(user, "User");

                    // Generate email confirmation token
                    var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    var confirmationLink = Url.Action(
                        "ConfirmEmail",
                        "Account",
                        new { userId = user.Id, token = token },
                        protocol: HttpContext.Request.Scheme);

                    // Send verification email using Mailjet
                    await SendVerificationEmailAsync(user.Email, confirmationLink);

                    // Redirect to the home page or a confirmation page
                    return RedirectToAction("CheckYourEmail", "Account");
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
                        if (await _userManager.IsInRoleAsync(user, "Admin"))
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
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }


        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var activeListings = await _context.Item
                .Where(i => i.UserId == user.Id && i.IsSold == false)
                .ToListAsync();

            var savedItems = await _context.SavedItem
                .Where(si => si.UserId == user.Id)
                .Include(si => si.Item)
                .Select(si => si.Item)
                .ToListAsync();

            var suggestedCategories = await _context.Category
                .Where(c => c.CreatedBy == user.Id)
                .ToListAsync();

            // Get most clicked categories (assuming you have a CategoryClicks table)
            var mostClickedCategories = await _context.CategoryClick
                .Where(cc => cc.UserId == user.Id)
                .GroupBy(cc => cc.CategoryId)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => g.First().Category)
                .ToListAsync();

            var soldItemsCount = await _context.Item
                .CountAsync(i => i.UserId == user.Id && i.IsSold == true);

            var viewModel = new ProfileViewModel
            {
                UserName = user.UserName,
                JoinDate = user.CreatedAt,
                AvatarUrl = user.Avatar,
                ActiveListings = activeListings,
                ItemsSoldCount = soldItemsCount,
                SavedItems = savedItems,
                SuggestedCategories = suggestedCategories,
                MostClickedCategories = mostClickedCategories
            };

            return View(viewModel);
        }

        private async Task SendVerificationEmailAsync(string email, string verificationLink)
        {
            var subject = "Email Confirmation";
            var body = $"Please confirm your email address by clicking on the following link: <a href='{verificationLink}'>Confirm Email</a>";

            // Use the MailService to send the email
            await _mailService.SendEmailAsync(email, subject, body);
        }

        // GET: /Account/ConfirmEmail
        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (userId == null || token == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var result = await _userManager.ConfirmEmailAsync(user, token);

            if (result.Succeeded)
            {
                // Redirect to a confirmation success page or log the user in
                return RedirectToAction("Login", "Account");
            }

            // If email confirmation fails, redirect to the home page or show an error
            return RedirectToAction("Index", "Home");
        }
        // GET: /Account/ResendVerificationEmail
        [HttpGet]
        public async Task<IActionResult> ResendVerificationEmail()
        {
            var user = await _userManager.GetUserAsync(User); // Get the currently logged-in user

            if (user == null)
            {
                return RedirectToAction("Login", "Account"); // Redirect to login page if user is not authenticated
            }

            // Check if the user's email is already confirmed
            if (user.EmailConfirmed)
            {
                return RedirectToAction("Index", "Home"); // Redirect to home page if email is already confirmed
            }

            // Generate a new email confirmation token
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            // Create the verification link
            var verificationLink = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, token = token }, protocol: Request.Scheme);

            // Send the verification email
            await SendVerificationEmailAsync(user.Email, verificationLink);

            // Show a message indicating that the email has been sent
            ViewBag.Message = "A new verification email has been sent. Please check your inbox.";

            return View();
        }



    }

}
