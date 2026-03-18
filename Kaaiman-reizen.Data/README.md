# Structure

```
├── Dtos                   # Data Transfer Objects
├── Enums                  # Enumerations used across the application
├── Interfaces             # Interfaces for services and repositories
├── Migrations             # Database migration files
├── Models                 # Entity models representing database tables
├── Resources              # Resource files for localization (if applicable)
├── Rules                  # Business rules and validation logic
├── Services               # Service classes implementing business logic  
```

## Migrations

To add a migration from the solution root, specify the data project with `-p` and the startup project with `-s`. Example:

```
dotnet ef migrations add AddAvailabilityPeriods -p Kaaiman-reizen.Data -s Kaaiman-reizen
```

Run the command once to create the migration. After the migration is created, apply it to the database with:

```
dotnet ef database update -p Kaaiman-reizen.Data -s Kaaiman-reizen
```

If you need to remove the last added migration before applying it, use:

```
dotnet ef migrations remove -p Kaaiman-reizen.Data -s Kaaiman-reizen
```

Adjust the migration name and project names as needed for other migrations.
