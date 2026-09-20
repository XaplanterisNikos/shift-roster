namespace Vardiologio.Application.Reports;

/// <summary>Data for the monthly roster (Σ.Ω.): one row per employee, columns per day + program counts.</summary>
public class ReportModel
{
	public int Year { get; set; }
	public int Month { get; set; }
	public int DaysInMonth { get; set; }
	public List<ReportRow> Rows { get; set; } = new();
}

/// <summary>One employee's row in the monthly roster.</summary>
public class ReportRow
{
	public int Index { get; set; }                 // Α/Α (sequential row number)
	public string LastName { get; set; } = "";
	public string FirstName { get; set; } = "";
	public string Speciality { get; set; } = "";
	public string?[] DayCodes { get; set; } = [];  // length = DaysInMonth; code per day or null
	public int[] ProgramCounts { get; set; } = new int[7]; // times programs 1..7 were worked this month
}