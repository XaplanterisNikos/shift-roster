using Microsoft.EntityFrameworkCore;
using Vardiologio.Domain.Entities;

namespace Vardiologio.Infrastructure.Persistence;

/// <summary>
/// EF Core database context: exposes the tables (DbSet) and configures the schema
/// (keys, required fields, indexes, relationships, delete behaviours, the Employee
/// soft-delete query filter, and the ShiftCode.Segment enum-to-text mapping).
/// Rule of thumb: a global IsActive query filter goes only on entities that are NOT the
/// required principal of another entity (e.g. Employee). Lookups referenced by a required
/// FK (ShiftCode) keep IsActive without a filter, see the ShiftCode configuration below.
/// </summary>
public class AppDbContext : DbContext
{
	public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

	// ── Tables (each DbSet is one table; the query entry point for that entity) ──

	/// <summary>Specialties lookup (ειδικότητα), e.g. "ΔΕ Οδηγών".</summary>
	public DbSet<Speciality> Specialities => Set<Speciality>();

	/// <summary>Shift and status codes (1–13 + Ρ/Α/Κ…), with times and Day/Night segment.</summary>
	public DbSet<ShiftCode> ShiftCodes => Set<ShiftCode>();

	/// <summary>Staff members who appear on the roster.</summary>
	public DbSet<Employee> Employees => Set<Employee>();

	/// <summary>One row per employee per day: which shift/status code that day.</summary>
	public DbSet<ShiftDay> ShiftDays => Set<ShiftDay>();

	/// <summary>Employment-type lookup (ΕΙΔΟΣ ΕΡΓΑΣΙΑΣ), e.g. Μ / Α.Χ / Ο.Χ.</summary>
	public DbSet<EmploymentType> EmploymentTypes => Set<EmploymentType>();

	/// <summary>Work-position lookup (ΘΕΣΗ ΕΡΓΑΣΙΑΣ), e.g. Οδηγός / Πύλη / Γραφεία.</summary>
	public DbSet<WorkPosition> WorkPositions => Set<WorkPosition>();

	/// <summary>Supervisors who sign the individual monthly sheet (Παράμετροι Εργαζομένων → Υπογράφοντες).</summary>
	public DbSet<Signatory> Signatories => Set<Signatory>();

	/// <summary>Hours per pay category that each shift code yields on each kind of day.</summary>
	public DbSet<ShiftCodeHours> ShiftCodeHours => Set<ShiftCodeHours>();

	/// <summary>Public holidays (αργίες), maintained per year by the user.</summary>
	public DbSet<Holiday> Holidays => Set<Holiday>();

	/// <summary>Monthly paid-hours limits (single-row settings table).</summary>
	public DbSet<HourLimits> HourLimits => Set<HourLimits>();

	/// <summary>Editable fixed texts of the official forms (letterhead, titles, signatures…).</summary>
	public DbSet<ReportText> ReportTexts => Set<ReportText>();

	/// <summary>Editable colours of the reports (weekend shading, header fills…).</summary>
	public DbSet<ReportColor> ReportColors => Set<ReportColor>();

	/// <summary>People who sign in to this installation (created from the login screen).</summary>
	public DbSet<User> Users => Set<User>();

	/// <summary>Epochs of the data: the original database, then one per database import.</summary>
	public DbSet<DatabaseEpoch> DatabaseEpochs => Set<DatabaseEpoch>();

	/// <summary>Activity log: one row per action (a save per table, an export, an import).</summary>
	public DbSet<ActivityEntry> ActivityEntries => Set<ActivityEntry>();

	/// <summary>Field changes behind the saves of the activity log.</summary>
	public DbSet<ActivityChange> ActivityChanges => Set<ActivityChange>();

	/// <summary>Administrator's settings of this installation (single-row table).</summary>
	public DbSet<AppSettings> AppSettings => Set<AppSettings>();

