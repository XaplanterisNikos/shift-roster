using Vardiologio.Application.ReportColors;
using Vardiologio.Application.Hours;
using Vardiologio.Application.ReportTexts;

namespace Vardiologio.Application.Reports;

/// <summary>
/// Data for a single employee's monthly analytical sheet
/// ("ΑΝΑΛΥΤΙΚΟΣ ΗΜΕΡΗΣΙΟΣ ΠΙΝΑΚΑΣ ΠΑΡΟΥΣΙΩΝ ΚΑΙ ΩΡΑΡΙΩΝ").
/// Everything is derived from the roster entries and the shift-code parameters; ΣΤΑΣΕΙΣ ΕΡΓΑΣΙΑΣ
/// stays blank (there is no data for it).
/// </summary>
public class IndividualReportModel
{
	public int Year { get; set; }
	public int Month { get; set; }

	public string EmployeeName { get; set; } = "";

	/// <summary>Surname alone (used for the exported file's name).</summary>
	public string LastName { get; set; } = "";

	/// <summary>ΕΙΔΟΣ ΕΡΓΑΣΙΑΣ: code of the employee's contract (e.g. "Μ", "Α.Χ"); empty when none is set.</summary>
	public string WorkType { get; set; } = "";

	public List<IndividualDayRow> Days { get; set; } = new();

	// "Εργασία προς Συμπλήρωση" totals, split per fortnight (Α' = days 1-15, Β' = days 16-end).
	public decimal WeekdayFirstHalf { get; set; }   // ΕΒΔΟΜΑΔΙΑΙΑ Α'
	public decimal WeekdaySecondHalf { get; set; }  // ΕΒΔΟΜΑΔΙΑΙΑ Β'
	public decimal WeekdayTotal => WeekdayFirstHalf + WeekdaySecondHalf;

	public decimal SundayFirstHalf { get; set; }    // ΚΥΡΙΑΚΩΝ Α'
	public decimal SundaySecondHalf { get; set; }   // ΚΥΡΙΑΚΩΝ Β'
	public decimal SundayTotal => SundayFirstHalf + SundaySecondHalf;

	/// <summary>
	/// The month's hours per pay category: declared by the roster and paid after the limits.
	/// Same calculation as the hours report and the overtime certificate, so the summary of
	/// this sheet always agrees with them.
	/// </summary>
	public IReadOnlyList<CategoryHours> Hours { get; set; } = [];

	/// <summary>The report's colours, as set in Παράμετροι → Αναφορών → Χρώματα (defaults until a service fills it).</summary>
	public ReportColorSet Colors { get; set; } = ReportColorSet.Default;

	// ---- Official print only (Excel / PDF export); the on-screen preview does not use these ----

	/// <summary>First name as stored (the employee signs with "First Last").</summary>
	public string FirstName { get; set; } = "";

	/// <summary>The employee's speciality (e.g. "ΥΕ Εργατών"): under his signature and, by its first word, in the summary.</summary>
	public string Speciality { get; set; } = "";

	/// <summary>"Η" instead of "Ο" before the employee's role on his signature.</summary>
	public bool IsFemale { get; set; }

	/// <summary>The editable texts of the form (Παράμετροι → Αναφορών → Ατομική μηνιαία).</summary>
	public ReportTextSet Texts { get; set; } = ReportTextSet.Empty;

	/// <summary>The employee's own supervisor (Εργαζόμενοι → Προϊστάμενος); null when none is set.</summary>
	public int? SignatoryId { get; set; }

	/// <summary>
	/// Who signs on the right: the employee's supervisor, or the one picked on the page for this
	/// export. Null = the right signature is printed empty.
	/// </summary>
	public IndividualSigner? Supervisor { get; set; }

	/// <summary>Date printed top right after its label ("Σ.Μ.Α: 01/09/26"); by default the 1st of the next month.</summary>
	public DateOnly IssueDate { get; set; }
}

/// <summary>A supervisor's signature block on the individual sheet.</summary>
/// <param name="Role">Role lines above the signing space.</param>
/// <param name="FullName">Name under the signing space.</param>
/// <param name="Title">Speciality / grade under the name ("" = none).</param>
public sealed record IndividualSigner(string[] Role, string FullName, string Title);

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

	/// <summary>ΩΡΑΡΙΑ — the shift code of the day ("3", "13"…) or the status code; null when nothing was entered.</summary>
	public string? Code { get; set; }

	/// <summary>ΩΡΑΡΙΟ ΥΠΕΡΩΡΙΑΣ — start "HH:mm" of the overtime part of the shift (its last hours); null when none.</summary>
	public string? OvertimeStart { get; set; }

	/// <summary>ΩΡΑΡΙΟ ΥΠΕΡΩΡΙΑΣ — end "HH:mm" of the overtime part; null when none.</summary>
	public string? OvertimeEnd { get; set; }

	/// <summary>ΣΥΜΠΛΗΡ. ΕΡΓΑΣΙΑ ΗΜΕΡ. — the day's Προς συμπλήρωση hours (pay category ToComplete).</summary>
	public decimal? SupplementWeekday { get; set; }

	/// <summary>
	/// ΣΥΜΠΛΗΡ. ΕΡΓΑΣΙΑ ΚΥΡΙΑΚΗΣ — the day's Αργίας hours (pay category Holiday), i.e. the
	/// "Κυριακών και εξαιρέσιμων" column of the overtime certificate.
	/// </summary>
	public decimal? SupplementSunday { get; set; }

	/// <summary>Hours of the shift inside the regular schedule (shift length minus overtime); null or 0 when the whole shift is overtime.</summary>
	public decimal? RegularHours { get; set; }

	/// <summary>True for a night shift (the code's Day/Night segment): its times go to the ΝΥΚΤΕΡΙΝΟ columns.</summary>
	public bool IsNight { get; set; }

	/// <summary>True on a Sunday or a holiday: the shift's times go to the ΩΡΑΡΙΟ ΚΥΡΙΑΚΩΝ columns.</summary>
	public bool IsSundayOrHoliday { get; set; }

	/// <summary>ΠΑΡΟΥΣΙΕΣ — "√" for a worked shift, the status code otherwise, empty when nothing entered.</summary>
	public string Presence { get; set; } = "";

	public bool IsSunday => Date.DayOfWeek == DayOfWeek.Sunday;
}