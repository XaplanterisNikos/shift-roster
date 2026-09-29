using Vardiologio.Domain.Enums;

namespace Vardiologio.Infrastructure.Persistence.Seeding;

/// <summary>
/// Static, hard-coded seed data (dictionaries). This half holds the reference
/// lists; the employees (personal data) live in the SeedData.Employees.cs partial.
/// </summary>
public static partial class SeedData   
{
	/// <summary>Specialties, cleaned/de-duplicated from the source Σ.Ω. sheet.</summary>
	public static readonly string[] Specialities =
	{
		"ΔΕ Διοικητικού",
		"ΔΕ Διοικητικού - Λογιστικού",
		"ΔΕ Δομικών έργων",
		"ΔΕ Ηλεκτρολόγων",
		"ΔΕ Ηλεκτρονικών",
		"ΔΕ Ηλεκτροτεχνιτών",
		"ΔΕ Ηλεκτροτεχνιτών Οχημάτων",
		"ΔΕ Μηχανοτεχνιτών",
		"ΔΕ Μηχανοτεχνιτών Οχημάτων",
		"ΔΕ Οδηγών",
		"ΔΕ Πληροφορικής",
		"ΔΕ Τεχνικού",
		"ΔΕ Χειριστών Μηχανημάτων Εργου",
		"ΠΕ Διοικητικού - Οικονομικού",
		"ΠΕ Πολιτικών Μηχανικών",
		"ΠΕ Χημικών Μηχανικών",
		"ΤΕ Διοικητικού - Λογιστικού",
		"ΤΕ Τεχνικών Εφαρμογών",
		"ΥΕ Εργάτης",
		"ΥΕ Εργατών Γενικών καθηκόντων",
		"ΥΕ Προσωπικού Καθαριότητας Εξωτ. Χώρων",
		"ΥΕ Προσωπικού Καθαριότητας Εσωτ. Χώρων",
	};

	// Short aliases to keep the seed tables below readable.
	private const DayType W = DayType.Weekday;
	private const DayType Sat = DayType.Saturday;
	private const DayType Sun = DayType.Sunday;
	private const DayType Hol = DayType.Holiday;

	/// <summary>
	/// Shift/status codes: working shifts carry times, a Day/Night segment and the kinds of day
	/// they are allowed on; statuses have none of these. Allowed days as confirmed by the
	/// manager (12 kept on Saturday and 15 on weekdays, as stated in the confirmed hour splits).
	/// </summary>
	public static readonly (string Code, string Description, TimeOnly? Start, TimeOnly? End, ShiftSegment? Segment, DayType AllowedDays)[] ShiftCodes =
	{
		("1",  "00:00 – 06:30", new TimeOnly(0, 0),  new TimeOnly(6, 30), ShiftSegment.Night, W),
		("2",  "00:00 – 06:00", new TimeOnly(0, 0),  new TimeOnly(6, 0),  ShiftSegment.Night, Sat | Hol),
		("3",  "03:00 – 09:30", new TimeOnly(3, 0),  new TimeOnly(9, 30), ShiftSegment.Day,   W),
		("4",  "04:00 – 10:30", new TimeOnly(4, 0),  new TimeOnly(10, 30),ShiftSegment.Day,   W),
		("5",  "05:00 – 11:30", new TimeOnly(5, 0),  new TimeOnly(11, 30),ShiftSegment.Day,   W),
		("6",  "06:00 – 12:30", new TimeOnly(6, 0),  new TimeOnly(12, 30),ShiftSegment.Day,   W | Sat | Sun | Hol),
		("7",  "06:00 – 14:00", new TimeOnly(6, 0),  new TimeOnly(14, 0), ShiftSegment.Day,   Sat | Sun | Hol),
		("8",  "06:00 – 16:00", new TimeOnly(6, 0),  new TimeOnly(16, 0), ShiftSegment.Day,   Sat | Sun | Hol),
		("9",  "08:30 – 15:00", new TimeOnly(8, 30), new TimeOnly(15, 0), ShiftSegment.Day,   W),
		("10", "11:30 – 18:00", new TimeOnly(11, 30),new TimeOnly(18, 0), ShiftSegment.Day,   W),
		("11", "15:30 – 22:00", new TimeOnly(15, 30),new TimeOnly(22, 0), ShiftSegment.Day,   W),
		("12", "22:00 – 06:00", new TimeOnly(22, 0), new TimeOnly(6, 0),  ShiftSegment.Night, Sat),
		("13", "22:00 – 06:00", new TimeOnly(22, 0), new TimeOnly(6, 0),  ShiftSegment.Night, W),   // was 22:00–04:30 (corrected)
		("14", "03:00 – 11:30", new TimeOnly(3, 0),  new TimeOnly(11, 30),ShiftSegment.Day,   W),   // new
		("15", "22:00 – 06:00", new TimeOnly(22, 0), new TimeOnly(6, 0),  ShiftSegment.Night, W),   // new: Thursday -> Friday
		("Υ.Δ.", "Ν105", null, null, null, DayType.None),
		("Α",    "ΑΣΘΕΝΕΙΑ", null, null, null, DayType.None),
		("ΑΡ.",  "ΑΡΓΙΑ", null, null, null, DayType.None),
		("Κ",    "ΚΑΝΟΝΙΚΗ ΑΔΕΙΑ", null, null, null, DayType.None),
		("Δ",    "ΑΝΑΤΡΟΦΗΣ ΤΕΚΝΟΥ", null, null, null, DayType.None),
		("Ε.Α",  "ΕΙΔΙΚΗ ΑΔΕΙΑ", null, null, null, DayType.None),
		("Κ.Σ",  "ΚΑΙΡΙΚΩΝ ΣΥΝΘΗΚΩΝ", null, null, null, DayType.None),
		("Ρ",    "ΡΕΠΟ", null, null, null, DayType.None),
		("Σ",    "ΣΥΝΔΙΚΑΛΙΣΤΙΚΗ", null, null, null, DayType.None),
		("Α.Τ",  "ΑΔΕΙΑ ΤΟΚΕΤΟΥ", null, null, null, DayType.None),
	};

