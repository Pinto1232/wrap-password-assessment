# Wrap Password Assessment

An ASP.NET Core API with an Angular frontend organized around MVC boundaries.
Password assessment runs entirely in the browser; the entered value is not sent
to the backend.

Requires .NET 9 and Node.js 24.15 or newer. The client includes an `.nvmrc` for
Node 24.18.

## Architecture

```text
Controllers/                 ASP.NET API controllers
Models/                      Backend response models
ClientApp/src/
  models/                    Domain rules and derived state
  controllers/               Angular signals and event orchestration
  views/                     Presentational Angular components
  app.ts                     Client composition root
```

On the client, user events flow from a view to a controller hook. The controller
passes input to the model and returns the resulting view state for rendering.

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
`http://localhost:5080`.

## Production build

Build Angular into ASP.NET's static web root before publishing or running the
server:

```bash
cd ClientApp
nvm use
npm ci
npm run build
cd ..
dotnet publish -c Release
```

## Verification

```bash
dotnet build
cd ClientApp
npm run format:check
npm test
npm run build
```
