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
		SeedShiftCodeRules(db); // after codes: fixes/adds codes once, then their hour split
		SeedHolidays(db);
		SeedHourLimits(db);
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

	/// <summary>Seeds shift/status codes (times, Day/Night segment, allowed days). Idempotent.</summary>
	private static void SeedShiftCodes(AppDbContext db)
	{
		if (db.ShiftCodes.Any()) return;
		db.ShiftCodes.AddRange(SeedData.ShiftCodes.Select(s => new ShiftCode
		{
			Code = s.Code,
			Description = s.Description,
			StartTime = s.Start,
			EndTime = s.End,
			Segment = s.Segment,
			AllowedDays = s.AllowedDays
		}));
		db.SaveChanges();
	}

	/// <summary>
	/// One-time bootstrap of the hours rules, keyed on the ShiftCodeHours table being empty
	/// (i.e. it runs once, right after the migration that created it, and never again, so
	/// later edits made in the app are not overwritten).
	/// On a database created before these rules it first brings the working codes in line
	/// with the confirmed table: corrects times/description/segment/allowed days (e.g. code 13
	/// 22:00–04:30 -> 22:00–06:00) and adds the missing codes (14, 15). On a fresh database the
	/// codes were just seeded from the same table, so that part changes nothing.
	/// Then seeds the hour split, resolving each ShiftCode by its Code.
	/// </summary>
	private static void SeedShiftCodeRules(AppDbContext db)
	{
		if (db.ShiftCodeHours.Any()) return;   // already bootstrapped -> skip

		// Sync the working codes (status codes are left untouched).
		var codesByCode = db.ShiftCodes.ToDictionary(c => c.Code);
		foreach (var seed in SeedData.ShiftCodes.Where(x => x.Start.HasValue))
		{
			if (!codesByCode.TryGetValue(seed.Code, out var code))
			{
				// Missing in an older database (14, 15): add it.
				code = new ShiftCode { Code = seed.Code };
				db.ShiftCodes.Add(code);
			}

			code.Description = seed.Description;
			code.StartTime = seed.Start;
			code.EndTime = seed.End;
			code.Segment = seed.Segment;
			code.AllowedDays = seed.AllowedDays;
		}
		db.SaveChanges();   // new codes get their Ids here

		// Map "Code" -> Id, now including any codes added above.
		var idByCode = db.ShiftCodes.ToDictionary(c => c.Code, c => c.Id);

		foreach (var (code, day, category, hours) in SeedData.ShiftCodeHours)
		{
			if (!idByCode.TryGetValue(code, out var shiftCodeId))
				throw new InvalidOperationException($"Unknown shift code in hours seed: '{code}'");

			db.ShiftCodeHours.Add(new ShiftCodeHours
			{
				ShiftCodeId = shiftCodeId,
				DayType = day,
				Category = category,
				Hours = hours
			});
		}
		db.SaveChanges();
	}

	/// <summary>Seeds the 2026 public holidays. Idempotent.</summary>
	private static void SeedHolidays(AppDbContext db)
	{
		if (db.Holidays.Any()) return;
		db.Holidays.AddRange(SeedData.Holidays.Select(h => new Holiday { Date = h.Date, Name = h.Name }));
		db.SaveChanges();
	}

	/// <summary>Seeds the single row of monthly limits. Idempotent.</summary>
	private static void SeedHourLimits(AppDbContext db)
	{
		if (db.HourLimits.Any()) return;

		var l = SeedData.HourLimits;
		db.HourLimits.Add(new HourLimits
		{
			ToCompleteMonthly = l.ToComplete,
			SimpleMonthly = l.Simple,
			NightMonthly = l.Night,
			SundayMonthly = l.Sunday,
			HolidayPerHoliday = l.PerHoliday,
			ToCompletePlusHolidayMonthly = l.ToCompletePlusHoliday
		});
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
}