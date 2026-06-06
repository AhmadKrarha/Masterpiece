using Materpiece.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Stripe;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>() 
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.AccessDeniedPath = "/Home/AccessDenied";
});


// Configure the Stripe SDK globally with your secret test key

builder.Services.AddControllersWithViews();
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    // Tells the browser to always require HTTPS for cookies
    options.Secure = CookieSecurePolicy.Always;

    // Required for some modern browser strictness, especially with Stripe elements
    options.MinimumSameSitePolicy = SameSiteMode.Lax;
});

var app = builder.Build();


// Create a scope to resolve dependencies securely
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

        // Define the roles you want to seed
        string[] roleNames = { "Admin", "User" };

        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        //  Seed Admin User
        var adminEmail = "admin@example.com";
        var adminPassword = "Admin@123"; //  Change this in production

        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new IdentityUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }

        // Clean up orphaned Pending bookings on startup to clear blocked time slots
        var dbContext = services.GetRequiredService<ApplicationDbContext>();
        var pendingBookings = dbContext.Bookings.Where(b => b.Status == BookingStatus.Pending);
        dbContext.Bookings.RemoveRange(pendingBookings);
        await dbContext.SaveChangesAsync();

        // Seed default prices and power ratings for any existing charging slots
        var slots = await dbContext.ChargerSlots.ToListAsync();
        foreach (var s in slots)
        {
            // If it's a Fast DC charger and hasn't been set yet
            if (s.Type == ChargerType.FastDC && s.PowerOutputKw == 0.0)
            {
                s.PowerOutputKw = 50.0; // 50kW Fast DC default
                s.PricePerKwh = 0.17m; // 0.17 JOD/kWh
            }
            // If it's a Normal AC charger and hasn't been set yet
            else if (s.Type == ChargerType.NormalAC && s.PowerOutputKw == 0.0)
            {
                s.PowerOutputKw = 11.0; // 11kW AC default
                s.PricePerKwh = 0.12m; // 0.12 JOD/kWh
            }
        }
        await dbContext.SaveChangesAsync();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding or cleaning up the database on startup.");
    }
}
// Error handling & security first
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else 
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts(); // Strict Transport Security
}

app.UseHttpsRedirection();   // Force HTTPS
app.UseStaticFiles();        // Serve static files (CSS, JS, images)

app.UseRouting();            // Enable endpoint routing

app.UseCookiePolicy();       // Apply cookie rules (Secure, SameSite, etc.)
app.UseAuthentication();     // Identity authentication middleware
app.UseAuthorization();      // Role/Policy-based authorization

// Map routes AFTER middleware
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();         // Identity UI endpoints


app.Run();
