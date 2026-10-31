using Vardiologio.Application.ReportColors;
using Vardiologio.Application.ReportTexts;

namespace Vardiologio.Application.Reports;

/// <summary>Data for the monthly roster (Σ.Ω.): one row per employee, columns per day + program counts.</summary>
public class ReportModel
{
	public int Year { get; set; }
	public int Month { get; set; }
	public int DaysInMonth { get; set; }
	public List<ReportRow> Rows { get; set; } = new();

	/// <summary>
	/// Work-shift codes in numeric order with their times (Παράμετροι → Βαρδιών): the count
	/// columns of the table ("ΒΑΡΔΙΑ ΕΡΓΑΣΙΑΣ", one per code, in this order) and the first part
	/// of the legend under the table. Status codes (leave, rest…) are not counted.
	/// </summary>
	public List<(string Code, string Description)> WorkCodes { get; set; } = new();

	/// <summary>Status codes (leave, rest, holiday…): second part of the legend.</summary>
	public List<(string Code, string Description)> StatusCodes { get; set; } = new();

	/// <summary>
	/// Active employees without a recognisable contract ("Επώνυμο Όνομα"). They print with the
	/// permanent staff; the page lists them so the user can set their contract.
	/// </summary>
	public List<string> WithoutContract { get; set; } = new();

	/// <summary>The form's editable texts (letterhead, titles, addressee, signatures), as set in Παράμετροι → Αναφορών.</summary>
	public ReportTextSet Texts { get; set; } = ReportTextSet.Empty;

	/// <summary>The report's colours, as set on the report parameters page (defaults until a service fills them).</summary>
	public ReportColorSet Colors { get; set; } = ReportColorSet.Default;
}

/// <summary>One employee's row in the monthly roster.</summary>
public class ReportRow
{
	/// <summary>Sequential row number (not printed on the monthly roster).</summary>
	public int Index { get; set; }

	/// <summary>
	/// The contract group the row prints in: permanent first, then open-ended (ΙΔΑΧ), then
	/// fixed-term (ΙΔΟΧ), each group alphabetical and after a separator row (except the first).
	/// An employee without a recognisable contract is in the permanent group.
	/// </summary>
	public ContractKind Group { get; set; }
	public string LastName { get; set; } = "";
	public string FirstName { get; set; } = "";
	public string Speciality { get; set; } = "";
	/// <summary>Length = DaysInMonth; code per day or null.</summary>
	public string?[] DayCodes { get; set; } = [];

	/// <summary>Times each work code was worked this month, aligned with <see cref="ReportModel.WorkCodes"/>.</summary>
	public int[] ProgramCounts { get; set; } = [];

	/// <summary>Paid "to complete" hours (the daily column; same value as on the shift table).</summary>
	public decimal DailyToComplete { get; set; }

	/// <summary>Paid holiday hours (the Sunday and holiday column; same value as on the shift table).</summary>
	public decimal SundayHolidayToComplete { get; set; }
}