namespace Vardiologio.Application.Reports;

/// <summary>
/// Pure rules that place a shift's overtime inside its time window. The database stores how
/// many overtime hours a code yields on a kind of day, not when they are worked; the rule
/// (as on the organisation's sheet) is that overtime is the last part of the shift.
/// </summary>
public static class ShiftHoursRules
{
	/// <summary>Length of a shift in hours; a shift that ends at or before its start ends the next day (22:00–06:00 = 8).</summary>
	public static decimal Duration(TimeOnly start, TimeOnly end)
	{
		var minutes = (end - start).TotalMinutes;   // TimeOnly subtraction already wraps past midnight
		if (minutes == 0) minutes = 24 * 60;        // same start and end = a full day, not zero
		return (decimal)minutes / 60m;
	}

	/// <summary>
	/// Start and end of the overtime part of a shift: its last <paramref name="overtimeHours"/> hours.
	/// Null when there is no overtime. When the overtime covers the whole shift (or more, if the
	/// data is inconsistent) the window is the whole shift.
	/// Examples: 03:00–11:30 with 2 h → 09:30–11:30; 22:00–06:00 with 1.5 h → 04:30–06:00.
	/// </summary>
	public static (TimeOnly Start, TimeOnly End)? OvertimeWindow(TimeOnly start, TimeOnly end, decimal overtimeHours)
	{
		if (overtimeHours <= 0) return null;
		if (overtimeHours >= Duration(start, end)) return (start, end);

		// TimeOnly.AddHours wraps around midnight, which is what a night shift needs.
		return (end.AddHours(-(double)overtimeHours), end);
	}

	/// <summary>
	/// Hours of the shift that belong to the regular schedule: its length minus the overtime
	/// (never negative). 0 for a shift that is overtime as a whole (e.g. a Saturday shift).
	/// </summary>
	public static decimal RegularHours(TimeOnly start, TimeOnly end, decimal overtimeHours) =>
		Math.Max(0, Duration(start, end) - Math.Max(0, overtimeHours));
}
