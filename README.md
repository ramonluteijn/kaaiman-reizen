# Kaaiman Reizen

[![CI status](https://github.com/ramonluteijn/kaaiman-reizen/actions/workflows/workflow.yml/badge.svg)](https://github.com/ramonluteijn/kaaiman-reizen/actions/workflows/workflow.yml)

`Kaaiman-reizen` is a .NET solution for the Kaaiman Reizen application.
It includes the main web app, shared data layer, and tests.

## Getting started

Clone the repo and run the following commands from the **solution root** (the folder containing `Kaaiman-reizen.sln`):

```powershell
# Downloads all NuGet packages referenced across every project in the solution.
# Run this first, or after pulling changes that add/update packages.
dotnet restore

# Compiles all projects in the solution (also runs restore automatically).
# Fix any build errors before continuing.
dotnet build

# Runs all test projects discovered in the solution.
# Requires a successful build first.
dotnet test
```

> **Tip:** `dotnet build` and `dotnet test` both call restore internally, so in most cases you only need `dotnet build` followed by `dotnet test`.

## Project structure

```
Kaaiman-reizen.sln
├── Kaaiman-reizen/        # Main web application
├── Kaaiman-reizen.Data/   # Shared data layer
└── Kaaiman-reizen.Tests/  # Test project
```

## Further reading

- [Application README](Kaaiman-reizen/README.md) — setup details for the web project
- [Deployment guide](DEPLOYMENT.md) — how to deploy to production

## Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) (version specified in `global.json`)
