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
- **Database export / import** (Διαχείριση → Εισαγωγή / Εξαγωγή βάσης) — open to every user.
  Export writes a dated copy of the database to `Documents\Vardiologio\Database`. Import lets
  the user pick such a copy: it is accepted only if it is a database of this application with
  the same schema; after confirmation the current data is backed up and then replaced, and the
  user is returned to the login screen. What belongs to the installation — users, administrator's
  settings, activity log — is never imported.
- **Activity log** — every database save is recorded automatically, for every table: who, when,
  which table, how many rows, and every changed field with its old and new value (a roster day
  is shown by employee, date and shift code). Report exports, database export / import and the
  zip of the files are recorded as well. The log is kept per "epoch" of the data: the original
  database, then one per database import, named after the imported file.
- **Administrator's area** — the administrator signs in to four pages of its own and none of the
  others: the activity log (one tab per epoch, filters by user and dates, a line expands to its
  field changes), the error log files as a grid, the users (delete one, delete all, set the
  maximum number of users) and a zip of everything in `Documents\Vardiologio`. The start page
  shows the version and the creator's details.
- **Login and users** — every installation has its own users, created from the login screen
  (username, password, password once more) and stored in its database with salted PBKDF2
  hashes. A single administrator account, the same in every installation, is listed after them.

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

## Activity log and error log

- **Saves** — `Infrastructure/Activity/ActivityInterceptor.cs`, an EF Core `SaveChangesInterceptor`
  attached to every context of the factory (`App.xaml.cs`). Before a save it reads the change
  tracker and adds the log rows to the same save, so data and log are written together. Password
  fields are never written. Bulk statements (`ExecuteDelete`, raw SQL) bypass `SaveChanges` and
  are not recorded, so services change tracked entities. The start-up context (migrations,
  seeding) has no interceptor.
- **Other actions** — `IActivityLog.RecordAsync`, called from the single place each kind of file
  is written: `ReportFileStore` (every report), `DatabaseTransferService`, `DocumentsArchive`.
- **Tables** — `DatabaseEpochs`, `ActivityEntries`, `ActivityChanges`. The user is stored as text,
  so a line survives the user's deletion.
- **Errors** — a failed save is written to the error log by the same interceptor; exports log
  their failures in the pages; unhandled exceptions are logged globally in `App.xaml.cs`.
  `Infrastructure/Logging/ErrorLogReader.cs` parses the daily files for the administrator's grid.

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
| `admin.json` | `admin.demo.json` (public demo password `admin-demo`; copied to the output only when `admin.json` is absent) | Salt and hash of the administrator's password — never the password itself. Created with `tools/New-AdminFile.ps1`, which asks for the password on screen. |

The texts are seeded per line from the data file (`Persistence/Seeding/ReportDataFile.cs`): a
line that already exists is never overwritten, and a line added to the file in a later version
is inserted on the next start. After that they are edited in Παράμετροι → Αναφορών. With the
demo file every line starts empty (an empty line is not printed) and is filled in there.

Users are rows of the `Users` table of the local database: username, salt and hash
(`Infrastructure/Auth/PasswordHasher.cs`, PBKDF2-SHA256). A fresh database has no users; the
first one is created from the login screen, and only the administrator can delete a user or
limit how many may exist. The administrator is not a row of the table: its
salt and hash are read from the data file above (`Infrastructure/Auth/AdminAccountFile.cs`).
Passwords are never stored in plain text. A build from this repository uses the demo
administrator password, which is public: create `admin.json` before any real use.

## Files on disk

- **Database:** `%LOCALAPPDATA%\Vardiologio\vardiologio.db` (hidden, survives reinstalls).
  - `Backups\vardiologio_yyyy-MM-dd_HHmmss.db` next to it — a copy for every export and before
    every import; the newest 20 are kept.
- **Documents\Vardiologio** (Latin names only):
  - `Logs\yyyy-MM-dd.log` — errors only (startup, unhandled, failed exports).
  - `Database\vardiologio_yyyy-MM-dd_HHmmss.db` — exported copies of the database.
  - `Reports\<Monthly-Total | Monthly-Hours | Staff-Attendance | Overtime-Certificate | Contract-Certificate | Shift-Table | Individual-Sheet>\yyyy-MM Month\` — exported reports.
- **Version:** `<Version>` in `Vardiologio.App.csproj` is the only place to change it; the login
  screen shows it and the installer reads it from the built exe.

## Status

Working: database + seeding, login with per-installation users, employee CRUD, daily shift entry, monthly
Σ.Ω. report (Excel/PDF/preview), individual sheet (preview/Excel/PDF),
shift-code & extra-hours parameter management (CRUD + soft-delete), context help and
message to the developer.

Not finished yet:

- Report totals columns (ΗΜΕΡΗΣΙΑ / ΚΥΡΙΑΚΩΝ) — calculation rule pending.
- Automatic sync between the two workstations (today: manual export / import of the database).

## License

MIT — see [LICENSE](LICENSE).
