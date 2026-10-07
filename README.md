# EventForge

EventForge is an ASP.NET Core MVC starter for event-business customer relationship management. It includes a secure sign-in flow, role bootstrapping, a metrics dashboard, customer directory/search/details/edit, SQLite persistence, and container/GitHub Actions support.

## Run locally

Requirements: .NET 8 SDK.

From the repository root, set a one-time administrator login in the current shell, then run the web project:

```powershell
$env:BootstrapAdmin__Email = "admin@example.com"
$env:BootstrapAdmin__Password = "Use-a-long-unique-password!"
dotnet run --project src/EventForge.Web
```

Open the HTTPS URL printed by `dotnet run`. The SQLite database is created as `eventforge.db` in the application working directory. The first configured administrator is assigned the `Admin` role. Additional roles (`Manager`, `Sales Executive`) are created on startup; public registration is intentionally disabled. Do not reuse the example password or commit credentials.

## Run with Docker Compose

Copy `.env.example` to `.env`, set a unique `BOOTSTRAP_ADMIN_PASSWORD`, and start with `docker compose up --build`; then open <http://localhost:8080>. Compose persists SQLite data in the `eventforge-data` volume. The local `.env` file is ignored by Git.

For production, terminate TLS at a trusted reverse proxy/load balancer, use a high-entropy administrator password from your hosting provider's secret store, back up the SQLite volume, and review the application's database migration strategy before making schema changes. Startup uses `EnsureCreated` for a zero-setup first run; that does not apply future EF Core migrations to an existing database.

## GitHub Actions

Pushing to `main` or opening a pull request runs a .NET 8 Release build. Pushing to `main` or a version tag also builds the Docker image and publishes it to GitHub Container Registry (GHCR). The GHCR image is a deployable artifact; GitHub Pages does not run ASP.NET applications. To make the live app publicly available, deploy the container to a host such as Azure Container Apps, Azure App Service, or another container platform, and configure the bootstrap credentials as platform secrets.

## Project structure

- `src/EventForge.Web/` — ASP.NET Core MVC application, Identity, EF Core, customer workflows, and dashboard.
- `Dockerfile` / `docker-compose.yml` — non-root .NET container and persistent local deployment.
- `.github/workflows/` — build validation and GHCR publishing.
