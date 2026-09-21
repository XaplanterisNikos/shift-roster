# Vardiologio — Shift Roster

A small desktop application for managing monthly work shifts of a department's
staff and producing the official attendance/shift reports.

> ⚠️ **Work in progress.** The core (data entry, employee management, and the
> monthly Σ.Ω. report) works; several report fields and features are not finished
> yet — see [Status](#status).

## What it does

- **Employee management** — full CRUD with soft-delete (deactivate/restore),
  organized by specialty; contract type and work position per employee.
- **Shift-code parameters** — shift/status codes and their optional extra hours
  are editable from a dedicated settings screen (Παράμετροι Βαρδιών), with
  soft-delete instead of removal: a code is never physically deleted or reused
  once created, so historical roster entries always keep their original
  meaning. Editing the time range or Day/Night segment of a code already used
  in existing entries prompts for confirmation, since it retroactively changes
  how those entries are interpreted.
- **Daily shift entry** — pick an employee and month, assign a shift/status code
  to each day (split into two fortnights).
- **Monthly roster report ** — one row per employee, one column per day,
  with per-program counts; exported to **Excel** and **PDF** (A3 landscape) or
  previewed inline.
- **Individual analytical sheet** — per-employee monthly preview (attendance,
  shift times, supplementary hours, fortnight totals).
- **Login** — fixed users with password hashing.

## Technology

- **.NET 10**, **C#**
- **UI:** Blazor (Razor / HTML / CSS) hosted in **WPF** via **BlazorWebView (WebView2)** — a desktop app with a web UI
- **Database:** **SQLite** through **EF Core** (code-first, migrations, seeding)
- **Reports:** **ClosedXML** (Excel) and **QuestPDF** (PDF)
- **DI:** `Microsoft.Extensions.DependencyInjection`

## Architecture

Layered / Clean-ish, four projects (dependencies point inward):

```
Vardiologio.App            UI + host (WPF/BlazorWebView, Razor pages, DI setup)
Vardiologio.Application    contracts & models (interfaces, DTOs, report models)
Vardiologio.Domain         core entities & enums (no dependencies)
Vardiologio.Infrastructure implementations (EF Core, services, renderers, seeding)
```

The local database is created per-user at `%LOCALAPPDATA%\Vardiologio\vardiologio.db`;
migrations and seeding run automatically at startup.

## Getting started

Requires the .NET 10 SDK on Windows.

```bash
dotnet run --project Vardiologio.App
```

The database is created and seeded automatically on first run. Sign in with a
demo user (see [Data & seeding](#data--seeding)).

## Data & seeding

Reference data (specialties, positions, etc.) is seeded from code on first run.
Shift/status codes are seeded once with their original default values, then
become fully editable at runtime from the Παράμετροι Βαρδιών screen (add, edit,
soft-delete) — the code-level seed values are only the installation defaults,
not a source of truth that gets re-applied over user edits on upgrade.

**Employee names are personal data and are kept out of this repository:**

- `employees.json` — real employee data, **git-ignored** (only on the maintainer's machine).
- `employees.demo.json` — fake sample data, **committed**. Used automatically as a
  fallback when `employees.json` is absent, so a fresh clone builds and runs with
  demo data.

Login credentials are hard-coded in `UserStore.cs` with PBKDF2 password hashes.
The committed values are **demo passwords** (`user1/1111`, `user2/2222`) and must
be changed before any real use. Real passwords are never stored in plain text.

## Status

Working: database + seeding, login, employee CRUD, daily shift entry, monthly
Σ.Ω. report (Excel/PDF/preview), individual sheet preview (computable fields),
shift-code & extra-hours parameter management (CRUD + soft-delete).

Not finished yet:

- Report totals columns (ΗΜΕΡΗΣΙΑ / ΚΥΡΙΑΚΩΝ) — calculation rule pending.
- Individual sheet: day/night split, overtime, and Excel/PDF export.
- First-run password setup (currently fixed users).
- Backup / sync (USB / OneDrive) between the two workstations.

## License

MIT — see [LICENSE](LICENSE).
