using System.Globalization;
using Vardiologio.Application.Reports;
using Vardiologio.Application.ReportTexts;

namespace Vardiologio.Infrastructure.Reports;

/// <summary>
/// Texts and the emblem of the attendance report, as on the organisation's "ΠΙΝΑΚΑΣ ΠΑΡΟΥΣΙΩΝ"
/// sheet. The letterhead, addressee, titles and signatures are the user's editable texts
/// (<see cref="ReportTextSet"/>); read through this class so the Excel and the PDF renderer
/// always print the same wording with the same formatting.
/// </summary>
internal static class AttendanceSheetTexts
{
	/// <summary>How many letterhead lines (from the top of the form) are bold.</summary>
	private const int LetterheadBoldLines = 4;

	/// <summary>
	/// Letterhead lines under the emblem (top left) and whether each is bold. Also used by the
	/// overtime certificate, which has the same letterhead layout (with its own texts).
	/// </summary>
	public static (string Text, bool Bold)[] Letterhead(ReportTextSet texts) =>
		texts.Lines(ReportTextBlocks.Letterhead).Select(l => (l.Text, l.Line < LetterheadBoldLines)).ToArray();

	/// <summary>Weekday names as printed under the day numbers, indexed by <see cref="DayOfWeek"/>.</summary>
	private static readonly string[] DayNames =
		{ "ΚΥΡΙΑΚΗ", "ΔΕΥΤΕΡΑ", "ΤΡΙΤΗ", "ΤΕΤΑΡΤΗ", "ΠΕΜΠΤΗ", "ΠΑΡΑΣΚΕΥΗ", "ΣΑΒΒΑΤΟ" };

	/// <summary>Weekday name of a date ("ΣΑΒΒΑΤΟ").</summary>
	public static string DayName(DateOnly date) => DayNames[(int)date.DayOfWeek];

	/// <summary>Worksheet / file label of each report.</summary>
	public static string SheetName(AttendanceSheet sheet) => sheet switch
	{
		AttendanceSheet.Directorate => "ΔΙΕΥΘΥΝΣΗ",
		AttendanceSheet.FixedTerm => "ΙΔΟΧ",
		_ => "ΙΔΑΧ - ΜΟΝΙΜΟΙ"   // a worksheet name cannot contain "/"
	};

	/// <summary>
	/// Line printed above the letterhead, or null when the form has none (only the directorate
	/// report has one: it is addressed to HR).
	/// </summary>
	public static string? Addressee(ReportTextSet texts) => texts.Texts(ReportTextBlocks.Addressee).FirstOrDefault();

	/// <summary>Centre title lines (without the period line).</summary>
	public static string[] Titles(ReportTextSet texts) => texts.Texts(ReportTextBlocks.Titles);

	/// <summary>Period line under the titles ("Από 1/8/2026 έως 15/8/2026").</summary>
	public static string Period(DateOnly from, DateOnly to) => $"Από {Date(from)} έως {Date(to)}";

	/// <summary>A date as printed on the sheet ("1/8/2026").</summary>
	public static string Date(DateOnly date) => date.ToString("d/M/yyyy", CultureInfo.InvariantCulture);

	/// <summary>
	/// The directorate report uses the formal layout of its own sheet: bold addressee and
	/// signatures, the period dates bold and underlined, and a taller signing space.
	/// The two staff reports keep the plain layout.
	/// </summary>
	public static bool IsFormal(AttendanceSheet sheet) => sheet == AttendanceSheet.Directorate;

	/// <summary>Table caption above each table ("ΠΙΝΑΚΑΣ 1").</summary>
	public static string TableCaption(int table) => $"ΠΙΝΑΚΑΣ {table}";

	/// <summary>
	/// Signature blocks under the last table of each fortnight, left to right: the role lines
	/// above the signing space and the name / title lines below it. The staff reports have two
	/// (author and acting head of the directorate); the directorate report has one.
	/// </summary>
	public static (string[] Role, string[] Name)[] Signatures(ReportTextSet texts) => texts.Signatures();

	/// <summary>File name of the emblem, next to the executable (see the copy rule in Vardiologio.App.csproj).</summary>
	public const string EmblemFileName = "emblem.png";

	/// <summary>
	/// The emblem printed above the letterhead (PNG), read once from "emblem.png" next to the
	/// executable; null when the file is absent. The image is kept out of the repository
	/// (git-ignored), so a fresh clone prints the forms without it.
	/// </summary>
	public static byte[]? Emblem => _emblem.Value;

	private static readonly Lazy<byte[]?> _emblem = new(() =>
	{
		var path = Path.Combine(AppContext.BaseDirectory, EmblemFileName);
		return File.Exists(path) ? File.ReadAllBytes(path) : null;
	});
}
