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

	/// <summary>Hours per pay category that each shift code yields on each kind of day.</summary>
	public DbSet<ShiftCodeHours> ShiftCodeHours => Set<ShiftCodeHours>();

	/// <summary>Public holidays (αργίες), maintained per year by the user.</summary>
	public DbSet<Holiday> Holidays => Set<Holiday>();

	/// <summary>Monthly paid-hours limits (single-row settings table).</summary>
	public DbSet<HourLimits> HourLimits => Set<HourLimits>();

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

		b.Entity<WorkPosition>(e =>
		{
			e.Property(x => x.Name).IsRequired().HasMaxLength(60);
			e.HasIndex(x => x.Name).IsUnique();

			// Soft-delete flag, no global query filter (same reason as Speciality).
			e.Property(x => x.IsActive).HasDefaultValue(true);
		});

	}
}