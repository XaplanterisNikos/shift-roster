using Vardiologio.Application.ReportTexts;

namespace Vardiologio.Application.Reports;

/// <summary>Which of the three attendance reports a model holds.</summary>
public enum AttendanceSheet
{
	/// <summary>
	/// Permanent and open-ended staff (Μόνιμοι, ΙΔΑΧ), plus anyone whose contract is not
	/// recognised, so that nobody is left out. Signed by the author and the head of the directorate.
	/// </summary>
	Staff,

	/// <summary>Fixed-term staff (ΙΔΟΧ), on a form with its own texts.</summary>
	FixedTerm,

	/// <summary>The head of the directorate alone (signed by the Secretary General).</summary>
	Directorate
}

/// <summary>
/// Data for the monthly staff attendance report ("ΠΙΝΑΚΑΣ ΠΑΡΟΥΣΙΩΝ ΠΡΟΣΩΠΙΚΟΥ"):
/// one row per employee, one mark per day ("√", a status code, or empty).
/// Printed per fortnight (1–15, 16–end). Each fortnight is one document: the letterhead and the
/// titles once at the top, the employees in numbered tables (one per page), and the signatures
/// once under the last table.
/// </summary>
public class AttendanceReportModel
{
	/// <summary>
	/// Employees in the first table, which shares its page with the letterhead. Chosen so the
	/// page fits one A4 landscape in both Excel and PDF.
	/// </summary>
	public const int RowsFirstPage = 22;

	/// <summary>Employees in each following table, which has a page of its own.</summary>
	public const int RowsNextPages = 30;

	/// <summary>
	/// Rows of table height kept free under the last table, so the signatures always print on
	/// the same page as employees and never on a page of their own.
	/// </summary>
	public const int SignatureRows = 7;

	/// <summary>Report year.</summary>
	public int Year { get; set; }

	/// <summary>Report month (1–12).</summary>
	public int Month { get; set; }

	/// <summary>Which of the three reports this is.</summary>
	public AttendanceSheet Sheet { get; set; }

	/// <summary>Number of days in the month (28–31).</summary>
	public int DaysInMonth => DateTime.DaysInMonth(Year, Month);

	/// <summary>One row per employee, ordered by name.</summary>
	public List<AttendanceReportRow> Rows { get; set; } = new();

	/// <summary>
	/// Names of the employees of this report whose contract is missing or not recognised
	/// (staff report only: they are printed there). Shown as a warning on the page.
	/// </summary>
	public List<string> WithoutContract { get; set; } = new();

	/// <summary>The form's editable texts (letterhead, titles, signatures…), as set in Παράμετροι → Αναφορές.</summary>
	public ReportTextSet Texts { get; set; } = ReportTextSet.Empty;

	/// <summary>The two printed documents of the month: days 1–15 and days 16–end.</summary>
	public IEnumerable<(DateOnly From, DateOnly To)> Fortnights()
	{
		yield return (new DateOnly(Year, Month, 1), new DateOnly(Year, Month, 15));
		yield return (new DateOnly(Year, Month, 16), new DateOnly(Year, Month, DaysInMonth));
	}

	/// <summary>
	/// The employees split into the numbered tables of one document (ΠΙΝΑΚΑΣ 1, 2, …), the same
	/// for both fortnights. Each table is one printed page. The last table is kept short enough
	/// to leave room for the signatures; when it is not, its last rows move to one more table.
	/// With no employees there is still one (empty) table, so the report is never blank.
	/// </summary>
	public List<AttendanceReportRow[]> Tables()
	{
		var tables = new List<AttendanceReportRow[]>();

		// Fill the pages in order: the first holds fewer rows because of the letterhead.
		for (var start = 0; start < Rows.Count;)
		{
			var capacity = tables.Count == 0 ? RowsFirstPage : RowsNextPages;
			var count = Math.Min(capacity, Rows.Count - start);
			tables.Add(Rows.GetRange(start, count).ToArray());
			start += count;
		}
		if (tables.Count == 0) tables.Add([]);

		// Room for the signatures under the last table.
		var last = tables[^1];
		var room = (tables.Count == 1 ? RowsFirstPage : RowsNextPages) - SignatureRows;
		if (last.Length > room)
		{
			tables[^1] = last[..room];
			tables.Add(last[room..]);
		}

		return tables;
	}
}

/// <summary>One employee's row in the attendance report.</summary>
public class AttendanceReportRow
{
	public string LastName { get; set; } = "";
	public string FirstName { get; set; } = "";
	public string Speciality { get; set; } = "";

	/// <summary>Mark per day of the month (index 0 = day 1): "√", a status code, or "" when nothing was entered.</summary>
	public string[] Marks { get; set; } = [];

	/// <summary>Name as printed ("Επώνυμο Όνομα"), without stray spaces from the stored data.</summary>
	public string FullName => $"{LastName.Trim()} {FirstName.Trim()}".Trim();

	/// <summary>Mark of one day of the month (1-based).</summary>
	public string Mark(int day) => Marks[day - 1];
}
