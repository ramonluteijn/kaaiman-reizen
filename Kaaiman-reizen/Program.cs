using Kaaiman_reizen.Components;
using Kaaiman_reizen.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Kaaiman_reizen.Data.Identity;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddMainContext(connectionString);
builder.Services.AddDataServices();

// Add services to the container.
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<MainContext>()
    .AddDefaultTokenProviders()
    .AddDefaultUI();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorization();
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
});

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddRazorPages();

var app = builder.Build();

// Seed roles for Identity framework
using var scope = app.Services.CreateScope();

var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

List<string> roles = new () { "Planner", "Reisleider" };

foreach (var role in roles)
{
    if (await roleManager.RoleExistsAsync(role) is false)
        await roleManager.CreateAsync(new IdentityRole(role));
}

// Create one account for each role
Dictionary<string, string> users = new ()
{
    { "planner@kaaiman.nl", "Planner" },
    { "reisleider@kaaiman.nl", "Reisleider" }
};

foreach (var user in users)
{
    if (await userManager.FindByEmailAsync(user.Key) is null)
    {
        var account = new ApplicationUser
        {
            UserName = user.Key,
            Email = user.Key,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(account, "Kaaiman26!");
        if (result.Succeeded)
            await userManager.AddToRoleAsync(account, user.Value);
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapRazorPages();

app.Run();