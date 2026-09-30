using System.Globalization;
using Vardiologio.Application.Reports;

namespace Vardiologio.Infrastructure.Reports;

/// <summary>
/// Fixed texts and the emblem of the attendance report, as on the organisation's
/// "ΠΙΝΑΚΑΣ ΠΑΡΟΥΣΙΩΝ" sheet. Kept in one place so the Excel and the PDF renderer
/// always print the same wording; change them here if a signatory changes.
/// </summary>
internal static class AttendanceSheetTexts
{
	/// <summary>Letterhead under the emblem (top left). The first four lines are printed bold.</summary>
	public static readonly string[] Letterhead =
	{
		"ΕΛΛΗΝΙΚΗ ΔΗΜΟΚΡΑΤΙΑ",
		"ΠΕΡΙΦΕΡΕΙΑ ΑΤΤΙΚΗΣ",
		"ΕΙΔΙΚΟΣ ΔΙΑΒΑΘΜΙΔΙΚΟΣ ΣΥΝΔΕΣΜΟΣ",
		"Δ/ΝΣΗ: ΣΤΑΘΜΩΝ ΜΕΤΑΦΟΡΤΩΣΗΣ ΑΠΟΡΡΙΜΜΑΤΩΝ",
		"ΕΔΡΑ: Άντερσεν 6 και Μωραΐτη 90, 11525 Αθήνα",
		"Πληρ.: Βρύνας Γεώργιος",
		"Τηλ.: 210 4015080, 210 4320581",
		"e-mail: vrinas@edsna.gr"
	};

	/// <summary>How many letterhead lines (from the top) are bold.</summary>
	public const int LetterheadBoldLines = 4;

	/// <summary>Weekday names as printed under the day numbers, indexed by <see cref="DayOfWeek"/>.</summary>
	private static readonly string[] DayNames =
		{ "ΚΥΡΙΑΚΗ", "ΔΕΥΤΕΡΑ", "ΤΡΙΤΗ", "ΤΕΤΑΡΤΗ", "ΠΕΜΠΤΗ", "ΠΑΡΑΣΚΕΥΗ", "ΣΑΒΒΑΤΟ" };

	/// <summary>Weekday name of a date ("ΣΑΒΒΑΤΟ").</summary>
	public static string DayName(DateOnly date) => DayNames[(int)date.DayOfWeek];

	/// <summary>Worksheet / file label of each report.</summary>
	public static string SheetName(AttendanceSheet sheet) =>
		sheet == AttendanceSheet.Directorate ? "ΔΙΕΥΘΥΝΣΗ" : "ΥΠΑΛΛΗΛΟΙ";

	/// <summary>Line printed above the letterhead (directorate only: the report is addressed to HR).</summary>
	public static string? Addressee(AttendanceSheet sheet) =>
		sheet == AttendanceSheet.Directorate ? "Προς τη Δ/νση Διοικητικών Υπηρεσιών/Τμήμα Προσωπικού" : null;

	/// <summary>Centre title lines (without the period line).</summary>
	public static string[] Titles(AttendanceSheet sheet) => sheet == AttendanceSheet.Directorate
		? new[]
		{
			"Δ/ΝΣΗ ΣΤΑΘΜΩΝ ΜΕΤΑΦΟΡΤΩΣΗΣ ΑΠΟΡ/ΤΩΝ ΣΜΑ",
			"ΠΙΝΑΚΑΣ ΠΑΡΟΥΣΙΩΝ ΥΠΑΛΛΗΛΟΥ Δ/ΝΣΗΣ ΣΜΑ ΠΑΠΑΣΤΑΜΑΤΗ ΑΘΑΝΑΣΙΟΥ"
		}
		: new[]
		{
			"ΔΙΕΥΘΥΝΣΗ ΣΤΑΘΜΩΝ ΜΕΤΑΦΟΡΤΩΣΗΣ",
			"ΠΙΝΑΚΑΣ ΠΑΡΟΥΣΙΩΝ ΠΡΟΣΩΠΙΚΟΥ",
			"ΤΜΗΜΑΤΟΣ ΚΕΝΤΡΙΚΩΝ ΣΜΑ"
		};

	/// <summary>Period line under the titles ("Από 1/8/2026 έως 15/8/2026").</summary>
	public static string Period(DateOnly from, DateOnly to) => $"Από {Date(from)} έως {Date(to)}";

	/// <summary>A date as printed on the sheet ("1/8/2026").</summary>
	public static string Date(DateOnly date) => date.ToString("d/M/yyyy", CultureInfo.InvariantCulture);

	/// <summary>
	/// The directorate report uses the formal layout of its own sheet: bold addressee and
	/// signatures, the period dates bold and underlined, and a taller signing space.
	/// The staff report keeps the plain layout.
	/// </summary>
	public static bool IsFormal(AttendanceSheet sheet) => sheet == AttendanceSheet.Directorate;

	/// <summary>Table caption above each page's table ("ΠΙΝΑΚΑΣ 1").</summary>
	public static string TableCaption(int table) => $"ΠΙΝΑΚΑΣ {table}";

	/// <summary>
	/// Signature blocks at the bottom of every page, left to right: the role lines above the
	/// signing space and the name / title lines below it. The staff report has two
	/// (author and acting head of the directorate); the directorate report has one.
	/// </summary>
	public static (string[] Role, string[] Name)[] Signatures(AttendanceSheet sheet) => sheet == AttendanceSheet.Directorate
		? new[]
		{
			(new[] { "Με εντολή Προέδρου", "Ο Γενικός Γραμματέας του Ε.Δ.Σ.Ν.Α." },
			 new[] { "Γεώργιος Αποστολόπουλος" })
		}
		: new[]
		{
			(new[] { "Ο Συντάξας" },
			 new[] { "Βρύνας Γεώργιος", "ΠΕ Διοικητικού - Οικονομικού" }),
			(new[] { "Ο Αν. Προϊστάμενος", "Διεύθυνσης", "Σταθμών Μεταφόρτωσης" },
			 new[] { "Αθανάσιος Παπασταμάτης", "ΠΕ Χημικών Μηχανικών MSc / Α΄" })
		};

	/// <summary>Name of the emblem resource (see LogicalName in Vardiologio.Infrastructure.csproj).</summary>
	private const string EmblemResource = "Vardiologio.Reports.Emblem.png";

	/// <summary>The Hellenic Republic emblem (PNG), read once from the embedded resource.</summary>
	public static byte[] Emblem => _emblem.Value;

	private static readonly Lazy<byte[]> _emblem = new(() =>
	{
		using var stream = typeof(AttendanceSheetTexts).Assembly.GetManifestResourceStream(EmblemResource)
			?? throw new InvalidOperationException($"Embedded resource {EmblemResource} not found.");
		using var ms = new MemoryStream();
		stream.CopyTo(ms);
		return ms.ToArray();
	});
}