	/// <summary>
	/// Hours per pay category that each code yields on each kind of day (confirmed table).
	/// Codes 6, 9, 10, 11 on a weekday are regular hours only, so they have no rows.
	/// Code 2 on a Saturday counts as night overtime (the Saturday 1st-of-month variant of 12).
	/// </summary>
	public static readonly (string Code, DayType Day, HourCategory Category, decimal Hours)[] ShiftCodeHours =
	{
		// Weekday: night hours of the regular schedule (+ overtime beyond 6.5h).
		("1",  W, HourCategory.ToComplete, 6m),
		("3",  W, HourCategory.ToComplete, 3m),
		("4",  W, HourCategory.ToComplete, 2m),
		("5",  W, HourCategory.ToComplete, 1m),
		("13", W, HourCategory.ToComplete, 6.5m),
		("13", W, HourCategory.Night,      1.5m),
		("14", W, HourCategory.ToComplete, 3m),
		("14", W, HourCategory.Simple,     2m),
		("15", W, HourCategory.ToComplete, 6m),
		("15", W, HourCategory.Night,      2m),

		// Saturday: everything is overtime.
		("12", Sat, HourCategory.Night,  8m),
		("2",  Sat, HourCategory.Night,  6m),
		("6",  Sat, HourCategory.Simple, 6.5m),
		("7",  Sat, HourCategory.Simple, 8m),
		("8",  Sat, HourCategory.Simple, 10m),

		// Sunday.
		("6", Sun, HourCategory.Sunday, 6.5m),
		("7", Sun, HourCategory.Sunday, 8m),
		("8", Sun, HourCategory.Sunday, 10m),

		// Holiday (code 2 = Good Friday night, counted as Προς συμπλήρωση).
		("2", Hol, HourCategory.ToComplete, 6m),
		("6", Hol, HourCategory.Holiday,    6.5m),
		("7", Hol, HourCategory.Holiday,    8m),
		("8", Hol, HourCategory.Holiday,    10m),
	};

	/// <summary>Public holidays of 2026, as sent by the manager.</summary>
	public static readonly (DateOnly Date, string Name)[] Holidays =
	{
		(new DateOnly(2026, 1, 1),   "Πρωτοχρονιά"),
		(new DateOnly(2026, 1, 6),   "Θεοφάνεια"),
		(new DateOnly(2026, 2, 23),  "Καθαρά Δευτέρα"),
		(new DateOnly(2026, 3, 25),  "25η Μαρτίου"),
		(new DateOnly(2026, 4, 10),  "Μεγάλη Παρασκευή"),
		(new DateOnly(2026, 4, 12),  "Κυριακή του Πάσχα"),
		(new DateOnly(2026, 4, 13),  "Δευτέρα του Πάσχα"),
		(new DateOnly(2026, 5, 1),   "Εργατική Πρωτομαγιά"),
		(new DateOnly(2026, 6, 1),   "Αγίου Πνεύματος"),
		(new DateOnly(2026, 8, 15),  "Κοίμηση της Θεοτόκου"),
		(new DateOnly(2026, 10, 28), "28η Οκτωβρίου"),
		(new DateOnly(2026, 12, 25), "Χριστούγεννα"),
		(new DateOnly(2026, 12, 26), "2η ημέρα των Χριστουγέννων"),
	};

	/// <summary>Monthly paid-hours limits as confirmed by the manager.</summary>
	public static readonly (decimal ToComplete, decimal Simple, decimal Night, decimal Sunday, decimal PerHoliday, decimal ToCompletePlusHoliday) HourLimits =
		(120m, 20m, 30m, 30m, 10m, 138m);

	/// <summary>Employment Types values (Code, Name), normalized from the source.</summary>
	public static readonly (string Code, string Name)[] EmploymentTypes =
	{
		("Μ",   "Μόνιμος"),
		("Α.Χ", "ΙΔΑΧ"),
		("Ο.Χ", "ΙΔΟΧ"),
	};

	/// <summary>Work Positions values, de-duplicated from the source.</summary>
	public static readonly string[] WorkPositions =
	{
		"ΟΔΗΓΟΣ", "ΣΥΝΤΟΝΙΣΜΟΥ", "ΓΡΑΦΕΙΑ", "ΧΕΙΡΙΣΤΗΣ", "ΣΥΝΕΡΓΕΙΟ", "ΕΡΓΑΤΗΣ",
		"ΠΥΛΗ", "Γ. ΚΙΝΗΣΗΣ", "ΧΕΙΡ. JCB", "ΝΥΚΤΟΦΥΛΑΚΑΣ", "ΗΛΕΚΤΡΟΛΟΓΟΣ",
		"ΠΛΥΝΤΗΡΙΟ", "ΒΟΗΘΟΣ", "ΕΠΟΠΤΗΣ", "ΔΙΕΥΘΥΝΤΗΣ", "ΠΡΟΪΣΤΑΜΕΝΟΣ",
		"ΚΑΘΑΡΙΣΤΡΙΑ", "ΔΙΟΙΚΗΤΙΚΟΥ", "ΤΟΠΙΚΟΙ",
	};
}