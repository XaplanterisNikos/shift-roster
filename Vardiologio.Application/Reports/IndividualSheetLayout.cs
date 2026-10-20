using System.Globalization;
using Vardiologio.Domain.Enums;

namespace Vardiologio.Application.Reports;

/// <summary>
/// Layout and cell values of the individual analytical sheet ("ΑΝΑΛΥΤΙΚΟΣ ΗΜΕΡΗΣΙΟΣ ΠΙΝΑΚΑΣ
/// ΠΑΡΟΥΣΙΩΝ ΚΑΙ ΩΡΑΡΙΩΝ"), in one place so the on-screen preview, the Excel file and the PDF
/// always show the same columns with the same values.
/// </summary>
public static class IndividualSheetLayout
{
	/// <summary>Sheet title.</summary>
	public const string Title = "ΑΝΑΛΥΤΙΚΟΣ ΗΜΕΡΗΣΙΟΣ ΠΙΝΑΚΑΣ ΠΑΡΟΥΣΙΩΝ ΚΑΙ ΩΡΑΡΙΩΝ";

	private static readonly string[] MonthsGen =
		{ "ΙΑΝΟΥΑΡΙΟΥ","ΦΕΒΡΟΥΑΡΙΟΥ","ΜΑΡΤΙΟΥ","ΑΠΡΙΛΙΟΥ","ΜΑΪΟΥ","ΙΟΥΝΙΟΥ",
		  "ΙΟΥΛΙΟΥ","ΑΥΓΟΥΣΤΟΥ","ΣΕΠΤΕΜΒΡΙΟΥ","ΟΚΤΩΒΡΙΟΥ","ΝΟΕΜΒΡΙΟΥ","ΔΕΚΕΜΒΡΙΟΥ" };

	/// <summary>Line under the title: employee, contract and month ("ΠΑΠΑΔΟΠΟΥΛΟΣ ΓΕΩΡΓΙΟΣ (Μ) — ΜΗΝΟΣ ΑΥΓΟΥΣΤΟΥ 2026").</summary>
	public static string Subtitle(IndividualReportModel m)
	{
		var contract = m.WorkType.Length > 0 ? $" ({m.WorkType})" : "";
		return $"{GreekText.Upper(m.EmployeeName)}{contract} — ΜΗΝΟΣ {MonthsGen[m.Month - 1]} {m.Year}";
	}

	/// <summary>
	/// A header of the day table: a single column two rows high (no <paramref name="Subs"/>),
	/// or a group over its sub-columns.
	/// </summary>
	public sealed record HeaderGroup(string Text, string[] Subs)
	{
		/// <summary>Number of table columns under this header.</summary>
		public int Span => Math.Max(Subs.Length, 1);
	}

	/// <summary>Headers of the day table, left to right (23 columns in all), as on the organisation's sheet.</summary>
	public static readonly HeaderGroup[] Headers =
	{
		new("ΗΜΕΡ.", []),
		new("Α/Α", []),
		new("ΟΝΟΜΑΤΕΠΩΝΥΜΟ", []),
		new("ΕΙΔΟΣ ΕΡΓΑΣΙΑΣ", []),
		new("ΩΡΑΡΙΟ ΒΑΡΔΙΑΣ", ["ΕΝΑΡΞΗ", "ΛΗΞΗ"]),
		new("ΩΡΑΡΙΟ ΥΠΕΡΩΡΙΑΣ", ["ΕΝΑΡΞΗ", "ΛΗΞΗ"]),
		new("ΣΥΜΠΛΗΡ. ΕΡΓΑΣΙΑ", ["ΗΜΕΡ.", "ΚΥΡΙΑΚΗΣ"]),
		new("ΩΡΑΡΙΑ", []),
		new("ΠΑΡΟΥΣΙΕΣ", []),
		new("ΣΤΑΣΕΙΣ", []),
		new("ΗΜΕΡΗΣΙΟ (Δ–Σ)", ["ΩΡ.", "ΕΝ.", "ΛΗ."]),
		new("ΝΥΚΤΕΡΙΝΟ (Δ–Σ)", ["ΩΡ.", "ΕΝ.", "ΛΗ."]),
		new("ΗΜΕΡ. ΚΥΡ.", ["ΕΝ.", "ΛΗ."]),
		new("ΝΥΚΤ. ΚΥΡ.", ["ΕΝ.", "ΛΗ."])
	};

