using System.Text.Json;
using Vardiologio.Domain.Entities;

namespace Vardiologio.Infrastructure.Persistence.Seeding;

/// <summary>
/// Populates the database with base data on first run. Every step is idempotent
/// (skips if its table already has rows), so it is safe to run at every startup.
/// Reads the actual values from <see cref="SeedData"/>.
/// </summary>
public static class DatabaseSeeder
{
	/// <summary>
	/// Runs all seed steps in dependency order: lookups first, then employees
	/// (which need existing Speciality ids).
	/// </summary>
	public static void Seed(AppDbContext db)
	{
		SeedSpecialities(db);
		SeedEmploymentTypes(db);   
		SeedWorkPositions(db);
		SeedShiftCodes(db);
		SeedExtraHours(db);
		SeedEmployees(db);      // last: resolves each employee's SpecialityId by name
	}

	/// <summary>Seeds the specialties lookup. Idempotent.</summary>
	private static void SeedSpecialities(AppDbContext db)
	{
		if (db.Specialities.Any()) return;                       // table has data -> skip
		db.Specialities.AddRange(SeedData.Specialities.Select(n => new Speciality { Name = n }));
		db.SaveChanges();
	}

	/// <summary>Seeds ΕΙΔΟΣ ΕΡΓΑΣΙΑΣ lookup. Idempotent.</summary>
	private static void SeedEmploymentTypes(AppDbContext db)
	{
		if (db.EmploymentTypes.Any()) return;
		db.EmploymentTypes.AddRange(
			SeedData.EmploymentTypes.Select(t => new EmploymentType { Code = t.Code, Name = t.Name }));
		db.SaveChanges();
	}

	/// <summary>Seeds Work Position lookup. Idempotent.</summary>
	private static void SeedWorkPositions(AppDbContext db)
	{
		if (db.WorkPositions.Any()) return;
		db.WorkPositions.AddRange(
			SeedData.WorkPositions.Select(n => new WorkPosition { Name = n }));
		db.SaveChanges();
	}

	/// <summary>Seeds shift/status codes (times + Day/Night segment). Idempotent.</summary>
	private static void SeedShiftCodes(AppDbContext db)
	{
		if (db.ShiftCodes.Any()) return;
		db.ShiftCodes.AddRange(SeedData.ShiftCodes.Select(s => new ShiftCode
		{
			Code = s.Code,
			Description = s.Description,
			StartTime = s.Start,
			EndTime = s.End,
			Segment = s.Segment  
		}));
		db.SaveChanges();
	}

	/// <summary>
	/// Seeds employees from a JSON data file (kept out of source code).
	/// Prefers the real file "employees.json"; if it is not present (e.g. a fresh
	/// clone from GitHub, where the real data is git-ignored) it falls back to the
	/// committed "employees.demo.json". Idempotent; resolves SpecialityId by name.
	/// </summary>
	private static void SeedEmployees(AppDbContext db)
	{
		if (db.Employees.Any()) return;

		// JSON files are copied next to the executable (see the .csproj copy rules).
		var dir = AppContext.BaseDirectory;
		var realPath = Path.Combine(dir, "employees.json");
		var demoPath = Path.Combine(dir, "employees.demo.json");
		var path = File.Exists(realPath) ? realPath : demoPath;

		if (!File.Exists(path))
			throw new FileNotFoundException(
				"No employee seed file found (employees.json or employees.demo.json) next to the app.");

		// Deserialize; case-insensitive so camelCase JSON maps to the PascalCase record.
		var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
		var rows = JsonSerializer.Deserialize<EmployeeSeed[]>(File.ReadAllText(path), options) ?? [];

		// Map specialty name -> Id for the specialties already inserted above.
		var specByName = db.Specialities.ToDictionary(s => s.Name, s => s.Id);
		foreach (var row in rows)
		{
			if (!specByName.TryGetValue(row.Speciality, out var specId))
				throw new InvalidOperationException($"Unknown specialty in seed: '{row.Speciality}'");

			db.Employees.Add(new Employee
			{
				LastName = row.LastName,
				FirstName = row.FirstName,
				SpecialityId = specId
			});
		}
		db.SaveChanges();
	}

	/// <summary>
	/// Seeds the 1:1 extra-hours rows, resolving each ShiftCode by its Code.
	/// Idempotent: skips entirely if the table already has data.
	/// </summary>
	private static void SeedExtraHours(AppDbContext db)
	{
		if (db.ShiftExtraHours.Any()) return;   // already seeded -> skip

		// Map "Code" -> Id for the shift codes already in the database.
		var idByCode = db.ShiftCodes.ToDictionary(c => c.Code, c => c.Id);

		foreach (var (code, hours) in SeedData.ExtraHours)
		{
			if (!idByCode.TryGetValue(code, out var shiftCodeId))
				throw new InvalidOperationException($"Unknown shift code in extra-hours seed: '{code}'");

			db.ShiftExtraHours.Add(new ShiftExtraHours
			{
				ShiftCodeId = shiftCodeId,
				Hours = hours
			});
		}
		db.SaveChanges();
	}
}