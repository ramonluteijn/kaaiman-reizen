using Azure.Identity;
using Hangfire;
using Hangfire.MySql;
using Kaaiman_reizen.Components.Account;
using Kaaiman_reizen.Data;
using Kaaiman_reizen.Data.Identity;
using Kaaiman_reizen.Data.Services;
using Kaaiman_reizen.Extensions;
using Kaaiman_reizen.Helpers;
using Kaaiman_reizen.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using MudBlazor.Services;
using QuestPDF.Infrastructure;
using System.Globalization;
using System.Transactions;

QuestPDF.Settings.License = LicenseType.Community;

// ====================================
// CULTURE FOR DATE AND TIME FORMATTING
// ====================================
var culture = new CultureInfo("nl-NL");

CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

var builder = WebApplication.CreateBuilder(args);

// ======================
// KEY VAULT
// ======================
var keyVaultUri = builder.Configuration["KeyVault:VaultUri"];

if (!string.IsNullOrWhiteSpace(keyVaultUri))
{
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUri),
        new DefaultAzureCredential()
    );
}

// ======================
// DATABASE
// ======================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Connection string 'DefaultConnection' ontbreekt."
    );
}

builder.Services.AddMainContext(connectionString);
builder.Services.AddDataServices();
builder.Services.AddDevSeeder(builder.Environment);

// ======================
// HANGFIRE (scheduled jobs)
// ======================
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseStorage(new MySqlStorage(connectionString, new MySqlStorageOptions
    {
        TransactionIsolationLevel = IsolationLevel.ReadCommitted,
        QueuePollInterval = TimeSpan.FromMinutes(15),
        JobExpirationCheckInterval = TimeSpan.FromHours(1),
        PrepareSchemaIfNecessary = true
    })));

builder.Services.AddHangfireServer();

// ======================
// UI
// ======================
builder.Services.AddMudServices();
builder.Services.AddRazorPages();

builder.Services.AddRazorComponents().AddInteractiveServerComponents();

// ======================
// SERVICES
// ======================
builder.Services.AddScoped<IPlannerDraftService, PlannerDraftService>();
builder.Services.AddScoped<IUserTimezoneService, UserTimezoneService>();
builder.Services.AddScoped<JourneyNotificationService>();
builder.Services.AddHostedService<JourneyReminderHostedService>();

// ======================
// AUTHENTICATION
// ======================
var authBuilder = builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
});

authBuilder.AddIdentityCookies();

// ======================
// LIVE / OATH 
// ======================
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.LoginPath = "/login";
});

// ======================
// IDENTITY
// ======================
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
builder.Services.AddScoped<AccountService, AccountService>();

builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddTransient<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender, ConsoleEmailSender>();
    builder.Services.AddTransient<IEmailSender<ApplicationUser>, ConsoleEmailSender>();
}
else
{
    builder.Services.AddTransient<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender, SmtpEmailSender>();
    builder.Services.AddTransient<IEmailSender<ApplicationUser>, SmtpEmailSender>();
}

builder.Services.AddScoped<IEmailDispatcher, EmailDispatcher>();

builder.Services.AddAuthorization();

// ======================
// OAUTH
// ======================
var google = builder.Configuration.GetSection("Authentication:Google");
var microsoft = builder.Configuration.GetSection("Authentication:Microsoft");

if (!string.IsNullOrWhiteSpace(google["ClientId"]))
    authBuilder.AddGoogle(options =>
    {
        options.ClientId = google["ClientId"]!;
        options.ClientSecret = google["ClientSecret"]!;
        options.CallbackPath = "/login-google";
        options.Events.OnTicketReceived = ExternalLoginHandler.HandleExternalLogin;
        options.Events.OnRedirectToAuthorizationEndpoint = context =>
        {
            context.Response.Redirect(context.RedirectUri + "&prompt=select_account");
            return Task.CompletedTask;
        };
    });

if (!string.IsNullOrWhiteSpace(microsoft["ClientId"]))
    authBuilder.AddMicrosoftAccount(options =>
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

await app.MigrateAndSeedAsync();
// ======================
// LIVE / SERVER
// ======================
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// ======================
// PIPELINE
// ======================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

// ======================
// ENDPOINTS
// ======================
app.MapStaticAssets();

app.MapAdditionalIdentityEndpoints();

app.MapRazorComponents<Kaaiman_reizen.Components.App>()
    .AddInteractiveServerRenderMode();

app.MapRazorPages();

app.Run();