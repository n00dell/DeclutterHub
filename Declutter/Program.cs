using Microsoft.EntityFrameworkCore;
using DeclutterHub.Data;
using DeclutterHub.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Razor;
using DeclutterHub.Services;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Add the database context using PostgreSQL
builder.Services.AddDbContext<DeclutterHubContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DeclutterHubContext") ??
                      throw new InvalidOperationException("Connection string 'DeclutterHubContext' not found.")));

// Add Identity services before authentication
builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;

    // User settings
    options.User.RequireUniqueEmail = true;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
})
.AddEntityFrameworkStores<DeclutterHubContext>()
.AddDefaultTokenProviders();

// Configure cookie settings
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Home/AccessDenied";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
});
builder.Services.AddScoped<CategoryService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IMailService, MailService>();

builder.Services.Configure<MailjetSettings>(builder.Configuration.GetSection("MailJet"));
// Add authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("EmailVerified", policy =>
        policy.Requirements.Add(new EmailVerifiedRequirement()));
});
builder.Services.AddScoped<IAuthorizationHandler, EmailVerifiedHandler>();

// Add services for controllers with views
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages(); // Required for Identity UI
builder.Services.AddHttpContextAccessor();

// Configure Razor view locations
builder.Services.Configure<RazorViewEngineOptions>(options =>
{
    options.AreaViewLocationFormats.Clear();
    options.AreaViewLocationFormats.Add("/Areas/{2}/Views/{1}/{0}.cshtml");
    options.AreaViewLocationFormats.Add("/Areas/{2}/Views/Shared/{0}.cshtml");
    options.AreaViewLocationFormats.Add("/Views/Shared/{0}.cshtml");
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Authentication & Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Map routes
app.MapRazorPages(); // Required for Identity pages

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "categoryItems",
    pattern: "Category/{categoryId}/Items",
    defaults: new { controller = "Items", action = "ItemsByCategory" });

app.MapControllerRoute(
    name: "itemsByCategory",
    pattern: "Items/Category/{categoryId}",
    defaults: new { controller = "Items", action = "ItemsByCategory" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Ensure database is created and migrations are applied
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<DeclutterHubContext>();
        var userManager = services.GetRequiredService<UserManager<User>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        context.Database.Migrate();

        // Seed default roles if they don't exist
        if (!roleManager.Roles.Any())
        {
            await roleManager.CreateAsync(new IdentityRole("Admin"));
            await roleManager.CreateAsync(new IdentityRole("User"));
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating or seeding the database.");
    }
}

app.Run();