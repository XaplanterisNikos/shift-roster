namespace Vardiologio.Application.ShiftEntry;

/// <summary>Employee option for the entry dropdown (Display = "LastName FirstName — Speciality").</summary>
public record EmployeeListItem(int Id, string Display);

/// <summary>Shift-code option for the day dropdown (Label = "Code — Description").</summary>
public record ShiftCodeOption(int Id, string Label);

/// <summary>One day of the month in the entry grid.</summary>
public class DayEntry
{
	public DateOnly Date { get; set; }
	/// <summary>Chosen shift code, or null for an empty day (allowed).</summary>
	public int? ShiftCodeId { get; set; }   
}

/// <summary>A whole month of shift entries for one employee (what the entry page edits).</summary>
public class MonthEntry
{
	public int EmployeeId { get; set; }
	public int Year { get; set; }
	public int Month { get; set; }
	/// <summary>One entry per calendar day of the month.</summary>
	public List<DayEntry> Days { get; set; } = new();
}