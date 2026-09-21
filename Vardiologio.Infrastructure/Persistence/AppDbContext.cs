using Microsoft.EntityFrameworkCore;
using Vardiologio.Domain.Entities;

namespace Vardiologio.Infrastructure.Persistence;

/// <summary>
/// EF Core database context: exposes the tables (DbSet) and configures the schema
/// (keys, required fields, indexes, relationships, delete behaviours, the Employee
/// soft-delete query filter, and the ShiftCode.Segment enum-to-text mapping).
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

	/// <summary>Extra ("supplementary") hours per shift code — 1:1 with ShiftCode.</summary>
	public DbSet<ShiftExtraHours> ShiftExtraHours => Set<ShiftExtraHours>();

	/// <summary>Configures every entity's rules via the Fluent API (used by migrations and at runtime).</summary>
	protected override void OnModelCreating(ModelBuilder b)
	{
		b.Entity<Speciality>(e =>
		{
			e.Property(x => x.Name).IsRequired().HasMaxLength(100);
			e.HasIndex(x => x.Name).IsUnique();
		});

		b.Entity<ShiftCode>(e =>
		{
			e.Property(x => x.Code).IsRequired().HasMaxLength(10);
			e.Property(x => x.Description).IsRequired().HasMaxLength(100);
			e.HasIndex(x => x.Code).IsUnique();

			// Store the ShiftSegment enum as readable text in SQLite (e.g. "Day"/"Night") instead of a number.
			e.Property(x => x.Segment).HasConversion<string>().HasMaxLength(20);

			// Soft-delete, same convention as Employee: default active, global filter hides
			// inactive rows everywhere except when IgnoreQueryFilters() is used explicitly.
			e.Property(x => x.IsActive).HasDefaultValue(true);
			e.HasQueryFilter(x => x.IsActive);
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

		b.Entity<ShiftExtraHours>(e =>
		{
			// Shared primary key => the PK is also the FK to ShiftCode (true 1:1).
			e.HasKey(x => x.ShiftCodeId);

			// decimal(5,2) is plenty for an hours value (up to 999.99).
			e.Property(x => x.Hours).HasColumnType("decimal(5,2)");

			// Configure the 1:1 from the dependent side only, so no navigation
			// property is added on ShiftCode. Cascade: removing a shift code
			// removes its extra-hours row too.
			e.HasOne(x => x.ShiftCode)
			 .WithOne()
			 .HasForeignKey<ShiftExtraHours>(x => x.ShiftCodeId)
			 .OnDelete(DeleteBehavior.Cascade);
		});

		// --- New lookups ---
		b.Entity<EmploymentType>(e =>
		{
			e.Property(x => x.Code).IsRequired().HasMaxLength(10);
			e.Property(x => x.Name).IsRequired().HasMaxLength(60);
			e.HasIndex(x => x.Code).IsUnique();
		});

		b.Entity<WorkPosition>(e =>
		{
			e.Property(x => x.Name).IsRequired().HasMaxLength(60);
			e.HasIndex(x => x.Name).IsUnique();
		});

	}
}