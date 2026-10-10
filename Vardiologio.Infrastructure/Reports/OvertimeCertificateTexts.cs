using System.Globalization;
using Vardiologio.Application.Reports;
using Vardiologio.Application.ReportTexts;

namespace Vardiologio.Infrastructure.Reports;

/// <summary>
/// Texts of the individual overtime certificate. The titles, the introduction (with the
/// decision numbers), the declaration and the certification are the user's editable texts
/// (<see cref="ReportTextSet"/>); the month and the period dates are always added here.
/// The table headers stay fixed. The emblem and the letterhead layout are shared with the
/// attendance report (<see cref="AttendanceSheetTexts"/>).
/// </summary>
internal static class OvertimeCertificateTexts
{
	private static readonly string[] MonthsGen =
		{ "ΙΑΝΟΥΑΡΙΟΥ","ΦΕΒΡΟΥΑΡΙΟΥ","ΜΑΡΤΙΟΥ","ΑΠΡΙΛΙΟΥ","ΜΑΪΟΥ","ΙΟΥΝΙΟΥ",
		  "ΙΟΥΛΙΟΥ","ΑΥΓΟΥΣΤΟΥ","ΣΕΠΤΕΜΒΡΙΟΥ","ΟΚΤΩΒΡΙΟΥ","ΝΟΕΜΒΡΙΟΥ","ΔΕΚΕΜΒΡΙΟΥ" };

	/// <summary>Letterhead row (0-based) that each hand-filled label sits on, by the label's position.</summary>
	private static readonly int[] HandFilledRows = { 0, 2, 4 };

	/// <summary>How many letterhead rows the hand-filled labels need (the renderers keep at least this many).</summary>
	public const int HandFilledRowCount = 5;

	/// <summary>
	/// Labels on the right of the letterhead, filled in by hand ("Αθήνα :", "Αρ. Πρωτ. :",
	/// "Προς :"), each with the letterhead row it sits on. The rows are fixed, so the labels
	/// do not move when a letterhead line is left empty.
	/// </summary>
	public static (int Row, string Text)[] HandFilled(ReportTextSet texts) =>
		texts.Lines(ReportTextBlocks.HandFilled)
			.Where(l => l.Line < HandFilledRows.Length)
			.Select(l => (HandFilledRows[l.Line], l.Text))
			.ToArray();

	/// <summary>First title line: the editable text, then the month ("… ΜΗΝΟΣ ΑΥΓΟΥΣΤΟΥ 2026").</summary>
	public static string Title(ReportTextSet texts, int year, int month)
	{
		var text = texts.Line(ReportTextBlocks.Titles, 0);
		var monthPart = $"ΜΗΝΟΣ {MonthsGen[month - 1]} {year}";
		return text.Length == 0 ? monthPart : $"{text}  {monthPart}";
	}

	/// <summary>Second title line ("" when it was left empty).</summary>
	public static string Subtitle(ReportTextSet texts) => texts.Line(ReportTextBlocks.Titles, 1);

	/// <summary>
	/// Introduction under the titles (centred lines). The period is always added to the end of
	/// the last line: "… κατά το χρονικό διάστημα από 01/08/2026 έως 31/08/2026".
	/// </summary>
	public static string[] Intro(ReportTextSet texts, DateOnly from, DateOnly to)
	{
		var lines = texts.Texts(ReportTextBlocks.Intro);
		var period = $"από {Date(from)} έως {Date(to)}";
		if (lines.Length == 0) return new[] { period };

		lines[^1] = $"{lines[^1]} {period}";
		return lines;
	}

	// Table headers: four single columns, then three groups of two sub-columns.
	public const string HeadIndex = "A/A", HeadLastName = "Επίθετο", HeadFirstName = "Όνομα", HeadSpeciality = "κλάδος/ειδικότητα";

	/// <summary>Group headers, each over two sub-columns.</summary>
	public static readonly string[] Groups =
	{
		"Υπερωριακή Εργασία σε ώρες",
		"Υπερωρική Εργασία\nΚυριακές και εξαιρέσιμες",
		"Εργασία προς Συμπλήρωση"
	};

	/// <summary>Sub-column headers, left to right (two per group).</summary>
	public static readonly string[] SubHeads =
	{
		"Απογευματινή  Εως  22:00'",
		"Νυχτερινή  22:00 - 06:00",
		"Ημερήσιες\n6:00 - 22:00",
		"Νυχτερινή  22:00 - 06:00",
		"Νυχτερινή εργασίμων ημερών",
		"Κυριακών και εξαιρέσιμων ημερών"
	};

	/// <summary>Heading of the declaration block.</summary>
	public static string DeclarationTitle(ReportTextSet texts) => texts.Line(ReportTextBlocks.DeclarationTitle, 0);

	/// <summary>The numbered declaration items (numbered again from 1 when one is left empty).</summary>
	public static string[] Declarations(ReportTextSet texts) => texts.Texts(ReportTextBlocks.Declarations);

	/// <summary>Certification paragraph under the declaration.</summary>
	public static string Certification(ReportTextSet texts) => texts.Line(ReportTextBlocks.Certification, 0);

	/// <summary>Signature role: "Ο" for men, "Η" for women.</summary>
	public static string SignatureRole(bool isFemale) => isFemale ? "Η Βεβαιών Υπάλληλος" : "Ο Βεβαιών Υπάλληλος";

	/// <summary>Name under the signature ("ΒΡΥΝΑΣ      ΓΕΩΡΓΙΟΣ"), as on the form.</summary>
	public static string SignatureName(OvertimeCertificateModel m) => $"{GreekText.Upper(m.LastName)}      {GreekText.Upper(m.FirstName)}";

	/// <summary>A date as printed in the introduction ("01/08/2026").</summary>
	public static string Date(DateOnly date) => date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

	/// <summary>Hours as a clock value ("10:00", "6:30", "0:00") for the four overtime columns.</summary>
	public static string Clock(decimal hours)
	{
		var minutes = (int)Math.Round(hours * 60, MidpointRounding.AwayFromZero);
		return $"{minutes / 60}:{minutes % 60:00}";
	}

	/// <summary>Hours as a number ("33", "0", "6,5") for the two "προς συμπλήρωση" columns.</summary>
	public static string Number(decimal hours) => hours.ToString("0.##", CultureInfo.GetCultureInfo("el-GR"));

	/// <summary>The six values in column order, each already formatted as printed.</summary>
	public static string[] Values(OvertimeCertificateModel m) => new[]
	{
		Clock(m.AfternoonOvertime),
		Clock(m.NightOvertime),
		Clock(m.SundayDayOvertime),
		Clock(m.SundayNightOvertime),
		Number(m.WeekdayNightToComplete),
		Number(m.SundayHolidayToComplete)
	};
}
