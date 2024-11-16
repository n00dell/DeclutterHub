using DeclutterHub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace DeclutterHub.Services
{
    public class EmailVerifiedHandler : AuthorizationHandler<EmailVerifiedRequirement>
    {
        private readonly UserManager<User> _userManager;

        public EmailVerifiedHandler(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, EmailVerifiedRequirement requirement)
        {
            if (context.User.Identity == null || !context.User.Identity.IsAuthenticated)
            {
                return; // No user or not authenticated, so no need to check email
            }

            var user = await _userManager.GetUserAsync(context.User);

            if (user != null && user.EmailConfirmed)
            {
                context.Succeed(requirement); // Email is verified, allow access
            }
            else
            {
                context.Fail(); // Email not verified, fail authorization
            }
        }
    }
}
