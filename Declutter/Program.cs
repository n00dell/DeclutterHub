using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DeclutterHub.Data;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add the database context using PostgreSQL
builder.Services.AddDbContext<DeclutterHubContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DeclutterHubContext") ??
                      throw new InvalidOperationException("Connection string 'DeclutterHubContext' not found.")));

// Add authentication services with cookie-based authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";  // Path for login
        options.LogoutPath = "/Account/Logout";  // Path for logout
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});
// Add services for controllers with views
builder.Services.AddControllersWithViews();



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

// Use authentication and authorization
app.UseAuthentication();  // Added this to ensure authentication middleware is used
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

app.MapControllerRoute(
    name: "categoryItems",
    pattern: "Category/{categoryId}/Items",
    defaults: new { controller = "Items", action = "ItemsByCategory" });
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapControllerRoute(
    name: "itemsByCategory",
    pattern: "Items/Category/{categoryId}",
    defaults: new { controller = "Items", action = "ItemsByCategory" });