	/// <summary>Total number of columns of the day table.</summary>
	public static int ColumnCount => Headers.Sum(h => h.Span);

	/// <summary>Index (0-based) of the name column: the only left-aligned one.</summary>
	public const int NameColumn = 2;

	/// <summary>
	/// The cells of one day, one text per column of <see cref="Headers"/>. A status day shows
	/// its code in place of the shift start; the shift's times are repeated in the day / night
	/// columns of its kind of day (Monday–Saturday, or Sundays and holidays).
	/// </summary>
	public static string[] Cells(IndividualReportModel m, IndividualDayRow r)
	{
		var isShift = r.StatusCode is null && r.ShiftStart is not null;
		var weekday = isShift && !r.IsSundayOrHoliday;
		var sunday = isShift && r.IsSundayOrHoliday;

		// Start / end go to exactly one of the four schedule groups; the others stay empty.
		string In(bool here, string? value) => here ? value ?? "" : "";

		return
		[
			r.Date.ToString("dd/MM", CultureInfo.InvariantCulture),
			r.Index.ToString(CultureInfo.InvariantCulture),
			m.EmployeeName,
			m.WorkType,
			r.StatusCode ?? r.ShiftStart ?? "",                    // ΩΡΑΡΙΟ ΒΑΡΔΙΑΣ – ΕΝΑΡΞΗ (or the status code)
			isShift ? r.ShiftEnd ?? "" : "",                       // ΩΡΑΡΙΟ ΒΑΡΔΙΑΣ – ΛΗΞΗ
			r.OvertimeStart ?? "",                                 // ΩΡΑΡΙΟ ΥΠΕΡΩΡΙΑΣ – ΕΝΑΡΞΗ
			r.OvertimeEnd ?? "",                                   // ΩΡΑΡΙΟ ΥΠΕΡΩΡΙΑΣ – ΛΗΞΗ
			Number(r.SupplementWeekday),                           // ΣΥΜΠΛΗΡ. ΕΡΓΑΣΙΑ – ΗΜΕΡ.
			Number(r.SupplementSunday),                            // ΣΥΜΠΛΗΡ. ΕΡΓΑΣΙΑ – ΚΥΡΙΑΚΗΣ
			r.Code ?? "",                                          // ΩΡΑΡΙΑ (the code)
			r.Presence,                                            // ΠΑΡΟΥΣΙΕΣ
			"",                                                    // ΣΤΑΣΕΙΣ (no data)
			In(weekday && !r.IsNight, Clock(r.RegularHours)),      // ΗΜΕΡΗΣΙΟ (Δ–Σ)
			In(weekday && !r.IsNight, r.ShiftStart),
			In(weekday && !r.IsNight, r.ShiftEnd),
			In(weekday && r.IsNight, Clock(r.RegularHours)),       // ΝΥΚΤΕΡΙΝΟ (Δ–Σ)
			In(weekday && r.IsNight, r.ShiftStart),
			In(weekday && r.IsNight, r.ShiftEnd),
			In(sunday && !r.IsNight, r.ShiftStart),                // ΗΜΕΡ. ΚΥΡ.
			In(sunday && !r.IsNight, r.ShiftEnd),
			In(sunday && r.IsNight, r.ShiftStart),                 // ΝΥΚΤ. ΚΥΡ.
			In(sunday && r.IsNight, r.ShiftEnd)
		];
	}

	/// <summary>One block of the summary under the day table: a title, what the numbers are, and its values.</summary>
	public sealed record SummaryBlock(string Title, string Note, (string Header, string Value)[] Cells);

