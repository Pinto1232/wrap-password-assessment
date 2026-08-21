# Wrap Password Assessment

An ASP.NET Core API with a blank Angular frontend organized around MVC
boundaries.

Requires .NET 9 and Node.js 24.15 or newer. The client includes an `.nvmrc` for
Node 24.18.

## Architecture

```text
Controllers/                 ASP.NET API controllers
Models/                      Backend response models
ClientApp/src/app/
  models/                    Domain rules and derived state
  controllers/               Angular state and event orchestration
  views/                     Presentational Angular components
  app.ts                     Blank client composition root
```

The frontend shell intentionally contains no page content or custom styling.

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
