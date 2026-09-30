namespace Vardiologio.Application.Reports;

/// <summary>Which of the two attendance reports a model holds.</summary>
public enum AttendanceSheet
{
	/// <summary>Every active employee except the directorate (signed by the author and the head of the directorate).</summary>
	Staff,

	/// <summary>The head of the directorate alone (signed by the Secretary General).</summary>
	Directorate
}

/// <summary>
/// Data for the monthly staff attendance report ("ΠΙΝΑΚΑΣ ΠΑΡΟΥΣΙΩΝ ΠΡΟΣΩΠΙΚΟΥ"):
/// one row per employee, one mark per day ("√", a status code, or empty).
/// Printed per fortnight (1–15, 16–end), a fixed number of employees per page,
/// so that every page keeps room for its letterhead and its signatures.
/// </summary>
public class AttendanceReportModel
{
	/// <summary>
	/// Employees per printed page. Chosen so the letterhead, the table and the signature
	/// block always fit on one A4 landscape page (Excel and PDF use the same value).
	/// </summary>
	public const int RowsPerPage = 16;

	/// <summary>Report year.</summary>
	public int Year { get; set; }

	/// <summary>Report month (1–12).</summary>
	public int Month { get; set; }

	/// <summary>Staff report or directorate report.</summary>
	public AttendanceSheet Sheet { get; set; }

	/// <summary>Number of days in the month (28–31).</summary>
	public int DaysInMonth => DateTime.DaysInMonth(Year, Month);

	/// <summary>One row per employee, ordered by name.</summary>
	public List<AttendanceReportRow> Rows { get; set; } = new();

	/// <summary>
	/// The printed pages in order: every page of the first fortnight, then every page of the second.
	/// Table numbers (ΠΙΝΑΚΑΣ 1, 2, …) restart at 1 in each fortnight, as in the original sheet.
	/// A fortnight with no employees still yields one (empty) page, so the report is never blank.
	/// </summary>
	public IEnumerable<(DateOnly From, DateOnly To, int Table, AttendanceReportRow[] Rows)> Pages()
	{
		var fortnights = new[]
		{
			(From: new DateOnly(Year, Month, 1), To: new DateOnly(Year, Month, 15)),
			(From: new DateOnly(Year, Month, 16), To: new DateOnly(Year, Month, DaysInMonth))
		};

		foreach (var (from, to) in fortnights)
		{
			// Split the employees into page-sized chunks; keep one empty chunk when there are none.
			var chunks = Rows.Chunk(RowsPerPage).ToList();
			if (chunks.Count == 0) chunks.Add(Array.Empty<AttendanceReportRow>());

			for (var i = 0; i < chunks.Count; i++)
				yield return (from, to, i + 1, chunks[i]);
		}
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
