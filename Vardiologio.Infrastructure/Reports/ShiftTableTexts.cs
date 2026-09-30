using System.Globalization;
using Vardiologio.Application.Reports;

namespace Vardiologio.Infrastructure.Reports;

/// <summary>
/// Fixed texts, colours and formatting of the official shift table, copied from the "Σ.Ω." sheet
/// of the organisation's workbook (its letterhead differs from the other official forms).
/// </summary>
internal static class ShiftTableTexts
{
	private static readonly string[] MonthsGen =
		{ "ΙΑΝΟΥΑΡΙΟΥ","ΦΕΒΡΟΥΑΡΙΟΥ","ΜΑΡΤΙΟΥ","ΑΠΡΙΛΙΟΥ","ΜΑΪΟΥ","ΙΟΥΝΙΟΥ",
		  "ΙΟΥΛΙΟΥ","ΑΥΓΟΥΣΤΟΥ","ΣΕΠΤΕΜΒΡΙΟΥ","ΟΚΤΩΒΡΙΟΥ","ΝΟΕΜΒΡΙΟΥ","ΔΕΚΕΜΒΡΙΟΥ" };

	/// <summary>
	/// Letterhead under the emblem (as on the form). Bold is the part of the line printed bold:
	/// the whole line, nothing, or — for the e-mail — just the address.
	/// </summary>
	public static readonly (string Text, string Bold)[] Letterhead =
	{
		("ΕΛΛΗΝΙΚΗ ΔΗΜΟΚΡΑΤΙΑ", ""),
		("ΠΕΡΙΦΕΡΕΙΑ ΑΤΤΙΚΗΣ", ""),
		("ΕΙΔΙΚΟΣ ΔΙΑΒΑΘΜΙΔΙΚΟΣ ΣΥΝΔΕΣΜΟΣ", "ΕΙΔΙΚΟΣ ΔΙΑΒΑΘΜΙΔΙΚΟΣ ΣΥΝΔΕΣΜΟΣ"),
		("ΝΟΜΟΥ ΑΤΤΙΚΗΣ", "ΝΟΜΟΥ ΑΤΤΙΚΗΣ"),
		("ΕΔΡΑ: Άντερσεν 6 και Μωραΐτη 90, 115 25,", ""),
		("τηλ.: 213 214 8 478-479", ""),
		("Δ/ΝΣΗ ΣΤΑΘΜΩΝ ΜΕΤΑΦΟΡΤΩΣΗΣ ΑΠΟΡΡΙΜΜΑΤΩΝ", "Δ/ΝΣΗ ΣΤΑΘΜΩΝ ΜΕΤΑΦΟΡΤΩΣΗΣ ΑΠΟΡΡΙΜΜΑΤΩΝ"),
		("Τηλ: 210 43 20 581", ""),
		("E-mail: iona@edsna.gr", "iona@edsna.gr")
	};

	/// <summary>Centre titles: text and whether it is bold (the first line is the largest).</summary>
	public static (string Text, bool Bold)[] Titles(int year, int month) => new[]
	{
		("ΔΙΕΥΘΥΝΣΗ  ΣΤΑΘΜΩΝ ΜΕΤΑΦΟΡΤΩΣΗΣ", true),
		("ΠΙΝΑΚΑΣ   ΒΑΡΔΙΩΝ   ΕΡΓΑΣΙΑΣ  ΠΡΟΣΩΠΙΚΟΥ", true),
		("ΤΜΗΜΑΤΟΣ ΚΕΝΤΡΙΚΩΝ ΣΜΑ", false),
		($"ΜΗΝΟΣ {MonthsGen[month - 1]} {year}", true)
	};

	/// <summary>Addressee on the right of the titles (bold).</summary>
	public static readonly string[] Addressee =
	{
		"ΠΡΟΣ: Αν. Προϊσταμένη Τμ. Λογιστηρίου",
		"κ. Αγιανίδου Δέσποινα"
	};

	// Table headers.
	public const string HeadName = "ΟΝΟΜΑΤΕΠΩΝΥΜΟ", HeadSpeciality = "ΕΙΔΙΚΟΤΗΤΑ", HeadShifts = "ΒΑΡΔΙΑ ΕΡΓΑΣΙΑΣ";
	public const string HeadToComplete = "ΕΡΓΑΣΙΑ ΠΡΟΣ ΣΥΜΠΛΗΡΩΣΗ", HeadDaily = "ΗΜΕΡΗΣΙΑ", HeadSundayHoliday = "ΚΥΡΙΑΚΩΝ ΕΞΑΙΡΕΣΙΜΩΝ";

	/// <summary>Title of the legend under the table.</summary>
	public const string LegendTitle = "ΒΑΡΔΙΕΣ – ΩΡΑΡΙΟ ΕΡΓΑΣΙΑΣ";

	/// <summary>Signature blocks, left to right: role above the signing space, name / title below.</summary>
	public static readonly (string Role, string[] Name)[] Signatures =
	{
		("Ο συντάξας", new[] { "ΒΡΥΝΑΣ ΓΕΩΡΓΙΟΣ", "ΠΕ ΔΙΟΙΚΗΤΙΚΟΥ-ΟΙΚΟΝΟΜΙΚΟΥ" }),
		("Ο  Αν. Προϊστάμενος Δ/νσης Σ.Μ.Α.", new[] { "ΠΑΠΑΣΤΑΜΑΤΗΣ ΑΘΑΝΑΣΙΟΣ", "ΠΕ Χημικών Μηχανικών, MSc / Α'" })
	};

	/// <summary>Weekend / holiday shading: Office "Blue-Gray, Text 2, Lighter 80%" (theme dk2, tint 0.8).</summary>
	public const string ShadeHex = "#D6DCE4";

	/// <summary>Day header as on the form ("1/8").</summary>
	public static string DayHeader(int year, int month, int day) => $"{day}/{month}";

	/// <summary>Name as on the form: upper case, no accents ("ΑΛΙΜΑΝΤΗΡΗΣ ΓΕΩΡΓΙΟΣ").</summary>
	public static string Name(ShiftTableRow r) => GreekText.Upper($"{r.LastName.Trim()} {r.FirstName.Trim()}");

	/// <summary>A count, blank when zero (keeps the table readable).</summary>
	public static string Count(int n) => n == 0 ? "" : n.ToString(CultureInfo.InvariantCulture);

	/// <summary>Hours, blank when zero ("32,5").</summary>
	public static string Hours(decimal h) => h == 0 ? "" : h.ToString("0.##", CultureInfo.GetCultureInfo("el-GR"));
}
