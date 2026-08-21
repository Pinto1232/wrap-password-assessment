# Wrap Password Assessment

An ASP.NET Core API with a blank Angular frontend organized around MVC
boundaries and a lightweight local SQLite database.

Requires .NET 9 and Node.js 24.15 or newer. The client includes an `.nvmrc` for
Node 24.18.

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
ClientApp/src/app/
  models/                    Domain rules and derived state
  controllers/               Angular state and event orchestration
  views/                     Presentational Angular components
  app.ts                     Blank client composition root
```

The frontend shell intentionally contains no page content or custom styling.

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

In a second terminal, run the Angular client:

```bash
cd ClientApp
nvm use
npm install
npm start
```

Open `http://localhost:4200`. Angular proxies `/api` requests to the ASP.NET API at
`http://localhost:5080`. The backend port serves API routes only; opening
`http://localhost:5080/` returns HTTP 404 by design.

Database connectivity is included in `http://localhost:5080/api/status`.

## Production build

Build and publish the frontend and backend independently:

```bash
cd ClientApp
nvm use
npm ci
npm run build
cd ..
dotnet publish -c Release
```

The Angular output is written to `ClientApp/dist`. ASP.NET does not copy or serve
that output.

## Verification

```bash
dotnet build
cd ClientApp
npm run format:check
npm test
npm run build
```
