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
  to each day (split into two fortnights). The month's hours per pay category are shown
  live against their limits, and days whose code is not allowed are flagged.
- **Monthly roster report** (Συνολική μηνιαία Σ.Ω.) — one row per employee, one column per
  day, with per-program counts; emblem, letterhead, titles and addressee above the table, the
  legend of shift codes and the signatures below it. Exported to **Excel** and **PDF**
  (A4 landscape, flowing over as many pages as needed) or previewed inline.
- **Reports open ready** — the reports that need no employee (Σ.Ω., hours, shift table,
  attendance) show the current month as soon as their page opens.
- **Staff attendance report** (Πίνακας παρουσιών προσωπικού) — one mark per
  employee per day (√ / status code), printed per fortnight on A4 landscape with
  the emblem, letterhead and signatures on every page; a separate report for the
  head of the directorate (work position ΔΙΕΥΘΥΝΤΗΣ; a surname fallback can be set in the
  local data file, see [Data & seeding](#data--seeding)).
  Excel and PDF (one file per report) and inline preview.
- **Official shift table** (Πίνακας βαρδιών εργασίας) — the "Σ.Ω." form on one A3
  landscape page: codes per day (weekends / holidays shaded), counts per work code, paid
  "προς συμπλήρωση" / holiday hours, legend from the shift-code parameters, signatures.
  Excel (fit to one A3 page) and PDF (scaled to fit), inline preview.
- **Individual overtime certificate** (Βεβαίωση ατομική — «Πιστοποίηση υπερωριακής
  και λοιπής εργασίας») — one employee, one month, one A4 landscape page with the
  emblem, letterhead, declaration and signature; the paid hours per pay category
  (same numbers as the hours report). Excel, PDF and inline preview.
- **Collective overtime certificate per contract** (ΙΔΟΧ / ΙΔΑΧ / Μόνιμοι) — the same six hour
  columns as the individual certificate, one row per employee, in two forms (one tab each):
  permanent staff + open-ended contracts (ΙΔΑΧ), and fixed-term contracts (ΙΔΟΧ). Employees are
  grouped by their contract under a coloured band, the running number continues from one group
  to the next, every contract after the first starts on a new page, the table header repeats
  on every page and the signatures are printed once at the end. Paid hours from the same
  calculation as the individual certificate; a zero is left blank. The head of the directorate
  is left out; employees without one of the three contracts are listed in a note. Excel, PDF
  (A4 landscape) and inline preview; the page opens with the current month.
- **Report text parameters** (Παράμετροι → Αναφορών) — the fixed texts of six forms
  (shift table, staff attendance, directorate attendance, overtime certificate, certificate
  per contract, Σ.Ω.) are
  stored in the database and edited per form, header and footer separately: service details,
  phones, e-mail, titles, decision numbers, declaration and signatories. Lines are only
  updated (no add / delete); an empty line is not printed, and the last save of a form can be
  undone. The month and the period dates are always filled in automatically.
- **Report colours** (Παράμετροι → Αναφορών → Χρώματα) — the fills the reports use (weekend
  columns, header fills, excess hours…) are stored in the database, one row per colour, listed
  per report with the fields each one colours. Update only, with undo of the last save and a
  way back to the shipped colour; screen, Excel and PDF read the same value.
- **Individual analytical sheet** (Ατομική μηνιαία) — per-employee month, one row per day:
  contract code, shift times, overtime window (the last hours of the shift), "προς συμπλήρωση" /
  holiday hours, the shift code, attendance, and the day / night schedule columns. The summary
  shows declared overtime (from the roster codes, in place of the time-clock card of the paper
  form), "προς συμπλήρωση" per fortnight and the paid hours of the overtime certificate — each
  block says where its numbers come from. On-screen preview, Excel and PDF (one A4 landscape
  page) from one shared layout, so the three always agree.
- **Context help** — the "?" button of the top bar opens a side drawer with the instructions of
  the current page. The drawer sits next to the page, so the instructions can be read while
  working, and follows the user from page to page until it is closed.
- **Message to the developer** — the pen button of the top bar opens a form in the same drawer:
  kind (error / addition), the menu it concerns, the sender's name and the text. It opens the
  user's own mail program with recipient, subject ("kind - menu") and text filled in; nothing is
  sent by the application itself. For an error, the form shows the folder of the log files so
  that they can be attached by hand.
- **Login** — fixed users with password hashing.

## Technology

- **.NET 10**, **C#**
- **UI:** Blazor (Razor / HTML / CSS) hosted in **WPF** via **BlazorWebView (WebView2)** — a desktop app with a web UI
- **Components & theme:** **MudBlazor 9** (Material Design) with the app's own theme; **Manrope** font bundled (works offline)
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

## User interface

The UI is being moved from hand-written HTML/CSS to **MudBlazor** components, one piece at a time,
on the `ui/mudblazor` branch.

- **Theme** — `Vardiologio.App/Theme/AppTheme.cs` holds the colours, typography and layout sizes.
  `MudThemeProvider` applies it once in `Components/Routes.razor`; the app's stylesheet
  (`wwwroot/app.css`) reads the same values through MudBlazor's CSS variables.
- **Shell** — `Components/Layout/MainLayout.razor`: top bar (brand, help, message, user), a side
  menu grouped by section and a side drawer on the right.
- **Section colour** — the app icon of the top bar takes the colour of the section the current
  page belongs to: operations blue (the brand colour), reports and forms green, parameters grey,
  database management red (while its dialog is open). The mapping is `SectionClass` in
  `MainLayout.razor`; the colours are the `.brand-mark-*` rules of `app.css`.
- **Side drawer** — one persistent right drawer with two contents, so only one is open at a time:
  the help texts of the current page (`Shared/HelpContent.razor`, one block per route) and the
  message form (`Shared/FeedbackForm.razor`). The form stays rendered while hidden, so a
  half-written message is kept. Width: `DrawerWidthRight` in `AppTheme.cs`.
- **Message form** — builds a `mailto:` address, which BlazorWebView hands to Windows. A
  `mailto:` address cannot carry attachments, and some mail programs open the new message
  without the subject or the text; the "copy message" button puts recipient, subject and text
  on the clipboard as a way around it. The menu list of the form is kept by hand in step with
  the side menu.
- **About card** — the app icon (top bar and login card) opens a card with the application version and the
  creator's e-mail, LinkedIn and GitHub (`Shared/AboutButton.razor`, `Shared/AboutCard.razor`).
  Version and creator details live in `Vardiologio.App/AppInfo.cs`; the window title shows
  "Shift-Roster v<version>".
- **Employee picker** — `Shared/EmployeePicker.razor`, used wherever an employee is chosen:
  a search box (surname and / or first name, any order, case- and accent-insensitive) with the
  matching letters highlighted. The matching rules are in `Application/ShiftEntry/EmployeeSearch.cs`.
- **Legacy pages** — pages that still use plain HTML keep working inside the new shell through the
  "Legacy pages" block of `app.css`, which is deleted when the last page is migrated.

| Area | State |
|---|---|
| Theme, shell, login, administrator dialog, help drawer, message form | MudBlazor |
| Daily shift entry (Προγράμματα) | MudBlazor — hour tiles per pay category, day rows with a Day / Night / Status tag, holidays and weekends marked, sticky save bar |
| Report text parameters (Παράμετροι → Αναφορών) | MudBlazor — one tab per form, a header card and a footer card, save bar with undo of the last save |
| Employees, the other parameter pages and the report pages | plain HTML (to be migrated) |
| Dark theme switch | planned, after the pages are migrated |

## Getting started

Requires the .NET 10 SDK on Windows. NuGet packages (MudBlazor among them) are restored on the first build.

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

**Personal and organisation data are kept out of this repository.** They live in files next
to `Vardiologio.App.csproj` that are **git-ignored** (present only on the maintainer's machine
and inside the installer); each has a committed fallback, so a fresh clone builds and runs:

| Git-ignored (real) | Committed fallback | What it holds |
|---|---|---|
| `employees.json` | `employees.demo.json` (fake sample data) | Employee names and specialities. |
| `report-data.json` | `report-data.demo.json` (same blocks, every line empty) | Default texts of the official forms — service details, phones, e-mails, decisions, signatories — and, optionally, `directorLastName`: the surname that marks the head of the directorate while no employee has the work position ΔΙΕΥΘΥΝΤΗΣ. |
| `emblem.png` | none — the forms print without an emblem | The emblem above the letterhead. |

The texts are seeded per line from the data file (`Persistence/Seeding/ReportDataFile.cs`): a
line that already exists is never overwritten, and a line added to the file in a later version
is inserted on the next start. After that they are edited in Παράμετροι → Αναφορών. With the
demo file every line starts empty (an empty line is not printed) and is filled in there.

Login credentials are hard-coded in `UserStore.cs` with PBKDF2 password hashes.
The committed values are **demo passwords** (`user1/1111`, `user2/2222`) and must
be changed before any real use. Real passwords are never stored in plain text.

## Files on disk

- **Database:** `%LOCALAPPDATA%\Vardiologio\vardiologio.db` (hidden, survives reinstalls).
- **Documents\Vardiologio** (Latin names only):
  - `Logs\yyyy-MM-dd.log` — errors only (startup, unhandled, failed exports).
  - `Reports\<Monthly-Total | Monthly-Hours | Staff-Attendance | Overtime-Certificate | Contract-Certificate | Shift-Table | Individual-Sheet>\yyyy-MM Month\` — exported reports.
- **Version:** `<Version>` in `Vardiologio.App.csproj` is the only place to change it; the login
  screen shows it and the installer reads it from the built exe.

## Status

Working: database + seeding, login, employee CRUD, daily shift entry, monthly
Σ.Ω. report (Excel/PDF/preview), individual sheet (preview/Excel/PDF),
shift-code & extra-hours parameter management (CRUD + soft-delete), context help and
message to the developer.

Not finished yet:

- Report totals columns (ΗΜΕΡΗΣΙΑ / ΚΥΡΙΑΚΩΝ) — calculation rule pending.
- First-run password setup (currently fixed users).
- Backup / sync (USB / OneDrive) between the two workstations.

## License

MIT — see [LICENSE](LICENSE).
