using Kaaiman_reizen.Components;
using Kaaiman_reizen.Data;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Connection string 'DefaultConnection' is missing or empty.\n\n" +
        "Configure it using User Secrets (recommended for local development):\n" +
        "  cd Kaaiman-reizen\n" +
        "  dotnet user-secrets init\n" +
        "  dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"Server=localhost;Database=kaaiman_reizen;Uid=root;Pwd=;\"\n\n" +
        "Alternatively set it in Kaaiman-reizen/appsettings.Development.json (not recommended to commit because of security reasons)."
    );
}
builder.Services.AddMainContext(connectionString);
builder.Services.AddDataServices();
builder.Services.AddMudServices();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();