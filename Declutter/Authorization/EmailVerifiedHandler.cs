using DeclutterHub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.Threading.Tasks;

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
            return;
        }

        var user = await _userManager.GetUserAsync(context.User);

        if (user != null && user.IsEmailVerified)
        {
            context.Succeed(requirement);
        }
    }
}
