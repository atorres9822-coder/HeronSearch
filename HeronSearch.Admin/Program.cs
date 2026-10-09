using HeronSearch.Admin.Data;
using HeronSearch.Admin.Helpers;
using HeronSearch.Admin.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using MySql.EntityFrameworkCore.Extensions;

var builder = WebApplication.CreateBuilder(args);

// MVC with a global anti-forgery check on every POST (a bad or missing token returns 400).
builder.Services.AddControllersWithViews(o => o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));

// EF Core on the Oracle MySQL provider. The provider never detects the server version, so design-time tools never connect.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");
builder.Services.AddDbContext<AppDbContext>(o => o.UseMySQL(connectionString));

// The password hasher used to check sign-ins and to hash the seeded demo passwords.
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

// Cookie authentication: an HttpOnly, SameSite=Lax cookie with an 8-hour lifetime that SessionValidator renews.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/Account/Login";            // signed-out visitors land here with ?ReturnUrl=
        o.LogoutPath = "/Account/Logout";
        o.Cookie.Name = "HeronSearch.Auth";
        o.Cookie.HttpOnly = true;
        o.Cookie.SameSite = SameSiteMode.Lax;
        o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // Secure on HTTPS; the http launch profile still works locally
        o.ExpireTimeSpan = TimeSpan.FromHours(8);  // browser-session lifetime between requests
        o.SlidingExpiration = false;               // renewal is done by SessionValidator
        o.Events.OnValidatePrincipal = SessionValidator.ValidateAsync;
        o.Events.OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = 403; return Task.CompletedTask; };
    });

// Two authorization policies: BackOffice (Admin or Staff) and AdminOnly (Admin).
builder.Services.AddAuthorization(o =>
{
    o.AddPolicy(Policies.BackOffice, p => p.RequireAuthenticatedUser().RequireRole("Admin", "Staff"));
    o.AddPolicy(Policies.AdminOnly, p => p.RequireAuthenticatedUser().RequireRole("Admin"));
    // Only Verified Students get in: Login issues a cookie solely to Verified accounts, and SessionValidator
    // re-checks the account on every request, so requiring the Student role here means Student + Verified.
    o.AddPolicy(Policies.StudentOnly, p => p.RequireAuthenticatedUser().RequireRole("Student"));
});

var app = builder.Build();   // EF design-time tools stop here, so nothing below runs during migrations

// Outside Development, show the generic error page (no technical details) and send HSTS headers.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error/500");
    app.UseHsts();
}

// Re-run the request through the matching error page for status codes such as 400, 403, and 404.
app.UseStatusCodePagesWithReExecute("/Error/{0}");

// Send browsers to HTTPS.
app.UseHttpsRedirection();

// Serve runtime-uploaded photos: MapStaticAssets only knows files that existed at build time, so /uploads
// needs its own provider. The nosniff header stops browsers from guessing a different content type.
var uploadsFolder = Path.Combine(app.Environment.WebRootPath, "uploads");
Directory.CreateDirectory(uploadsFolder);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsFolder),
    RequestPath = "/uploads",
    OnPrepareResponse = c => c.Context.Response.Headers["X-Content-Type-Options"] = "nosniff"
});

// Match the request to a route.
app.UseRouting();

// Read the sign-in cookie (SessionValidator confirms the user) and then apply the controller policies.
app.UseAuthentication();
app.UseAuthorization();

// Serve the built-time static assets (css, js, images, lib).
app.MapStaticAssets();

// The default route: "/" is the public landing page (Home/Index); the admin Dashboard stays at /Dashboard.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Insert demo data on first run; never during EF design-time tooling.
if (!EF.IsDesignTime)
{
    await DbSeeder.SeedAsync(app.Services, app.Logger);
}

// Start handling requests.
app.Run();
