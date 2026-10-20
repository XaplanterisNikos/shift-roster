using System.Globalization;
using Vardiologio.Application.Hours;
using Vardiologio.Application.Reports;
using Vardiologio.Application.ReportTexts;
using Vardiologio.Domain.Enums;

namespace Vardiologio.Infrastructure.Reports;

/// <summary>
/// The official form of the individual monthly sheet, as on the organisation's "ΚΑΡΤΑ" sheets:
/// emblem + letterhead, the date label top right, the title, the day table with the
/// organisation's headers, ONE overtime row under it (aligned to the day columns) and the two
/// signatures (the employee left, his supervisor right). Read by both the Excel and the PDF
/// renderer, so they always print the same thing. The on-screen preview does not use this
/// class (it keeps <see cref="IndividualSheetLayout"/>).
/// Columns are 1-based positions in the 23 columns of the day table.
/// </summary>
internal static class IndividualSheetTexts
{
	/// <summary>Number of columns of the sheet (same as the day table).</summary>
	public static int ColumnCount => IndividualSheetLayout.ColumnCount;

	/// <summary>
	/// Relative widths of the 23 columns, taken from the organisation's sheet, so the
	/// proportions (wide name column, narrow Α/Α…) are the same on paper.
	/// </summary>
	public static readonly double[] Widths =
	{
		12.6, 9.3, 39.6, 13.8, 14.1, 15.8, 20.6, 13.8, 11.4, 16.3, 13.3, 11.7,
		11.0, 13.7, 14.8, 9.3, 11.1, 15.6, 14.0, 11.1, 9.3, 9.1, 11.7
	};

	/// <summary>
	/// Headers of the day table with the organisation's wording. Same columns and spans as
	/// <see cref="IndividualSheetLayout.Headers"/>, so its cell values fit under them.
	/// </summary>
	public static readonly IndividualSheetLayout.HeaderGroup[] Headers =
	{
		new("ΗΜΕΡΟΜΗΝ.", []),
		new("Α/Α", []),
		new("ΟΝΟΜΑΤΕΠΩΝΥΜΟ ΕΡΓΑΖΟΜΕΝΟΥ", []),
		new("ΕΙΔΟΣ ΕΡΓΑΣΙΑΣ", []),
		new("ΩΡΑΡΙΟ ΒΑΡΔΙΑΣ", ["ΕΝΑΡΞΗ", "ΛΗΞΗ"]),
		new("ΩΡΑΡΙΟ ΥΠΕΡΩΡΙΑΣ", ["ΕΝΑΡΞΗ", "ΛΗΞΗ"]),
		new("ΣΥΜΠΛΗΡ. ΕΡΓΑΣΙΑ", ["ΣΥΝ. ΩΡ. ΗΜΕΡ.", "ΣΥΝ. ΩΡ. ΚΥΡΙΑΚΗΣ"]),
		new("ΩΡΑΡΙΑ", []),
		new("ΠΑΡΟΥΣΙΕΣ", []),
		new("ΣΤΑΣΕΙΣ ΕΡΓΑΣΙΑΣ", []),
		new("ΗΜΕΡΗΣΙΟ", ["ΩΡΑΡΙΟ ΕΡΓΑΣΙΑΣ", "ΕΝΑΡΞΗ", "ΛΗΞΗ"]),
		new("ΝΥΚΤΕΡΙΝΟ", ["ΩΡΑΡΙΟ ΕΡΓΑΣΙΑΣ", "ΕΝΑΡΞΗ", "ΛΗΞΗ"]),
		new("ΗΜΕΡΗΣΙΟ", ["ΕΝΑΡΞΗ", "ΛΗΞΗ"]),
		new("ΝΥΚΤΕΡΙΝΟ", ["ΕΝΑΡΞΗ", "ΛΗΞΗ"])
	};

	/// <summary>Band over the schedule columns, above the headers: (text, first column, last column).</summary>
	public static readonly (string Text, int From, int To)[] ScheduleBands =
	{
		("ΩΡΑΡΙΟ ΔΕΥΤΕΡΑ ΕΩΣ ΣΑΒΒΑΤΟ", 14, 19),
		("ΩΡΑΡΙΟ ΚΥΡΙΑΚΩΝ", 20, 23)
	};

	/// <summary>Index (0-based) of the name column: the only left-aligned one.</summary>
	public const int NameColumn = IndividualSheetLayout.NameColumn;

	/// <summary>The cells of one day, one text per column (the same values as the preview).</summary>
	public static string[] Cells(IndividualReportModel m, IndividualDayRow day) => IndividualSheetLayout.Cells(m, day);

	// ---- Header ----

	/// <summary>Letterhead lines under the emblem (all plain, as on the sheet).</summary>
	public static string[] Letterhead(ReportTextSet texts) => texts.Texts(ReportTextBlocks.Letterhead);

	/// <summary>Letterhead row (0-based) the date label sits on, as on the sheet (the directorate line).</summary>
	public const int DateRow = 3;

	/// <summary>Columns of the date label and of the date (top right).</summary>
	public const int DateLabelFrom = 16, DateLabelTo = 17, DateFrom = 18, DateTo = 20;

	/// <summary>The label before the date ("Σ.Μ.Α:"); "" when it was left empty.</summary>
	public static string DateLabel(ReportTextSet texts) => texts.Line(ReportTextBlocks.DateLabel, 0);

