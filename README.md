English | [Türkçe](README.tr.md)

# DotNet10Templates

Two .NET 10 ASP.NET Core Web API `dotnet new` templates (N-Tier and Onion Architecture) with incremental level upgrade recipes.

![.NET 10](https://img.shields.io/badge/.NET-10-512BD4)
![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)

## ✨ What's inside

- **`1-NTier-Template` (`my-ntier`)** — 3-project N-Tier Web API template starting at Level S1 (EF Core, ASP.NET Core Identity + JWT with security-stamp revocation, FluentValidation, Mapster, Serilog, Scalar UI, xUnit).
- **`2-Onion-Template` (`my-onion`)** — 4-project Onion Architecture Web API template starting at Level S3 (Domain, Application, Infrastructure, WebAPI; refresh token rotation, audit fields, soft delete, RowVersion, correlation IDs, xUnit).
- **Level recipes (`recipes/`)** — Incremental upgrade packages (`S1-S2`, `S2-S3`, `S3-S4`, `S4-S5`) plus optional reference recipes, applied and merged via `recipes/recete-uygula.ps1`.

> **Language note:** Template-internal documentation (template `README.md` files, `recipes/`, `AGENTS.md`, `docs/`) and default error/validation messages are written in Turkish.

## ⚙️ Prerequisites

- **.NET 10 SDK**
- **SQL Server or SQL Server LocalDB** — In `Development`, `IdentityDataSeeder` runs `MigrateAsync()` on startup. The default `ConnectionStrings:DefaultConnection` in `appsettings.json` is `Server=(localdb)\mssqllocaldb;Database=ProjectAppDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True`. On Linux/macOS, update `DefaultConnection` to point to a SQL Server container or remote instance.
- **PowerShell 5.1+ or PowerShell 7 (`pwsh`)** — Required for `recipes/recete-uygula.ps1` and `tests/template-smoke.ps1` (works with both `powershell -File` on Windows and `pwsh`).

## 🚀 Quick start

Clone the repository and install both templates:

```powershell
git clone <repo-url>
cd DotNet10Templates-OSS
dotnet new install ./1-NTier-Template
dotnet new install ./2-Onion-Template
```

Create a new project using either `my-ntier` or `my-onion` (pass `--skip-restore` to skip the initial restore):

```powershell
dotnet new my-ntier -n MyApp -o ./MyApp
# or
dotnet new my-onion -n MyApp -o ./MyApp
```

Build, run tests, and start the API from the generated solution root:

```powershell
cd MyApp
dotnet build MyApp.slnx
dotnet test --solution MyApp.slnx --no-build
dotnet run --project ./MyApp.API   # or ./Presentation/MyApp.WebAPI for my-onion
```

> In `Development`, `IdentityDataSeeder` runs `MigrateAsync()` and seeds the default admin account (`admin@admin.com`) using `Seed:AdminPassword` from `appsettings.Development.json`. Scalar UI is available at `/scalar/v1`.

## 🧭 Which template?

| Template | Short name | Layers | Base level | Upgrade levels | When to choose |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **`1-NTier-Template`** | `my-ntier` | 3 (`API` → `Services` → `Repositories`) | S1 (Showcase) | S2, S3 (via `recipes/`) | Showcase sites, admin panels, and small-team APIs where services use `AppDbContext` directly. |
| **`2-Onion-Template`** | `my-onion` | 4 (`WebAPI` → `Application` / `Infrastructure` → `Domain`) | S3 (Internal org) | S4, S5 (via `recipes/`) | Internal org or multi-role apps needing domain isolation, refresh tokens, audit fields, soft delete, and `RowVersion`. |

## 📶 Levels & recipes

Features are scoped by project level so templates stay minimal until higher complexity is needed:

- **S1 — Showcase (`my-ntier` base):** 1–3 admins, anonymous visitors; Identity core, `Feature-Service`, `ServiceResult<T>`, global rate limit, security-stamp token revocation.
- **S2 — Small team (`S1-S2` recipe):** 3–20 users, 1–2 roles; registration closed by default (Admin-only creation; set `Registration:AllowPublic` to `true` for open `User` self-registration), hashed refresh token rotation, logout, password reset, profile, auth rate limit.
- **S3 — Internal org (`S2-S3` recipe / `my-onion` base):** 20–300 users, 3–5 roles; audit fields, soft delete, `RowVersion` (`409 Conflict`), email change, account deletion, correlation IDs.
- **S4 — Product (`S3-S4` recipe):** 300–5000 users; domain events with in-memory bus and API versioning (`/api/v1`).
- **S5 — Scaled product (`S4-S5` recipe):** Thousands of users; caching (`ICacheService`) and architecture notes for Redis, outbox, and multi-tenancy.

Run the recipe script from the root of the **generated project**. `-Uygula` requires a clean git tree (`git init` and an initial commit first). Run a dry-run report first, then `-Uygula` (unmodified files are copied and modified files are merged via `git merge-file`), and finally complete any manual steps (`appsettings*.json`, `CLAUDE.md`) and EF Core migrations listed in that recipe's `README.md`:

```powershell
pwsh -File recipes/recete-uygula.ps1 -Recete S1-S2
pwsh -File recipes/recete-uygula.ps1 -Recete S1-S2 -Uygula
```

See [docs/SEVIYE_TANIMLARI.md](docs/SEVIYE_TANIMLARI.md) for the full level matrix (in Turkish).

## 🗂️ Repository layout

- [`1-NTier-Template/`](1-NTier-Template/README.md) — N-Tier `.NET 10` template (`my-ntier`) and `recipes/` (`S1-S2`, `S2-S3`, `opsiyonel-generic-repository-dapper`).
- [`2-Onion-Template/`](2-Onion-Template/README.md) — Onion Architecture `.NET 10` template (`my-onion`) and `recipes/` (`S3-S4`, `S4-S5`, `opsiyonel-dapper`).
- [`tests/`](tests/template-smoke.ps1) — Template smoke-test script (`template-smoke.ps1`).
- [`tools/`](tools/recete/README.md) — Recipe maintenance scripts (`gen_recipe.py`, `patch_regen.py`, `add_onceki.py`, `verify.sh`).
- [`docs/`](docs/SEVIYE_TANIMLARI.md) — Level definitions (`SEVIYE_TANIMLARI.md`).

## 🧪 Testing

Each template includes xUnit unit and integration tests (`WebApplicationFactory` + SQLite in-memory; no SQL Server required). Run `dotnet test` from the template solution root where `global.json` configures `Microsoft.Testing.Platform`:

```powershell
dotnet build ProjectApp.slnx
dotnet test --solution ProjectApp.slnx --no-build
```

To smoke-test template generation, `ProjectApp` placeholder replacement, build, and tests across both templates (requires both templates to be installed via `dotnet new install` first):

```powershell
pwsh ./tests/template-smoke.ps1
```

## 🤖 AI-assisted development

Both templates include `AGENTS.md`, `CLAUDE.md`, and `docs/AI_WORKFLOW.md` to provide architecture rules, banned-package guardrails, and workflow conventions to AI coding assistants. Using them is completely optional and does not affect building or running the templates.

---

🤝 Contributing → [CONTRIBUTING.md](CONTRIBUTING.md) · 📄 License → MIT ([LICENSE](LICENSE))
