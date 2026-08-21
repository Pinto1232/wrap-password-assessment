# Wrap Password

A .NET 9 solution for the Password API Assessment. The required assessment
workflow is implemented as a console application using Clean Architecture. A
small ASP.NET Core Minimal API provides an optional SQLite-backed status endpoint.

## Current implementation

Completed:

- Generate every permitted variation of `password`.
- Validate that the dictionary contains exactly 1,296 unique candidates.
- Write the candidates to `dict.txt` using UTF-8.
- Verify the dictionary with unit, integration, and regression tests.

Still to be implemented:

- Authenticate against the recruitment API at no more than 10 requests per
  second.
- Create and validate the submission ZIP.
- Base64-encode and upload the ZIP to the temporary URL.

## Solution structure

```text
WrapPassword.sln
WrapPassword.csproj                     ASP.NET Core Minimal API host
Program.cs                              API composition root
Contracts/                              API request and response contracts
Data/                                   EF Core database context
Data/Entities/                          SQLite entities
Endpoints/                              Minimal API endpoints
Database/                               Generated local SQLite data
src/
  WrapPassword.Domain/                  Core password rules
  WrapPassword.Application/             Use cases, models, and abstractions
  WrapPassword.Infrastructure/          File and external-service adapters
  WrapPassword.Cli/                     Console entry point and dependency wiring
WrapPassword.Tests/
  WrapPassword.Application.UnitTests/   Dictionary and use-case unit tests
  WrapPassword.IntegrationTests/        Application and file-system integration tests
  WrapPassword.RegressionTests/         Stable dictionary content and ordering tests
```

## Clean Architecture

| Project | Depends on | Responsibility |
| --- | --- | --- |
| `WrapPassword.Domain` | Nothing | Defines the password variation rules |
| `WrapPassword.Application` | Domain | Generates and validates candidates and coordinates use cases |
| `WrapPassword.Infrastructure` | Application | Implements file access and, later, external HTTP and ZIP operations |
| `WrapPassword.Cli` | Application and Infrastructure | Parses commands and wires implementations to use cases |

Dependencies point toward the Domain. Business rules do not depend on file
access, HTTP clients, the console, ASP.NET Core, EF Core, or SQLite.

The root Minimal API is currently a separate support host. It exposes application
status but does not contain or duplicate the password-generation rules.

## Prerequisite

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

## Build the solution

From the repository root:

```bash
dotnet build WrapPassword.sln
```

## Run the automated tests

Run every test suite from the repository root:

```bash
dotnet test WrapPassword.sln
```

Run one suite at a time:

```bash
dotnet test WrapPassword.Tests/WrapPassword.Application.UnitTests/WrapPassword.Application.UnitTests.csproj
dotnet test WrapPassword.Tests/WrapPassword.IntegrationTests/WrapPassword.IntegrationTests.csproj
dotnet test WrapPassword.Tests/WrapPassword.RegressionTests/WrapPassword.RegressionTests.csproj
```

Show detailed test output:

```bash
dotnet test WrapPassword.sln --logger "console;verbosity=detailed"
```

| Test suite | Purpose |
| --- | --- |
| Application unit tests | Verify candidate count, uniqueness, allowed characters, deterministic ordering, and use-case validation |
| Integration tests | Run the real use case with the Infrastructure file writer and verify the UTF-8 file on disk |
| Regression tests | Detect unintended changes to canonical dictionary content and ordering using a SHA-256 fingerprint |

The integration tests use a temporary local directory and clean it afterward.
Automated tests never call the live recruitment API.

## Generate the password dictionary

Generate `dict.txt` in the repository root:

```bash
dotnet run --project src/WrapPassword.Cli -- generate
```

Provide an optional output path when needed:

```bash
dotnet run --project src/WrapPassword.Cli -- generate artifacts/dict.txt
```

Expected output:

```text
Generated 1 296 password candidates.
Dictionary: /absolute/path/to/dict.txt
```

The generator uses these choices independently at each position:

```text
p -> p, P
a -> a, A, @
s -> s, S, 5
s -> s, S, 5
w -> w, W
o -> o, O, 0
r -> r, R
d -> d, D
```

The total is `2 × 3 × 3 × 3 × 2 × 3 × 2 × 2 = 1,296` candidates.

## Run the optional API

```bash
dotnet run --project WrapPassword.csproj --launch-profile http
```

The API listens at `http://localhost:5080`. It intentionally serves API routes
only, so opening the root URL returns HTTP 404.

Check the API and local database connection at:

```text
http://localhost:5080/api/status
```

## Local database

The API uses Entity Framework Core with SQLite. No database server, Docker
container, credentials, or manual setup is required. On first startup it creates:

```text
Database/wrap-password-assessment.db
```

The connection string is configured in `appsettings.json`. Database and journal
files are ignored by Git, so every clone receives an independent local database.

To reset it, stop the API, delete `Database/wrap-password-assessment.db`, and
start the API again.

## Publish

Publish the console application:

```bash
dotnet publish src/WrapPassword.Cli/WrapPassword.Cli.csproj -c Release -o artifacts/cli
```

Publish the optional API separately:

```bash
dotnet publish WrapPassword.csproj -c Release -o artifacts/api
```

## Local and generated files

The following paths are intentionally ignored by Git:

- `dict.txt` and `artifacts/` — generated assessment output.
- `Database/*.db*` — per-clone SQLite data.
- `docs/` — local planning and AI-assistance records that will be added directly
  to the final submission ZIP.
