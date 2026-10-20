using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Vardiologio.Application.ReportColors;
using Vardiologio.Application.Reports;
using Vardiologio.Application.ReportTexts;
using Vardiologio.Domain.Entities;
using Vardiologio.Domain.Enums;

namespace Vardiologio.Infrastructure.Persistence.Seeding;

/// <summary>
/// Populates the database with base data on first run. Every step is idempotent
/// (skips if its table already has rows), so it is safe to run at every startup.
/// Reads the reference values from <see cref="SeedData"/>; the employees and the texts of the
/// official forms come from JSON files, so no personal or organisation data is in the source.
/// </summary>
public static class DatabaseSeeder
{
	/// <summary>
	/// Runs all seed steps in dependency order: lookups first, then employees
	/// (which need existing Speciality ids).
	/// </summary>
	/// <param name="db">The database to seed.</param>
	/// <param name="reportData">The organisation's report data, read from its file (see <see cref="ReportDataFile"/>).</param>
	public static void Seed(AppDbContext db, ReportDataSeed reportData)
	{
		SeedSpecialities(db);
		SeedEmploymentTypes(db);   
		SeedWorkPositions(db);
		SeedShiftCodes(db);
		SeedShiftCodeRules(db); // after codes: fixes/adds codes once, then their hour split
		SeedHolidays(db);
		SeedHourLimits(db);
		SeedReportTexts(db, reportData.Texts ?? []);
		SeedFixedTermAttendanceTexts(db);   // after the file: copies the staff form when the file has no such form
		SeedMonthlyTotalFixedTermBand(db);  // after the file: the Σ.Ω. separator line, when the file has none
		SeedIndividualMonthlyTexts(db);     // after the file: the individual sheet's form, when the file has none
		SeedReportColors(db);
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
	/// Adds the default texts of the official forms. Works per line, not per table: a line
	/// that already exists is never touched (the user may have edited it), and a line that a
	/// later version adds to the report data file is inserted on the next start.
	/// </summary>
	private static void SeedReportTexts(AppDbContext db, IReadOnlyList<ReportTextBlockSeed> blocks)
	{
		// Lines already stored, by their natural key (form + block + position).
		var existing = db.ReportTexts
			.Select(t => new { t.Form, t.Block, t.Line })
			.AsEnumerable()
			.Select(t => (t.Form, t.Block, t.Line))
			.ToHashSet();

		foreach (var block in blocks)
		{
			if (block.Lines is null) continue;   // a block without lines in the file: nothing to add

			for (var line = 0; line < block.Lines.Length; line++)
			{
				if (existing.Contains((block.Form, block.Block, line))) continue;

				db.ReportTexts.Add(new ReportText
				{
					Form = block.Form,
					Section = block.Section,
					Block = block.Block,
					Line = line,
					Text = block.Lines[line]
				});
			}
		}
		db.SaveChanges();
	}

	/// <summary>Added to the "ΠΙΝΑΚΑΣ ΠΑΡΟΥΣΙΩΝ ΠΡΟΣΩΠΙΚΟΥ" title line on the fixed-term form.</summary>
	private const string FixedTermTitleSuffix = " Ι.Δ. ΟΡΙΣΜΕΝΟΥ ΧΡΟΝΟΥ";

	/// <summary>
	/// Gives the fixed-term attendance form (ΙΔΟΧ) its lines when it has none: a copy of the
	/// staff attendance form as it is now in the database (the user's edits included), with
	/// "Ι.Δ. ΟΡΙΣΜΕΝΟΥ ΧΡΟΝΟΥ" added to the title line that names the table. The form was added
	/// after the first installations, so it is not in their report data file; a file that does
	/// list it wins, because its lines are already there when this runs. Runs once: afterwards
	/// the lines exist and are edited in Παράμετροι → Αναφορών like every other form.
	/// </summary>
	private static void SeedFixedTermAttendanceTexts(AppDbContext db)
	{
		if (db.ReportTexts.Any(t => t.Form == ReportForm.AttendanceFixedTerm)) return;

		var staffLines = db.ReportTexts
			.AsNoTracking()
			.Where(t => t.Form == ReportForm.AttendanceStaff)
			.OrderBy(t => t.Block).ThenBy(t => t.Line)
			.ToList();

		var titleDone = false;
		foreach (var source in staffLines)
		{
			var text = source.Text;

			// The first title line that names the attendance table gets the suffix.
			if (!titleDone && source.Block == ReportTextBlocks.Titles
				&& GreekText.Upper(text).Contains("ΠΑΡΟΥΣΙ", StringComparison.Ordinal))
			{
				text = text.TrimEnd() + FixedTermTitleSuffix;
				titleDone = true;
			}

			db.ReportTexts.Add(new ReportText
			{
				Form = ReportForm.AttendanceFixedTerm,
				Section = source.Section,
				Block = source.Block,
				Line = source.Line,
				Text = text
			});
		}
		db.SaveChanges();
	}

	/// <summary>
	/// Gives the Σ.Ω. form its separator line ("ΟΡΙΣΜΕΝΟΥ ΧΡΟΝΟΥ"), the row inside the table
	/// before the fixed-term employees. The line was added after the first installations, so
	/// their report data file does not have it; it is not private data, so its default lives
	/// here. Runs once: when the line exists (from the file or an earlier start) it is left
	/// alone, and afterwards it is edited in Παράμετροι → Αναφορών like every other line.
	/// </summary>
	private static void SeedMonthlyTotalFixedTermBand(AppDbContext db)
	{
		if (db.ReportTexts.Any(t => t.Form == ReportForm.MonthlyTotal && t.Block == ReportTextBlocks.FixedTermBand))
			return;

		db.ReportTexts.Add(new ReportText
		{
			Form = ReportForm.MonthlyTotal,
			Section = ReportSection.Header,   // part of the table, listed with the blocks above the signatures
			Block = ReportTextBlocks.FixedTermBand,
			Line = 0,
			Text = ReportTextBlocks.FixedTermBandDefault
		});
		db.SaveChanges();
	}

	/// <summary>
	/// Gives the individual monthly form (official Excel / PDF of the "Ατομική μηνιαία") its
	/// lines. The form was added after the first installations, so their report data file does
	/// not have it. The letterhead is the organisation's data, so it is copied from the overtime
	/// certificate as it is now in the database (the user's edits included) instead of living in
	/// the code; the other blocks are plain form wording (<see cref="ReportTextBlocks.IndividualMonthlyDefaults"/>).
	/// Works per line like <see cref="SeedReportTexts"/>: a line that exists is never touched.
	/// </summary>
	private static void SeedIndividualMonthlyTexts(AppDbContext db)
	{
		const ReportForm form = ReportForm.IndividualMonthly;

		var existing = db.ReportTexts
			.Where(t => t.Form == form)
			.Select(t => new { t.Block, t.Line })
			.AsEnumerable()
			.Select(t => (t.Block, t.Line))
			.ToHashSet();

		void Add(ReportSection section, string block, int line, string text)
		{
			if (!existing.Add((block, line))) return;   // already there (maybe edited): keep it
			db.ReportTexts.Add(new ReportText { Form = form, Section = section, Block = block, Line = line, Text = text });
		}

		// Letterhead: copied once, only while the form has no letterhead at all.
		if (!existing.Any(e => e.Block == ReportTextBlocks.Letterhead))
		{
			var source = db.ReportTexts
				.AsNoTracking()
				.Where(t => t.Form == ReportForm.OvertimeCertificate && t.Block == ReportTextBlocks.Letterhead)
				.OrderBy(t => t.Line)
				.ToList();

			if (source.Count == 0)
			{
				// Nothing to copy from (e.g. a fresh demo install): eight empty lines to fill in.
				for (var line = 0; line < 8; line++)
					Add(ReportSection.Header, ReportTextBlocks.Letterhead, line, "");
			}
			else
			{
				foreach (var line in source)
					Add(ReportSection.Header, ReportTextBlocks.Letterhead, line.Line, line.Text);
			}
		}

		foreach (var (block, footer, lines) in ReportTextBlocks.IndividualMonthlyDefaults)
			for (var line = 0; line < lines.Length; line++)
				Add(footer ? ReportSection.Footer : ReportSection.Header, block, line, lines[line]);

		db.SaveChanges();
	}

	/// <summary>
	/// Adds the default colours of the reports. Works per key: a colour that already exists is
	/// never touched (the user may have changed it), and a colour that a later version adds to
	/// the catalogue is inserted on the next start — as the colour of its
	/// <see cref="ReportColorCatalog.Entry.SeedFromKey"/> when it has one, else as its default.
	/// </summary>
	private static void SeedReportColors(AppDbContext db)
	{
		// Stored colours by key: to skip existing keys and to copy a colour a new key starts from.
		var stored = db.ReportColors.ToDictionary(c => c.Key, c => c.Hex);

		foreach (var entry in ReportColorCatalog.All)
		{
			if (stored.ContainsKey(entry.Key)) continue;

			var hex = entry.SeedFromKey is not null
				&& stored.TryGetValue(entry.SeedFromKey, out var source)
				&& ReportColorCatalog.IsHex(source)
					? source
					: entry.DefaultHex;

			db.ReportColors.Add(new ReportColor { Key = entry.Key, Hex = hex });
			stored[entry.Key] = hex;   // a later entry may start from this one
		}
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