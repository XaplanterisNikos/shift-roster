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

	/// <summary>Shift/status codes: working shifts carry times + a Day/Night segment; statuses are null.</summary>
	public static readonly (string Code, string Description, TimeOnly? Start, TimeOnly? End, ShiftSegment? Segment)[] ShiftCodes =
	{
		("1",  "00:00 – 06:30", new TimeOnly(0, 0),  new TimeOnly(6, 30), ShiftSegment.Night),
		("2",  "00:00 – 06:00", new TimeOnly(0, 0),  new TimeOnly(6, 0),  ShiftSegment.Night),
		("3",  "03:00 – 09:30", new TimeOnly(3, 0),  new TimeOnly(9, 30), ShiftSegment.Day),
		("4",  "04:00 – 10:30", new TimeOnly(4, 0),  new TimeOnly(10, 30),ShiftSegment.Day),
		("5",  "05:00 – 11:30", new TimeOnly(5, 0),  new TimeOnly(11, 30),ShiftSegment.Day),
		("6",  "06:00 – 12:30", new TimeOnly(6, 0),  new TimeOnly(12, 30),ShiftSegment.Day),
		("7",  "06:00 – 14:00", new TimeOnly(6, 0),  new TimeOnly(14, 0), ShiftSegment.Day),
		("8",  "06:00 – 16:00", new TimeOnly(6, 0),  new TimeOnly(16, 0), ShiftSegment.Day),
		("9",  "08:30 – 15:00", new TimeOnly(8, 30), new TimeOnly(15, 0), ShiftSegment.Day),
		("10", "11:30 – 18:00", new TimeOnly(11, 30),new TimeOnly(18, 0), ShiftSegment.Day),
		("11", "15:30 – 22:00", new TimeOnly(15, 30),new TimeOnly(22, 0), ShiftSegment.Day),
		("12", "22:00 – 06:00", new TimeOnly(22, 0), new TimeOnly(6, 0),  ShiftSegment.Night),
		("13", "22:00 – 04:30", new TimeOnly(22, 0), new TimeOnly(4, 30), ShiftSegment.Night),
		("Υ.Δ.", "Ν105", null, null, null),
		("Α",    "ΑΣΘΕΝΕΙΑ", null, null, null),
		("ΑΡ.",  "ΑΡΓΙΑ", null, null, null),
		("Κ",    "ΚΑΝΟΝΙΚΗ ΑΔΕΙΑ", null, null, null),
		("Δ",    "ΑΝΑΤΡΟΦΗΣ ΤΕΚΝΟΥ", null, null, null),
		("Ε.Α",  "ΕΙΔΙΚΗ ΑΔΕΙΑ", null, null, null),
		("Κ.Σ",  "ΚΑΙΡΙΚΩΝ ΣΥΝΘΗΚΩΝ", null, null, null),
		("Ρ",    "ΡΕΠΟ", null, null, null),
		("Σ",    "ΣΥΝΔΙΚΑΛΙΣΤΙΚΗ", null, null, null),
		("Α.Τ",  "ΑΔΕΙΑ ΤΟΚΕΤΟΥ", null, null, null),
	};

	/// <summary>Extra ("supplementary") hours per shift code. Codes not listed have no extra-hours row.</summary>
	public static readonly (string Code, decimal Hours)[] ExtraHours =
	{
		("1",  6m),
		("2",  6m),
		("3",  3m),
		("4",  2m),
		("5",  1m),
		("12", 8m),
		("13", 1.5m),   // value per spec provided by user
	};

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