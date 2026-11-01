using Vardiologio.Application.ReportColors;
using Vardiologio.Application.ReportTexts;

namespace Vardiologio.Application.Reports;

/// <summary>
/// Data for the monthly "ΠΙΝΑΚΑΣ ΒΑΡΔΙΩΝ ΕΡΓΑΣΙΑΣ ΠΡΟΣΩΠΙΚΟΥ" (official shift table, A3 landscape):
/// one row per active employee with the code of every day, how many times each work-shift code
/// was worked, and the paid "προς συμπλήρωση" hours; plus the legend of all codes. The employees
/// print in three groups (permanent, open-ended, fixed-term), each after its separator row.
/// </summary>
public class ShiftTableModel
{
	/// <summary>Table year.</summary>
	public int Year { get; set; }

	/// <summary>Table month (1–12).</summary>
	public int Month { get; set; }

	/// <summary>Number of days in the month (28–31).</summary>
	public int DaysInMonth => DateTime.DaysInMonth(Year, Month);

	/// <summary>Days of the month (1-based) that are holidays; shaded like weekends.</summary>
	public List<int> HolidayDays { get; set; } = new();

	/// <summary>
	/// Work-shift codes in numeric order: one count column each ("ΒΑΡΔΙΑ ΕΡΓΑΣΙΑΣ") and the
	/// first part of the legend (code + times, e.g. "00:00 – 06:30").
	/// </summary>
	public List<(string Code, string Description)> WorkCodes { get; set; } = new();

	/// <summary>Status codes (leave, rest, holiday…): second part of the legend.</summary>
	public List<(string Code, string Description)> StatusCodes { get; set; } = new();

	/// <summary>One row per active employee: permanent, then open-ended, then fixed-term, each group by name.</summary>
	public List<ShiftTableRow> Rows { get; set; } = new();

	/// <summary>
	/// Active employees without a recognisable contract ("Επώνυμο Όνομα"). They print with the
	/// permanent staff; the page lists them so the user can set their contract.
	/// </summary>
	public List<string> WithoutContract { get; set; } = new();

	/// <summary>The form's editable texts (letterhead, titles, signatures…), as set in Παράμετροι → Αναφορές.</summary>
	public ReportTextSet Texts { get; set; } = ReportTextSet.Empty;

	/// <summary>The report's colours, as set on the report parameters page (defaults until a service fills them).</summary>
	public ReportColorSet Colors { get; set; } = ReportColorSet.Default;

	/// <summary>True for Saturdays, Sundays and holidays — shaded on the form.</summary>
	public bool IsShaded(int day)
	{
		var dow = new DateOnly(Year, Month, day).DayOfWeek;
		return dow is DayOfWeek.Saturday or DayOfWeek.Sunday || HolidayDays.Contains(day);
	}
}

/// <summary>One employee's row in the shift table.</summary>
public class ShiftTableRow
{
	public string LastName { get; set; } = "";
	public string FirstName { get; set; } = "";
	public string Speciality { get; set; } = "";

	/// <summary>
	/// The contract group the row prints in. An employee without a recognisable contract is in
	/// the permanent group.
	/// </summary>
	public ContractKind Group { get; set; }

	/// <summary>Code per day of the month (index 0 = day 1), or null when nothing was entered.</summary>
	public string?[] DayCodes { get; set; } = [];

	/// <summary>Times each work-shift code was worked, aligned with <see cref="ShiftTableModel.WorkCodes"/>.</summary>
	public int[] Counts { get; set; } = [];

	/// <summary>Paid "to complete" hours (the daily column; same as the certificate).</summary>
	public decimal DailyToComplete { get; set; }

	/// <summary>Paid holiday hours (the Sunday and holiday column; same as the certificate).</summary>
	public decimal SundayHolidayToComplete { get; set; }
}
