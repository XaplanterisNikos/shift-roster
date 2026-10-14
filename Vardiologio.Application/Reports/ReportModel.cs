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

	/// <summary>Work-shift codes in numeric order with their times: first part of the legend under the table.</summary>
	public List<(string Code, string Description)> WorkCodes { get; set; } = new();

	/// <summary>Status codes (leave, rest, holiday…): second part of the legend.</summary>
	public List<(string Code, string Description)> StatusCodes { get; set; } = new();

	/// <summary>The form's editable texts (letterhead, titles, addressee, signatures), as set in Παράμετροι → Αναφορών.</summary>
	public ReportTextSet Texts { get; set; } = ReportTextSet.Empty;

	/// <summary>The report's colours, as set in Παράμετροι → Αναφορών → Χρώματα (defaults until a service fills it).</summary>
	public ReportColorSet Colors { get; set; } = ReportColorSet.Default;
}

/// <summary>One employee's row in the monthly roster.</summary>
public class ReportRow
{
	public int Index { get; set; }                 // Α/Α (sequential row number; not printed on the Σ.Ω.)

	/// <summary>
	/// True for a fixed-term (ΙΔΟΧ) employee. These rows come last, after the permanent /
	/// open-ended ones (and those without a contract), under the separator row of the table.
	/// </summary>
	public bool IsFixedTerm { get; set; }
	public string LastName { get; set; } = "";
	public string FirstName { get; set; } = "";
	public string Speciality { get; set; } = "";
	public string?[] DayCodes { get; set; } = [];  // length = DaysInMonth; code per day or null
	public int[] ProgramCounts { get; set; } = new int[7]; // times programs 1..7 were worked this month
}