using System.ComponentModel.DataAnnotations.Schema;
using Vardiologio.Domain.Enums;

namespace Vardiologio.Domain.Entities;

/// <summary>
/// A shift or status code used in the daily roster. Working-shift codes (1–13)
/// include a time range and a <see cref="Segment"/>; status codes do not.
/// </summary>
public class ShiftCode
{
	/// <summary>Primary key.</summary>
	public int Id { get; set; }

	/// <summary>Code entered in a roster cell, such as "1"–"13", "Ρ", "Α", or "Κ".</summary>
	public string Code { get; set; } = null!;

	/// <summary>
	/// Human-readable description, such as a shift time range or a status description.
	/// </summary>
	public string Description { get; set; } = null!;

	/// <summary>Shift start time. Set only for working-shift codes.</summary>
	public TimeOnly? StartTime { get; set; }

	/// <summary>Shift end time. Set only for working-shift codes.</summary>
	public TimeOnly? EndTime { get; set; }

	/// <summary>
	/// Day, night, or intermediate classification of a working shift.
	/// <c>null</c> for status codes. Used by the analytical report.
	/// </summary>
	public ShiftSegment? Segment { get; set; }

	/// <summary>
	/// Whether this code is currently offered in the app's settings screen.
	/// Soft-delete flag: existing ShiftDay rows keep referencing this code
	/// (FK delete behaviour is Restrict), so the code itself is never hard-deleted.
	/// </summary>
	public bool IsActive { get; set; } = true;

	/// <summary>Indicates whether this code represents a working shift with a time range.</summary>
	[NotMapped] 
	public bool IsWorkShift => StartTime.HasValue;

	/// <summary>Indicates whether the shift continues past midnight.</summary>
	[NotMapped]
	public bool CrossesMidnight =>
		StartTime.HasValue && EndTime.HasValue && EndTime < StartTime;
}