using Azure.Identity;
using Kaaiman_reizen.Components.Account;
using Kaaiman_reizen.Data;
using Kaaiman_reizen.Data.Identity;
using Kaaiman_reizen.Helpers;
using Kaaiman_reizen.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;


var builder = WebApplication.CreateBuilder(args);

var keyVaultUri = builder.Configuration["KeyVault:VaultUri"];

if (string.IsNullOrWhiteSpace(keyVaultUri) is false)
    builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUri), new DefaultAzureCredential());

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException(
        "Connection string 'DefaultConnection' is missing or empty.\n\n" +
        "Configure it using User Secrets (recommended for local development):\n" +
        "  cd Kaaiman-reizen\n" +
        "  dotnet user-secrets init\n" +
        "  dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"Server=localhost;Database=kaaiman_reizen;Uid=root;Pwd=;\"\n\n" +
        "Alternatively set it in Kaaiman-reizen/appsettings.Development.json (not recommended to commit because of security reasons)."
    );

builder.Services.AddMainContext(connectionString);
builder.Services.AddDataServices();
builder.Services.AddMudServices();
builder.Services.AddRazorPages();

// Registreer deze service slechts 1x en BUITEN de authenticatie-blokken
builder.Services.AddScoped<IPlannerDraftService, PlannerDraftService>();

// De authBuilder zorgt voor de koppeling met je cookies en externe logins (Google/Microsoft)
var authBuilder = builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
});

authBuilder.AddIdentityCookies();

// Add services to the container.
builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<MainContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

// TODO change for a real e-mailing server
builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

builder.Services.AddAuthorization();

// Add OAUTH for providers Google and Microsoft
var google = builder.Configuration.GetSection("Authentication:Google");
var microsoft = builder.Configuration.GetSection("Authentication:Microsoft");

authBuilder.AddGoogle(options =>
                {
                    options.ClientId = google["ClientId"]!;
                    options.ClientSecret = google["ClientSecret"]!;
                    options.CallbackPath = "/login-google";
                    options.Events.OnTicketReceived = ExternalLoginHandler.HandleExternalLogin;
                })
                .AddMicrosoftAccount(options =>
                {
                    options.ClientId = microsoft["ClientId"]!;
                    options.ClientSecret = microsoft["ClientSecret"]!;
                    options.CallbackPath = "/login-microsoft";
                    options.AuthorizationEndpoint = "https://login.microsoftonline.com/common/oauth2/v2.0/authorize";
                    options.TokenEndpoint = "https://login.microsoftonline.com/common/oauth2/v2.0/token";
                    options.Events.OnTicketReceived = ExternalLoginHandler.HandleExternalLogin;
                });

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Seed roles for Identity framework
using var scope = app.Services.CreateScope();

var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

List<string> roles = ["Planner", "Reisleider"];

foreach (var role in roles)
{
    if (await roleManager.RoleExistsAsync(role) is false)
        await roleManager.CreateAsync(new IdentityRole(role));
}

// Create one account for each role
Dictionary<string, string> users = new()
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
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapAdditionalIdentityEndpoints();

app.MapRazorComponents<Kaaiman_reizen.Components.App>()
    .AddInteractiveServerRenderMode();

app.MapRazorPages();

app.Run();
