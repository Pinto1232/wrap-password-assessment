# Wrap Password

A .NET 9 solution for the Password API Assessment. The required assessment
workflow is being implemented as a console application using Clean Architecture. A
small ASP.NET Core Minimal API provides an optional SQLite-backed status endpoint.

## Current implementation

Completed:

- Generate every permitted variation of `password`.
- Validate that the dictionary contains exactly 1,296 unique candidates.
- Write the candidates atomically to `dict.txt` using UTF-8.
- Authenticate candidates sequentially with HTTP Basic authentication at a safe
  rate of nine requests per second.
- Validate that a successful response contains the expected temporary HTTPS
  upload URL.
- Build and verify an allowlisted submission ZIP with a SHA-256 manifest.
- Reject submission ZIPs that are 5,000,000 bytes or larger.
- Base64-encode and upload the validated ZIP with one JSON POST.
- Reject untrusted upload URLs and never retry an upload automatically.
- Verify the dictionary with unit, integration, and regression tests.

Still to be implemented:

- Compose preparation, authentication, confirmation, and upload in one `run`
  command.

## Solution structure

```text
WrapPassword.sln
WrapPassword.csproj                     ASP.NET Core Minimal API host
Directory.Build.props                   Shared compiler and analyzer rules
.editorconfig                           Repository formatting conventions
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
    RecruitmentApi/                     Rate-limited authentication and one-shot upload clients
    Packaging/                          PDF validation and submission ZIP builder
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
| `WrapPassword.Infrastructure` | Application | Implements file access, external HTTP, PDF validation, and ZIP packaging |
| `WrapPassword.Cli` | Application and Infrastructure | Parses commands and wires implementations to use cases |

Dependencies point toward the Domain. Business rules do not depend on file
access, HTTP clients, the console, ASP.NET Core, EF Core, or SQLite.

The root Minimal API is currently a separate support host. It exposes application
status but does not contain or duplicate the password-generation rules.

## Code quality rules

The repository enforces the following rules for every project:

- Nullable reference types and compiler warnings are checked during builds.
- Recommended .NET analyzers and `.editorconfig` conventions run as part of the
  build.
- All warnings are treated as errors.
- Use cases validate dependencies and inputs at their boundaries.
- Dictionary rules are centralized in `PasswordDictionaryValidator` instead of
  being repeated by individual use cases.
- File generation uses a temporary file and atomic replacement, so cancellation
  cannot partially overwrite an existing dictionary.

Test methods deliberately use the readable `Method_Condition_Result` naming
convention. The underscore naming analyzer is disabled only for test source files.

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
| Application unit tests | Verify candidate count, uniqueness, ordering, use-case validation, authentication stopping, exhaustion, and cancellation |
| Integration tests | Verify UTF-8 output, Basic Auth, rate limiting, ZIP safety, exact upload JSON/Base64, URL validation, and one-shot behavior |
| Regression tests | Detect unintended changes to canonical dictionary content and ordering using a SHA-256 fingerprint |

File integration tests use temporary local directories and clean them afterward.
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

## Prepare the submission ZIP

The `prepare` command performs only local file operations. It generates a fresh
`dict.txt`, validates the CV, creates the ZIP, reopens and verifies every entry,
and reports the archive size and SHA-256 hash. It never contacts the recruitment
API.

From the repository root, provide the path to your CV PDF:

```bash
dotnet run --project src/WrapPassword.Cli -- prepare "/absolute/path/to/Your-CV.pdf"
```

The default output is `artifacts/submission.zip`. An alternative output path can
be provided as the second argument:

```bash
dotnet run --project src/WrapPassword.Cli -- prepare \
  "/absolute/path/to/Your-CV.pdf" \
  "/absolute/path/to/submission.zip"
```

Expected output:

```text
Preparing the submission ZIP locally. No network requests will be made.
Archive: /absolute/path/to/submission.zip
Files: <entry-count>
Size: <size> bytes
SHA-256: <archive-hash>
```

The ZIP uses an explicit allowlist and contains:

- The CV under `CV/`.
- The freshly generated `dict.txt`.
- Domain, Application, Infrastructure, CLI, API, and automated-test source.
- Project, solution, configuration, formatting, and build-quality files.
- `README.md`, `docs/IMPLEMENTATION_PLAN.md`, and `docs/AI_ASSISTANCE.md`.
- `submission-manifest.json`, which records the path, size, and SHA-256 hash of
  every payload entry. The manifest does not list itself because a file cannot
  contain its own final hash.

The builder excludes Git metadata, `bin`, `obj`, SQLite data, test results,
coverage, previous archives, IDE files, symbolic links, and files outside the
allowlist. It rejects unsafe entry paths, invalid PDFs, and archives of
5,000,000 bytes or more. The two files under `docs/` remain ignored by Git but
must exist locally when preparing the final assessment package.

## Authentication stage

The authentication client sends sequential `GET` requests using HTTP Basic
authentication. It uses the assessment username `John` and tries each generated
password at nine requests per second, which stays below the limit of ten.

The stage handles `401 Unauthorized` as an incorrect password and continues. On
`200 OK`, it stops and accepts the returned URL only when it is an HTTPS URL on
`recruitment.warpdevelopment.co.za` under `/v2/api/upload/`. Credentials,
candidate passwords, Authorization values, and temporary URLs are not logged.

> **Live-request warning:** ZIP preparation and one-shot upload are implemented,
> but they are not composed into the final confirmed workflow yet. Do not run
> live authentication merely to test it because the temporary URL is
> intentionally not printed or saved. Use
> `dotnet test WrapPassword.sln` for safe verification until the complete
> prepare-authenticate-upload workflow is available.

The standalone authentication command is:

```bash
dotnet run --project src/WrapPassword.Cli -- authenticate
```

The assessment username defaults to `John` and can be overridden without
changing source code:

```bash
WRAP_PASSWORD_USERNAME=John \
dotnet run --project src/WrapPassword.Cli -- authenticate
```

The authentication endpoint is deliberately fixed to the HTTPS URL supplied by
the assessment, preventing candidate credentials from being redirected through
configuration. Trying all 1,296 candidates takes at least approximately 2
minutes and 24 seconds at nine requests per second, plus network response time.

## Upload stage

The upload use case accepts the validated temporary URL, prepared ZIP path, and
applicant details. Its infrastructure client reads the ZIP, confirms it is a
readable archive below 5,000,000 bytes, Base64-encodes the exact bytes, and sends
one `application/json` POST containing only these lower-case fields:

```text
data, name, surname, email
```

The client accepts only the expected HTTPS recruitment upload URL and HTTP 200
with a `Success` message. It does not retry failures, timeouts, or HTTP 429
responses because the server might already have received the submission.
Personal details, Base64 data, and temporary URLs are never logged.

The upload stage intentionally has no standalone live CLI command. It will be
called only by the final `run` workflow after local preparation and explicit
confirmation. Fake-handler integration tests verify the request contract and
one-POST behavior without contacting the recruitment API.

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
