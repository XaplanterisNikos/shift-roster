using System.Globalization;
using Vardiologio.Application.Reports;
using Vardiologio.Application.ReportTexts;

namespace Vardiologio.Infrastructure.Reports;

/// <summary>
/// Texts, colours and formatting of the official shift table, as on the "Σ.Ω." sheet of the
/// organisation's workbook. The letterhead, titles, addressee and signatures are the user's
/// editable texts (<see cref="ReportTextSet"/>); this class only decides how each line is
/// formatted, by its position. Table headers and the legend title stay fixed.
/// </summary>
internal static class ShiftTableTexts
{
	private static readonly string[] MonthsGen =
		{ "ΙΑΝΟΥΑΡΙΟΥ","ΦΕΒΡΟΥΑΡΙΟΥ","ΜΑΡΤΙΟΥ","ΑΠΡΙΛΙΟΥ","ΜΑΪΟΥ","ΙΟΥΝΙΟΥ",
		  "ΙΟΥΛΙΟΥ","ΑΥΓΟΥΣΤΟΥ","ΣΕΠΤΕΜΒΡΙΟΥ","ΟΚΤΩΒΡΙΟΥ","ΝΟΕΜΒΡΙΟΥ","ΔΕΚΕΜΒΡΙΟΥ" };

	/// <summary>Which part of a letterhead line is printed bold.</summary>
	private enum BoldPart { None, All, AfterColon }

	/// <summary>
	/// Bold part of each letterhead line, by position (as on the form): the organisation and
	/// directorate names are bold, and on the e-mail line only the address after "E-mail: ".
	/// </summary>
	private static readonly BoldPart[] LetterheadBold =
	{
		BoldPart.None, BoldPart.None, BoldPart.All, BoldPart.All, BoldPart.None,
		BoldPart.None, BoldPart.All, BoldPart.None, BoldPart.AfterColon
	};

	/// <summary>Letterhead lines under the emblem, each split into its plain and its bold part.</summary>
	public static (string Plain, string Bold)[] Letterhead(ReportTextSet texts) =>
		texts.Lines(ReportTextBlocks.Letterhead).Select(l =>
		{
			var part = l.Line < LetterheadBold.Length ? LetterheadBold[l.Line] : BoldPart.None;
			if (part == BoldPart.All) return ("", l.Text);
			if (part == BoldPart.None) return (l.Text, "");

			// "E-mail: iona@edsna.gr" -> plain "E-mail: ", bold "iona@edsna.gr"; no colon -> all plain.
			var colon = l.Text.IndexOf(':');
			if (colon < 0) return (l.Text, "");
			var start = colon + 1;
			while (start < l.Text.Length && l.Text[start] == ' ') start++;   // keep the space with the label
			return (l.Text[..start], l.Text[start..]);
		}).ToArray();

	/// <summary>
	/// Centre titles followed by the month line, which is always added. The first title of the
	/// form is the largest and the third is not bold (as on the form).
	/// </summary>
	public static (string Text, bool Bold, bool Large)[] Titles(ReportTextSet texts, int year, int month) =>
		texts.Lines(ReportTextBlocks.Titles)
			.Select(l => (l.Text, Bold: l.Line != 2, Large: l.Line == 0))
			.Append(($"ΜΗΝΟΣ {MonthsGen[month - 1]} {year}", true, false))
			.ToArray();

	/// <summary>Addressee lines on the right of the titles (bold).</summary>
	public static string[] Addressee(ReportTextSet texts) => texts.Texts(ReportTextBlocks.Addressee);

	// Table headers.
	public const string HeadName = "ΟΝΟΜΑΤΕΠΩΝΥΜΟ", HeadSpeciality = "ΕΙΔΙΚΟΤΗΤΑ", HeadShifts = "ΒΑΡΔΙΑ ΕΡΓΑΣΙΑΣ";
	public const string HeadToComplete = "ΕΡΓΑΣΙΑ ΠΡΟΣ ΣΥΜΠΛΗΡΩΣΗ", HeadDaily = "ΗΜΕΡΗΣΙΑ", HeadSundayHoliday = "ΚΥΡΙΑΚΩΝ ΕΞΑΙΡΕΣΙΜΩΝ";

	/// <summary>Title of the legend under the table.</summary>
	public const string LegendTitle = "ΒΑΡΔΙΕΣ – ΩΡΑΡΙΟ ΕΡΓΑΣΙΑΣ";

	/// <summary>Signature blocks, left to right: role above the signing space, name / title lines below.</summary>
	public static (string Role, string[] Name)[] Signatures(ReportTextSet texts) =>
		texts.Signatures().Select(s => (string.Join(" ", s.Role), s.Name)).ToArray();

	/// <summary>Day header as on the form ("1/8").</summary>
	public static string DayHeader(int year, int month, int day) => $"{day}/{month}";

	/// <summary>Name as on the form: upper case, no accents ("ΑΛΙΜΑΝΤΗΡΗΣ ΓΕΩΡΓΙΟΣ").</summary>
	public static string Name(ShiftTableRow r) => GreekText.Upper($"{r.LastName.Trim()} {r.FirstName.Trim()}");

	/// <summary>A count, blank when zero (keeps the table readable).</summary>
	public static string Count(int n) => n == 0 ? "" : n.ToString(CultureInfo.InvariantCulture);

	/// <summary>Hours, blank when zero ("32,5").</summary>
	public static string Hours(decimal h) => h == 0 ? "" : h.ToString("0.##", CultureInfo.GetCultureInfo("el-GR"));
}
