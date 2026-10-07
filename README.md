# EventForge

EventForge is a responsive event-business CRM built with ASP.NET Core 8 MVC, ASP.NET Identity, Entity Framework Core, and SQLite. It runs in modern browsers on desktop, tablet, and mobile. The app and database run on a server/container; a GitHub repository by itself does not host an ASP.NET app, and GitHub Pages cannot run it.

## Features

- Secure sign-in, role initialization, lockout, and per-user access rules.
- Dashboard for leads, sales pipeline, events, and follow-ups.
- Customer directory with search, details, create, and edit.
- Lead, opportunity, event, vendor, and follow-up workflows.
- Transactional lead conversion that creates a customer and opportunity together.
- Completed interaction history linked to customers, leads, or events.
- Authenticated password change with current-password verification.
- Cloud health check at `/health` and persistent Identity encryption keys when `/data` is mounted.
- SQLite database, with a persistent data volume in the included container recipes.
- Docker image published to GitHub Container Registry after pushes to `main` or version tags.

## Try locally

Install the .NET 8 SDK, clone this repository, and run these commands from its root:

```powershell
$env:BootstrapAdmin__Email = "admin@example.com"
$env:BootstrapAdmin__Password = "LocalOnly-EventForge-2026!"
dotnet run --project src/EventForge.Web
```

Open the local HTTP or HTTPS URL printed by ASP.NET Core. The sample credentials above are for a new, local development database only. They are not seeded into the application and must never be reused on an internet-facing deployment. Replace them with unique credentials even on development computers shared with others.

On first startup, the app creates the SQLite database and the configured administrator. Registration is intentionally disabled. The database file is created under `src/EventForge.Web` and is excluded from Git. The three application roles are initialized automatically; additional team accounts must currently be provisioned by an administrator through ASP.NET Identity rather than a user-management screen.

## Try with Docker

Install Docker Desktop (Windows/macOS) or Docker Engine and the Compose plugin (Linux). Copy `.env.example` to `.env`, set a unique `BOOTSTRAP_ADMIN_PASSWORD`, then run:

```sh
docker compose up --build
```

Open <http://localhost:8080>. Compose keeps application data in the named `eventforge-data` volume. To stop the app, press Ctrl+C or run `docker compose down`; `docker compose down -v` also deletes the database volume.

### Run on your local network

To let another device on the same trusted Wi-Fi/LAN reach a development instance, expose the app on the host network interface (for example, set `ASPNETCORE_URLS=http://0.0.0.0:8080` in the Compose environment), allow that port through the computer's firewall, and visit `http://<host-LAN-IP>:8080` from the other device. Do not expose this development setup directly to the public internet; use a proper cloud deployment with HTTPS instead.

## Deploy from GitHub

The repository includes a Render Blueprint at `render.yaml`. To deploy a public, internet-accessible instance:

1. Sign in to [Render](https://render.com/) and choose **New → Blueprint**.
2. Choose GitHub as the provider if prompted, authorize Render to read your repositories, and select `Sasikiran-Kolluru/EventForge`.
3. Select the `main` branch, name the Blueprint/service if prompted, and continue.
4. Review the resources and environment settings from `render.yaml`, then select **Apply** or **Deploy Blueprint**. Render will build the Dockerfile, allocate a persistent disk for SQLite, create a random bootstrap admin password, and configure the `/health` probe. The Blueprint currently uses a paid Starter plan and a 1 GB persistent disk; adjust only after checking your provider's storage and pricing requirements.
5. Wait for the service status to become **Live**. Open the generated `https://…onrender.com` URL from any internet-connected device.
6. Retrieve the generated password from that service's environment/settings in Render. Sign in as `admin@eventforge.app`, then use **Change password** in the sidebar to replace the bootstrap password with a unique one. Keep passwords private; never paste them in public issues, chats, or repositories. Changing the environment variable later does not reset an already-created account's password.

The Blueprint uses a paid Starter web service because SQLite needs persistent storage; ephemeral/free container filesystems can lose the database during restarts or redeploys. Confirm current hosting prices and backup requirements with the provider before deploying. A pushed commit automatically triggers a service redeploy. Other container platforms can use the image `ghcr.io/sasikiran-kolluru/eventforge:main`, port `8080`, a persistent volume mounted at `/data`, and the environment variables in `render.yaml`.

For a different host, configure the same environment variables shown in the Blueprint, mount a durable volume at `/data`, and route public HTTPS traffic to container port `8080`. Configure the host's health probe to `GET /health`. Keep a single app instance when using SQLite; SQLite on a local mounted disk is not suitable for multiple app replicas. If you need horizontal scaling, use a shared production database and a shared/persisted ASP.NET Data Protection key store instead. The Docker image is multi-platform-capable through the .NET Linux base images; a provider can build for its native architecture from this Dockerfile.

### Testing access and security

There is deliberately **no shared public test/admin password** in this repository. A public administrator account would let anyone change or delete the data on the live app. The sample password in the local section only works when a developer explicitly configures a fresh local database. For a public demo, deploy a separate disposable instance with synthetic data and implement a restricted read-only demo role; do not share the production administrator login.

For production, use the provider's secret store, HTTPS, regular persistent-volume backups, and a managed database if you need multi-instance scaling. The initial schema is created with EF Core `EnsureCreated` for a zero-setup first run; it does not migrate an existing schema. Plan and test database migrations before updating a persistent production database.

## Dashboard metric semantics

The overview cards and charts are live summaries of the saved CRM records. Its date range filters leads, customers, opportunities, and events by their creation date, and follow-ups by their scheduled date. Upcoming event totals exclude cancelled and completed events. A won opportunity is no longer part of the open pipeline total. These are operational summaries, not accounting reports; exported financial reporting and activity-date-based period comparisons are not included yet.

## GitHub Actions

Pull requests and pushes to `main` build the .NET 8 app. Pushes to `main` and `v*` tags also build and publish a Linux container image to GitHub Container Registry (GHCR). GitHub stores the source and image; Render or another app host runs the web service.

## Project structure

- `src/EventForge.Web/` — MVC application, Identity, EF Core, views, and styles.
- `Dockerfile`, `docker-compose.yml`, `.env.example` — portable container build and local run configuration.
- `render.yaml` — persistent, HTTPS cloud-hosting blueprint.
- `.github/workflows/` — build validation and GHCR publishing.
