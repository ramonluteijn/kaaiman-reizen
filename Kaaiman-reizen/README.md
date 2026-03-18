# Structure

```
├── Properties             # Project properties and settings
├── wwwroot                # Static files (if applicable)
│   ├── css                # CSS files
│   ├── js                 # JavaScript files
│   ├── images             # Image files
│   └── lib                # Third-party libraries
├── Components             # Blazor components (if applicable)
│   ├── Layouts            # Layout components
│   ├── Pages              # Page components
│   └── Shared             # Shared components
├── Views                  # MVC views (if applicable)
│   ├── Home               # Views for the Home controller (example)
├── ViewModels             # View models for MVC or Blazor (if applicable)
│   └── Home               # View models for the Home controller (example)
├── Controllers            # MVC controllers (if applicable)
├── appsettings.json       # Application configuration file
├── Program.cs             # Application entry point
```

## Local development: database connection (User Secrets)

This app expects a connection string named `DefaultConnection`.

Recommended: configure it locally via **User Secrets** so passwords don’t end up in git.

From the repo root:

```bash
cd Kaaiman-reizen
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=kaaiman_reizen;Uid=root;Pwd=;{IF YOU HAVE PASSWORD OTHERWISE KEEP IT EMPTY ;) }"
```

After that you can run migrations using:

```bash
dotnet ef database update --project Kaaiman-reizen.Data --startup-project Kaaiman-reizen --context MainContext
```