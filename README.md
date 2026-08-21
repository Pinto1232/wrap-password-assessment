# Wrap Password Assessment

An ASP.NET Core API organized around MVC boundaries with a lightweight local
SQLite database.

Requires .NET 9.

> **Database note:** The project uses SQLite through Entity Framework Core. The
> database is created automatically at `Database/wrap-password-assessment.db`
> when the backend first starts. Database files are not committed, so each clone
> gets its own local database without requiring a separate database server.

## Architecture

```text
Controllers/                 ASP.NET API controllers
Data/                        EF Core database context
Database/                    Per-clone SQLite data (generated locally)
Models/                      Backend response models
```

## Local database

The backend uses EF Core with SQLite. No database server, Docker container,
credentials, or manual setup is required. On first startup it creates:

```text
Database/wrap-password-assessment.db
```

The database schema and initial application metadata are created automatically.
Database files and SQLite journal files are ignored by Git, so every clone gets
an independent local database rather than sharing mutable data in the repository.

The connection string is configured in `appsettings.json`. To reset the local
database, stop the backend, delete `Database/wrap-password-assessment.db`, and
start the backend again.

## Development

Run the backend API:

```bash
dotnet run --launch-profile http
```

The backend API runs at `http://localhost:5080`. Opening the root URL returns
HTTP 404 by design because the application serves API routes only.

Database connectivity is included in `http://localhost:5080/api/status`.

## Production build

Build and publish the backend:

```bash
dotnet publish -c Release
```

## Verification

```bash
dotnet build
```
