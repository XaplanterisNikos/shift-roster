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

	/// <summary>Placement lookup (the department the employee is placed in).</summary>
	public DbSet<Placement> Placements => Set<Placement>();

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

			e.Property(x => x.IsActive).HasDefaultValue(true);
		});

		b.Entity<ShiftCode>(e =>
		{
			e.Property(x => x.Code).IsRequired().HasMaxLength(10);
			e.Property(x => x.Description).IsRequired().HasMaxLength(100);
			e.HasIndex(x => x.Code).IsUnique();

			e.Property(x => x.Segment).HasConversion<string>().HasMaxLength(20);

			e.Property(x => x.IsActive).HasDefaultValue(true);
		});

		b.Entity<Employee>(e =>
		{
			e.Property(x => x.IsActive).HasDefaultValue(true);
			e.HasQueryFilter(x => x.IsActive);

			e.Property(x => x.LastName).IsRequired().HasMaxLength(80);
			e.Property(x => x.FirstName).IsRequired().HasMaxLength(80);

			e.HasOne(x => x.Speciality)
			 .WithMany(s => s.Employees)
			 .HasForeignKey(x => x.SpecialityId)
			 .OnDelete(DeleteBehavior.Restrict);

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

		b.Entity<ShiftCodeHours>(e =>
		{
			e.HasKey(x => new { x.ShiftCodeId, x.DayType, x.Category });

			e.Property(x => x.DayType).HasConversion<string>().HasMaxLength(20);
			e.Property(x => x.Category).HasConversion<string>().HasMaxLength(20);

			e.Property(x => x.Hours).HasColumnType("decimal(5,2)");

			e.HasOne(x => x.ShiftCode)
			 .WithMany()
			 .HasForeignKey(x => x.ShiftCodeId)
			 .OnDelete(DeleteBehavior.Cascade);
		});

		b.Entity<Holiday>(e =>
		{
			e.HasIndex(x => x.Date).IsUnique();
			e.Property(x => x.Name).IsRequired().HasMaxLength(100);
		});

		b.Entity<HourLimits>(e =>
		{
			e.Property(x => x.ToCompleteMonthly).HasColumnType("decimal(5,2)");
			e.Property(x => x.SimpleMonthly).HasColumnType("decimal(5,2)");
			e.Property(x => x.NightMonthly).HasColumnType("decimal(5,2)");
			e.Property(x => x.SundayMonthly).HasColumnType("decimal(5,2)");
			e.Property(x => x.HolidayPerHoliday).HasColumnType("decimal(5,2)");
			e.Property(x => x.ToCompletePlusHolidayMonthly).HasColumnType("decimal(5,2)");
		});

		b.Entity<EmploymentType>(e =>
		{
			e.Property(x => x.Code).IsRequired().HasMaxLength(10);
			e.Property(x => x.Name).IsRequired().HasMaxLength(60);
			e.HasIndex(x => x.Code).IsUnique();

			e.Property(x => x.IsActive).HasDefaultValue(true);
		});

		b.Entity<Signatory>(e =>
		{
			e.Property(x => x.Role).IsRequired().HasMaxLength(200);
			e.Property(x => x.FullName).IsRequired().HasMaxLength(100);
			e.Property(x => x.Title).IsRequired().HasMaxLength(150);

			e.Property(x => x.IsActive).HasDefaultValue(true);

		});

		b.Entity<WorkPosition>(e =>
		{
			e.Property(x => x.Name).IsRequired().HasMaxLength(60);
			e.HasIndex(x => x.Name).IsUnique();

			e.Property(x => x.IsActive).HasDefaultValue(true);
		});

		b.Entity<Placement>(e =>
		{
			e.Property(x => x.Name).IsRequired().HasMaxLength(60);
			e.HasIndex(x => x.Name).IsUnique();

			e.Property(x => x.IsActive).HasDefaultValue(true);

		});

		b.Entity<ReportText>(e =>
		{
			e.Property(x => x.Form).HasConversion<string>().HasMaxLength(30);
			e.Property(x => x.Section).HasConversion<string>().HasMaxLength(10);

			e.Property(x => x.Block).IsRequired().HasMaxLength(40);

			e.Property(x => x.Text).IsRequired().HasMaxLength(1000);
			e.Property(x => x.PreviousText).HasMaxLength(1000);

			e.HasIndex(x => new { x.Form, x.Block, x.Line }).IsUnique();
		});

		b.Entity<User>(e =>
		{
			e.Property(x => x.Username).IsRequired().HasMaxLength(40);
			e.HasIndex(x => x.Username).IsUnique();

			e.Property(x => x.FullName).IsRequired().HasMaxLength(80).HasDefaultValue("");
			e.Property(x => x.Email).IsRequired().HasMaxLength(100).HasDefaultValue("");

			e.Property(x => x.PasswordSalt).IsRequired().HasMaxLength(64);
			e.Property(x => x.PasswordHash).IsRequired().HasMaxLength(64);

			e.Property(x => x.IsActive).HasDefaultValue(true);
		});

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

			e.HasIndex(x => new { x.DatabaseEpochId, x.Timestamp });

			e.HasOne(x => x.DatabaseEpoch)
			 .WithMany()
			 .HasForeignKey(x => x.DatabaseEpochId)
			 .OnDelete(DeleteBehavior.Restrict);

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

			e.Property(x => x.OldValue).HasMaxLength(501);
			e.Property(x => x.NewValue).HasMaxLength(501);
		});

		b.Entity<ReportColor>(e =>
		{
			e.Property(x => x.Key).IsRequired().HasMaxLength(60);
			e.HasIndex(x => x.Key).IsUnique();

			e.Property(x => x.Hex).IsRequired().HasMaxLength(7);
			e.Property(x => x.PreviousHex).HasMaxLength(7);
		});
	}
}