	/// <summary>Configures every entity's rules via the Fluent API (used by migrations and at runtime).</summary>
	protected override void OnModelCreating(ModelBuilder b)
	{
		b.Entity<Speciality>(e =>
		{
			e.Property(x => x.Name).IsRequired().HasMaxLength(100);
			e.HasIndex(x => x.Name).IsUnique();

			// Soft-delete flag without a global query filter: Speciality is the required
			// principal of Employee, so a filter would drop employees from reports.
			e.Property(x => x.IsActive).HasDefaultValue(true);
		});

		b.Entity<ShiftCode>(e =>
		{
			e.Property(x => x.Code).IsRequired().HasMaxLength(10);
			e.Property(x => x.Description).IsRequired().HasMaxLength(100);
			e.HasIndex(x => x.Code).IsUnique();

			// Store the ShiftSegment enum as readable text in SQLite (e.g. "Day"/"Night") instead of a number.
			e.Property(x => x.Segment).HasConversion<string>().HasMaxLength(20);

			// Soft-delete flag, default active. Deliberately NO global query filter here:
			// ShiftCode is the required principal of ShiftDay, so a filter would turn every
			// ShiftDay -> ShiftCode navigation into an INNER JOIN on active codes only and
			// silently drop historical entries of a retired code from reports.
			// Filter explicitly (Where(c => c.IsActive)) only where new picks are offered.
			e.Property(x => x.IsActive).HasDefaultValue(true);
		});

		b.Entity<Employee>(e =>
		{
			// Soft-delete: default active, and a global filter that hides inactive rows
			// from every query (use IgnoreQueryFilters() to include them when needed).
			e.Property(x => x.IsActive).HasDefaultValue(true);
			e.HasQueryFilter(x => x.IsActive);

			e.Property(x => x.LastName).IsRequired().HasMaxLength(80);
			e.Property(x => x.FirstName).IsRequired().HasMaxLength(80);

			// Required specialty; Restrict prevents deleting a specialty still in use.
			e.HasOne(x => x.Speciality)
			 .WithMany(s => s.Employees)
			 .HasForeignKey(x => x.SpecialityId)
			 .OnDelete(DeleteBehavior.Restrict);

			// Optional employment type / work position (nullable FKs), also Restrict.
			e.HasOne(x => x.EmploymentType)
			 .WithMany(t => t.Employees)
			 .HasForeignKey(x => x.EmploymentTypeId)
			 .OnDelete(DeleteBehavior.Restrict);

			e.HasOne(x => x.WorkPosition)
			 .WithMany(p => p.Employees)
			 .HasForeignKey(x => x.WorkPositionId)
			 .OnDelete(DeleteBehavior.Restrict);
		});

		b.Entity<ShiftDay>(e =>
		{
			e.HasIndex(x => new { x.EmployeeId, x.Date }).IsUnique();

			e.HasOne(x => x.Employee)
			 .WithMany(emp => emp.ShiftDays)
			 .HasForeignKey(x => x.EmployeeId)
			 .OnDelete(DeleteBehavior.Cascade);

			e.HasOne(x => x.ShiftCode)
			 .WithMany()
			 .HasForeignKey(x => x.ShiftCodeId)
			 .OnDelete(DeleteBehavior.Restrict);
		});

		// --- Hours rules (pay categories, holidays, monthly limits) ---
		b.Entity<ShiftCodeHours>(e =>
		{
			// One row per code + kind of day + category, so the composite key is the natural key.
			e.HasKey(x => new { x.ShiftCodeId, x.DayType, x.Category });

			// Enums as readable text in SQLite (e.g. "Weekday", "ToComplete"), like Segment.
			e.Property(x => x.DayType).HasConversion<string>().HasMaxLength(20);
			e.Property(x => x.Category).HasConversion<string>().HasMaxLength(20);

			// decimal(5,2) is plenty for an hours value (up to 999.99).
			e.Property(x => x.Hours).HasColumnType("decimal(5,2)");

			// Configured from the dependent side only (no collection added on ShiftCode).
			// Cascade: the rows belong to the code (codes are soft-deleted in practice anyway).
			e.HasOne(x => x.ShiftCode)
			 .WithMany()
			 .HasForeignKey(x => x.ShiftCodeId)
			 .OnDelete(DeleteBehavior.Cascade);
		});

		b.Entity<Holiday>(e =>
		{
			// One holiday per date (same-date holidays are merged into one name).
			e.HasIndex(x => x.Date).IsUnique();
			e.Property(x => x.Name).IsRequired().HasMaxLength(100);
		});

		b.Entity<HourLimits>(e =>
		{
			// All limits are hour values, same precision as the other hour columns.
			e.Property(x => x.ToCompleteMonthly).HasColumnType("decimal(5,2)");
			e.Property(x => x.SimpleMonthly).HasColumnType("decimal(5,2)");
			e.Property(x => x.NightMonthly).HasColumnType("decimal(5,2)");
			e.Property(x => x.SundayMonthly).HasColumnType("decimal(5,2)");
			e.Property(x => x.HolidayPerHoliday).HasColumnType("decimal(5,2)");
			e.Property(x => x.ToCompletePlusHolidayMonthly).HasColumnType("decimal(5,2)");
		});

		// --- New lookups ---
		b.Entity<EmploymentType>(e =>
		{
			e.Property(x => x.Code).IsRequired().HasMaxLength(10);
			e.Property(x => x.Name).IsRequired().HasMaxLength(60);
			e.HasIndex(x => x.Code).IsUnique();

			// Soft-delete flag, no global query filter (same reason as Speciality).
			e.Property(x => x.IsActive).HasDefaultValue(true);
		});

		b.Entity<Signatory>(e =>
		{
			// Role may hold several lines ("\n"); the sizes leave room for the longest real ones.
			e.Property(x => x.Role).IsRequired().HasMaxLength(200);
			e.Property(x => x.FullName).IsRequired().HasMaxLength(100);
			e.Property(x => x.Title).IsRequired().HasMaxLength(150);

			// Soft-delete flag, no global query filter (employees keep pointing at it).
			e.Property(x => x.IsActive).HasDefaultValue(true);

			// Employee.SignatoryId is deliberately NOT a relationship (no navigation, no FK):
			// see Employee.SignatoryId.
		});

		b.Entity<WorkPosition>(e =>
		{
			e.Property(x => x.Name).IsRequired().HasMaxLength(60);
			e.HasIndex(x => x.Name).IsUnique();

			// Soft-delete flag, no global query filter (same reason as Speciality).
			e.Property(x => x.IsActive).HasDefaultValue(true);
		});

		// --- Editable texts of the official forms ---
		b.Entity<ReportText>(e =>
		{
			// Enums as readable text in SQLite (e.g. "ShiftTable", "Header"), like Segment.
			e.Property(x => x.Form).HasConversion<string>().HasMaxLength(30);
			e.Property(x => x.Section).HasConversion<string>().HasMaxLength(10);

			e.Property(x => x.Block).IsRequired().HasMaxLength(40);

			// Same size as ReportTextLine.MaxLength; an empty text is valid (line not printed).
			e.Property(x => x.Text).IsRequired().HasMaxLength(1000);
			e.Property(x => x.PreviousText).HasMaxLength(1000);

			// A line is identified by form + block + position (the seeder relies on it).
			e.HasIndex(x => new { x.Form, x.Block, x.Line }).IsUnique();
		});

		// --- Users of this installation ---
		b.Entity<User>(e =>
		{
			// Same size as UserRules.MaxUsernameLength. The index stops exact duplicates; the
			// case-insensitive check is done in AuthService (SQLite compares Greek case-sensitively).
			e.Property(x => x.Username).IsRequired().HasMaxLength(40);
			e.HasIndex(x => x.Username).IsUnique();

			// Same sizes as UserRules.MaxFullNameLength / MaxEmailLength. Empty for users created
			// before the columns existed, hence the default.
			e.Property(x => x.FullName).IsRequired().HasMaxLength(80).HasDefaultValue("");
			e.Property(x => x.Email).IsRequired().HasMaxLength(100).HasDefaultValue("");

			e.Property(x => x.PasswordSalt).IsRequired().HasMaxLength(64);
			e.Property(x => x.PasswordHash).IsRequired().HasMaxLength(64);

			// Soft-delete flag without a global query filter: other tables will refer to users
			// by key, and a filter would hide the rows of a deactivated user's past work.
			e.Property(x => x.IsActive).HasDefaultValue(true);
		});

		// --- Activity log (written by ActivityInterceptor / ActivityLog) ---
		b.Entity<DatabaseEpoch>(e =>
		{
			e.Property(x => x.Name).IsRequired().HasMaxLength(200);
		});

		b.Entity<ActivityEntry>(e =>
		{
			e.Property(x => x.Username).IsRequired().HasMaxLength(40);
			e.Property(x => x.Action).IsRequired().HasMaxLength(40);
			e.Property(x => x.Subject).IsRequired().HasMaxLength(100);
			e.Property(x => x.Details).IsRequired().HasMaxLength(1000);

			// The log is listed per epoch, newest first, optionally per user.
			e.HasIndex(x => new { x.DatabaseEpochId, x.Timestamp });

			// Restrict: an epoch with log lines is never deleted.
			e.HasOne(x => x.DatabaseEpoch)
			 .WithMany()
			 .HasForeignKey(x => x.DatabaseEpochId)
			 .OnDelete(DeleteBehavior.Restrict);

			// The field changes belong to their entry.
			e.HasMany(x => x.Changes)
			 .WithOne()
			 .HasForeignKey(c => c.ActivityEntryId)
			 .OnDelete(DeleteBehavior.Cascade);
		});

		b.Entity<ActivityChange>(e =>
		{
			e.Property(x => x.RowKey).IsRequired().HasMaxLength(200);
			e.Property(x => x.Kind).IsRequired().HasMaxLength(20);
			e.Property(x => x.Field).IsRequired().HasMaxLength(100);

			// Same limit as ActivityInterceptor.MaxValueLength, plus the "…" of a cut value.
			e.Property(x => x.OldValue).HasMaxLength(501);
			e.Property(x => x.NewValue).HasMaxLength(501);
		});

		// --- Editable colours of the reports ---
		b.Entity<ReportColor>(e =>
		{
			// One row per colour key (the seeder relies on it).
			e.Property(x => x.Key).IsRequired().HasMaxLength(60);
			e.HasIndex(x => x.Key).IsUnique();

			// "#RRGGBB".
			e.Property(x => x.Hex).IsRequired().HasMaxLength(7);
			e.Property(x => x.PreviousHex).HasMaxLength(7);
		});
	}
}