namespace Vardiologio.Application.Reports;

/// <summary>
/// Data for a single employee's monthly analytical sheet
/// ("ΑΝΑΛΥΤΙΚΟΣ ΗΜΕΡΗΣΙΟΣ ΠΙΝΑΚΑΣ ΠΑΡΟΥΣΙΩΝ ΚΑΙ ΩΡΑΡΙΩΝ").
/// Only the currently computable fields are populated; the rest are shown blank
/// in the view until the calculation rules / manual inputs are defined.
/// </summary>
public class IndividualReportModel
{
	public int Year { get; set; }
	public int Month { get; set; }

	public string EmployeeName { get; set; } = "";

	/// <summary>ΕΙΔΟΣ ΕΡΓΑΣΙΑΣ (e.g. "ΙΔΑΧ"). Empty slot for now; will be filled from a future Employee field.</summary>
	public string WorkType { get; set; } = "";

	public List<IndividualDayRow> Days { get; set; } = new();

	// "Εργασία προς Συμπλήρωση" totals, split per fortnight (Α' = days 1-15, Β' = days 16-end).
	public decimal WeekdayFirstHalf { get; set; }   // ΕΒΔΟΜΑΔΙΑΙΑ Α'
	public decimal WeekdaySecondHalf { get; set; }  // ΕΒΔΟΜΑΔΙΑΙΑ Β'
	public decimal WeekdayTotal => WeekdayFirstHalf + WeekdaySecondHalf;

	public decimal SundayFirstHalf { get; set; }    // ΚΥΡΙΑΚΩΝ Α'
	public decimal SundaySecondHalf { get; set; }   // ΚΥΡΙΑΚΩΝ Β'
	public decimal SundayTotal => SundayFirstHalf + SundaySecondHalf;
}

/// <summary>One calendar day of the analytical sheet.</summary>
public class IndividualDayRow
{
	public DateOnly Date { get; set; }
	public int Index { get; set; }                    // Α/Α (plain sequential number)

	/// <summary>Shift start "HH:mm" when the day is a work shift; otherwise null.</summary>
	public string? ShiftStart { get; set; }
	public string? ShiftEnd { get; set; }

	/// <summary>Status code (e.g. "Κ", "ΑΡΓΙΑ") when the day is a non-working status; otherwise null.</summary>
	public string? StatusCode { get; set; }

	/// <summary>ΣΥΜΠΛΗΡ. ΕΡΓΑΣΙΑ — weekday extra hours (Mon-Sat).</summary>
	public decimal? SupplementWeekday { get; set; }

	/// <summary>ΣΥΜΠΛΗΡ. ΕΡΓΑΣΙΑ — Sunday extra hours.</summary>
	public decimal? SupplementSunday { get; set; }

	/// <summary>ΠΑΡΟΥΣΙΕΣ — "√" for a worked shift, the status code otherwise, empty when nothing entered.</summary>
	public string Presence { get; set; } = "";

	public bool IsSunday => Date.DayOfWeek == DayOfWeek.Sunday;
}