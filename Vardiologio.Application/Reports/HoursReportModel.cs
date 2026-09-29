using Vardiologio.Application.Hours;
using Vardiologio.Domain.Enums;

namespace Vardiologio.Application.Reports;

/// <summary>
/// Data for the monthly hours report: one row per employee with declared / paid / excess
/// hours per pay category (same numbers as the entry screen's "Ώρες μήνα").
/// </summary>
public class HoursReportModel
{
	/// <summary>Report year.</summary>
	public int Year { get; set; }

	/// <summary>Report month (1–12).</summary>
	public int Month { get; set; }

	/// <summary>Number of holidays in the month (sets the Αργίας limit).</summary>
	public int HolidaysInMonth { get; set; }

	/// <summary>Each category's own monthly limit for this month (same for every employee).</summary>
	public Dictionary<HourCategory, decimal> Limits { get; set; } = new();

	/// <summary>Combined monthly limit for Προς συμπλήρωση + Αργίας.</summary>
	public decimal CombinedLimit { get; set; }

	/// <summary>One row per employee, ordered by name.</summary>
	public List<HoursReportRow> Rows { get; set; } = new();

	/// <summary>Column sum over all employees of one value of one category (for the totals row).</summary>
	public decimal Total(HourCategory category, Func<CategoryHours, decimal> value) =>
		Rows.SelectMany(r => r.Hours).Where(h => h.Category == category).Sum(value);
}

/// <summary>One employee's row in the hours report.</summary>
public class HoursReportRow
{
	/// <summary>Α/Α (sequential row number).</summary>
	public int Index { get; set; }

	public string LastName { get; set; } = "";
	public string FirstName { get; set; } = "";
	public string Speciality { get; set; } = "";

	/// <summary>Declared / paid / limit per category, in HourCategory order.</summary>
	public IReadOnlyList<CategoryHours> Hours { get; set; } = Array.Empty<CategoryHours>();

	/// <summary>True when any category has unpaid (excess) hours.</summary>
	public bool HasExcess => Hours.Any(h => h.Excess > 0);
}