	/// <summary>The date as on the sheet ("01/09/26").</summary>
	public static string Date(DateOnly date) => date.ToString("dd/MM/yy", CultureInfo.InvariantCulture);

	/// <summary>The title lines above the table (empty ones skipped).</summary>
	public static string[] Titles(ReportTextSet texts) => texts.Texts(ReportTextBlocks.Titles);

	// ---- Overtime row under the days ----

	/// <summary>
	/// One header of the overtime table: its text (editable), its columns and, for a group, its
	/// sub-columns. A header without sub-columns is two rows high; <paramref name="Caption"/> is
	/// the first one, three rows high, with no value under it.
	/// </summary>
	public sealed record SummaryGroup(string Text, int From, int To, SummaryColumn[] Columns, bool Caption = false);

	/// <summary>A sub-column of the overtime table: header ("" = none) and value, over its columns.</summary>
	public sealed record SummaryColumn(int From, int To, string Header, string Value);

	/// <summary>
	/// The overtime table under the days, as on the sheet. The group texts are the user's
	/// (<see cref="ReportTextBlocks.SummaryGroups"/>, by position); the sub-headers are fixed and
	/// the values are those of the preview's summary (declared before the limits, paid after them).
	/// </summary>
	public static SummaryGroup[] Summary(IndividualReportModel m)
	{
		string T(int line) => m.Texts.Line(ReportTextBlocks.SummaryGroups, line);
		decimal Declared(HourCategory c) => m.Hours.FirstOrDefault(h => h.Category == c)?.Declared ?? 0;
		decimal Paid(HourCategory c) => m.Hours.FirstOrDefault(h => h.Category == c)?.Paid ?? 0;
		string Clock(decimal h) => IndividualSheetLayout.Clock(h, zero: true);
		string Number(decimal h) => IndividualSheetLayout.Number(h, zero: true);

		return
		[
			new(T(0), 1, 1, [], Caption: true),
			new(T(1), 2, 2, [new(2, 2, "", "1")]),
			new(T(2), 3, 3, [new(3, 3, "", m.EmployeeName)]),
			new(T(3), 4, 4, [new(4, 4, "", Branch(m.Speciality))]),
			new(T(4), 5, 6,
			[
				new(5, 5, "ΗΜΕΡ.", Clock(Declared(HourCategory.Simple))),
				new(6, 6, "ΝΥΚΤ.", Clock(Declared(HourCategory.Night)))
			]),
			new(T(5), 7, 8,
			[
				new(7, 7, "ΗΜΕΡ.", Clock(Declared(HourCategory.Sunday))),
				new(8, 8, "ΝΥΚΤ.", Clock(0))   // no pay category feeds this column yet
			]),
			new(T(6), 9, 11,
			[
				new(9, 9, "Α΄", Number(m.WeekdayFirstHalf)),
				new(10, 10, "Β΄", Number(m.WeekdaySecondHalf)),
				new(11, 11, "ΣΥΝΟΛΟ ΜΗΝΟΣ", Number(m.WeekdayTotal))
			]),
			new(T(7), 12, 14,
			[
				new(12, 12, "Α΄", Number(m.SundayFirstHalf)),
				new(13, 13, "Β΄", Number(m.SundaySecondHalf)),
				new(14, 14, "ΣΥΝΟΛΟ ΜΗΝΟΣ", Number(m.SundayTotal))
			]),
			new(T(8), 15, 17,
			[
				new(15, 15, "Απογευματινή έως 22:00", Clock(Paid(HourCategory.Simple))),
				new(16, 17, "Νυχτερινή 22:00 - 06:00", Clock(Paid(HourCategory.Night)))
			]),
			new(T(9), 18, 19,
			[
				new(18, 18, "Ημερήσιες 06:00 - 22:00", Clock(Paid(HourCategory.Sunday))),
				new(19, 19, "Νυχτερινή 22:00 - 06:00", Clock(0))
			]),
			new(T(10), 20, 23,
			[
				new(20, 21, "Νυχτερινή εργασίμων ημερών", Number(Paid(HourCategory.ToComplete))),
				new(22, 23, "Κυριακών και εξαιρέσιμων ημερών", Number(Paid(HourCategory.Holiday)))
			])
		];
	}

	/// <summary>"κλάδος" of the summary: the first word of the speciality ("ΥΕ Εργατών" → "ΥΕ").</summary>
	public static string Branch(string speciality) =>
		speciality.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";

	// ---- Signatures ----

	/// <summary>A signature block: role lines, then (after the signing space) name and title.</summary>
	public sealed record Signature(string[] Role, string Name, string Title);

	/// <summary>Columns of the two signature blocks (left: the employee; right: his supervisor).</summary>
	public static readonly (int From, int To)[] SignatureColumns = { (3, 6), (14, 18) };

	/// <summary>
	/// The two signatures: the employee (role from the form, "Ο"/"Η" added, his name and
	/// speciality) and the supervisor (empty when none is set or picked).
	/// </summary>
	public static Signature[] Signatures(IndividualReportModel m)
	{
		var role = m.Texts.Line(ReportTextBlocks.SignatureRole(1), 0).Trim();
		var article = m.IsFemale ? "Η" : "Ο";
		var employee = new Signature(
			role.Length == 0 ? [] : [$"{article} {role}"],
			$"{m.FirstName} {m.LastName}".Trim(),
			m.Speciality);

		var supervisor = m.Supervisor is { } s
			? new Signature(s.Role, s.FullName, s.Title)
			: new Signature([], "", "");

		return [employee, supervisor];
	}
}