	/// <summary>
	/// The summary of the month, in the order of the organisation's sheet. Every block says
	/// where its numbers come from, so a block that is not wanted can be told apart and removed.
	/// </summary>
	public static SummaryBlock[] Summary(IndividualReportModel m)
	{
		decimal Declared(HourCategory c) => m.Hours.FirstOrDefault(h => h.Category == c)?.Declared ?? 0;
		decimal Paid(HourCategory c) => m.Hours.FirstOrDefault(h => h.Category == c)?.Paid ?? 0;

		return
		[
			// On the organisation's sheet this block held the overtime read from the time-clock
			// card. The application has no card data, so it shows the roster's overtime instead.
			new("Δηλωμένες υπερωρίες μήνα",
				"Από τους κωδικούς βάρδιας του μήνα, πριν από τα όρια (στο έντυπο: «από κάρτα»).",
				[
					("Εργάσιμων / Σαββάτου – ημερήσιες", Clock(Declared(HourCategory.Simple), zero: true)),
					("Εργάσιμων / Σαββάτου – νυχτερινές", Clock(Declared(HourCategory.Night), zero: true)),
					("Κυριακών – ημερήσιες", Clock(Declared(HourCategory.Sunday), zero: true)),
					("Κυριακών – νυχτερινές", Clock(0, zero: true))   // no pay category feeds this column yet
				]),
			new("Εργασία προς συμπλήρωση (εβδομαδιαία)",
				"Δηλωμένες ώρες «Προς συμπλήρωση» ανά δεκαπενθήμερο, πριν από τα όρια.",
				[
					("Α΄", Number(m.WeekdayFirstHalf, zero: true)),
					("Β΄", Number(m.WeekdaySecondHalf, zero: true)),
					("Σύνολο μήνα", Number(m.WeekdayTotal, zero: true))
				]),
			new("Εργασία προς συμπλήρωση (Κυριακών – εξαιρέσιμων)",
				"Δηλωμένες ώρες «Αργίας» ανά δεκαπενθήμερο, πριν από τα όρια.",
				[
					("Α΄", Number(m.SundayFirstHalf, zero: true)),
					("Β΄", Number(m.SundaySecondHalf, zero: true)),
					("Σύνολο μήνα", Number(m.SundayTotal, zero: true))
				]),
			new("Πληρωτέες ώρες",
				"Μετά τα μηνιαία όρια· οι ίδιες τιμές με την Ατομική Βεβαίωση.",
				[
					("Υπερωριακή – απογευματινή έως 22:00", Clock(Paid(HourCategory.Simple), zero: true)),
					("Υπερωριακή – νυχτερινή 22:00–06:00", Clock(Paid(HourCategory.Night), zero: true)),
					("Κυριακές / εξαιρέσιμες – ημερήσιες 06:00–22:00", Clock(Paid(HourCategory.Sunday), zero: true)),
					("Κυριακές / εξαιρέσιμες – νυχτερινή 22:00–06:00", Clock(0, zero: true)),
					("Προς συμπλήρωση – νυχτερινή εργασίμων ημερών", Number(Paid(HourCategory.ToComplete), zero: true)),
					("Προς συμπλήρωση – Κυριακών και εξαιρέσιμων", Number(Paid(HourCategory.Holiday), zero: true))
				])
		];
	}

	/// <summary>Hours as a number ("6,5"); blank when null or zero unless <paramref name="zero"/> is set.</summary>
	public static string Number(decimal? hours, bool zero = false) =>
		hours is null || (hours == 0 && !zero) ? "" : hours.Value.ToString("0.##", CultureInfo.GetCultureInfo("el-GR"));

	/// <summary>Hours as a clock value ("6:30"); blank when null or zero unless <paramref name="zero"/> is set.</summary>
	public static string Clock(decimal? hours, bool zero = false)
	{
		if (hours is null || (hours == 0 && !zero)) return "";
		var minutes = (int)Math.Round(hours.Value * 60, MidpointRounding.AwayFromZero);
		return $"{minutes / 60}:{minutes % 60:00}";
	}
}
