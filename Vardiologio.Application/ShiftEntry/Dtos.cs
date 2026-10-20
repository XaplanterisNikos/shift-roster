using Vardiologio.Domain.Enums;

namespace Vardiologio.Application.ShiftEntry;

/// <summary>Employee option for the entry dropdown (Display = "LastName FirstName — Speciality").</summary>
public record EmployeeListItem(int Id, string Display);

/// <summary>
/// Shift-code option of the entry screen (Label = "Code — Description").
/// Segment is the code's Day/Night classification, shown as a tag next to the day;
/// null for status codes (leave, day off, ...), which have no time range.
/// </summary>
/// <param name="Id">Shift code id.</param>
/// <param name="Label">"Code — Description" (with "(ανενεργός)" for a retired code).</param>
/// <param name="Segment">Day / Night / Intermediate, or null for a status code.</param>
/// <param name="Code">The code alone ("3", "Ρ"): the large text of its tile.</param>
/// <param name="Description">Its times or name ("06:00-14:00", "Ρεπό"): the small text of its tile.</param>
/// <param name="AllowedDays">Kinds of day the code is meant for, so the screen can mark it before it is picked.</param>
public record ShiftCodeOption(int Id, string Label, ShiftSegment? Segment,
	string Code = "", string Description = "", DayType AllowedDays = DayType.None);

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