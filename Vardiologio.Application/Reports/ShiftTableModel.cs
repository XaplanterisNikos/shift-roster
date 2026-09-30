namespace Vardiologio.Application.Reports;

/// <summary>
/// Data for the monthly "ΠΙΝΑΚΑΣ ΒΑΡΔΙΩΝ ΕΡΓΑΣΙΑΣ ΠΡΟΣΩΠΙΚΟΥ" (official shift table, A3 landscape):
/// one row per active employee with the code of every day, how many times each work-shift code
/// was worked, and the paid "προς συμπλήρωση" hours; plus the legend of all codes.
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

	/// <summary>One row per active employee, ordered by name.</summary>
	public List<ShiftTableRow> Rows { get; set; } = new();

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

	/// <summary>Code per day of the month (index 0 = day 1), or null when nothing was entered.</summary>
	public string?[] DayCodes { get; set; } = [];

	/// <summary>Times each work-shift code was worked, aligned with <see cref="ShiftTableModel.WorkCodes"/>.</summary>
	public int[] Counts { get; set; } = [];

	/// <summary>ΕΡΓΑΣΙΑ ΠΡΟΣ ΣΥΜΠΛΗΡΩΣΗ – ΗΜΕΡΗΣΙΑ: paid "Προς συμπλήρωση" hours (same as the certificate).</summary>
	public decimal DailyToComplete { get; set; }

	/// <summary>ΚΥΡΙΑΚΩΝ ΕΞΑΙΡΕΣΙΜΩΝ: paid "Αργίας" hours (same as the certificate).</summary>
	public decimal SundayHolidayToComplete { get; set; }
}
