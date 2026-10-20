using Vardiologio.Domain.Enums;

namespace Vardiologio.Domain.Entities;

/// <summary>
/// How many hours a shift code yields in one pay category on one kind of day.
/// Example: code 13 on a weekday yields 6.5 <see cref="HourCategory.ToComplete"/>
/// and 1.5 <see cref="HourCategory.Night"/> (two rows).
/// The split is data, not a rule computed from the shift times: codes 13 and 15 have the
/// same times (22:00–06:00) but a different split. A code/day with no rows yields no hours.
/// Replaces the old single-value ShiftExtraHours table.
/// </summary>
public class ShiftCodeHours
{
	/// <summary>The shift code (part of the composite primary key).</summary>
	public int ShiftCodeId { get; set; }

	/// <summary>Navigation to the shift code.</summary>
	public ShiftCode ShiftCode { get; set; } = null!;

	/// <summary>The kind of day this row applies to (exactly one flag; part of the key).</summary>
	public DayType DayType { get; set; }

	/// <summary>The pay category the hours count in (part of the key).</summary>
	public HourCategory Category { get; set; }

	/// <summary>Number of hours, e.g. 6.5.</summary>
	public decimal Hours { get; set; }
}
