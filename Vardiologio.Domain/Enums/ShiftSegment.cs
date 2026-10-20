namespace Vardiologio.Domain.Enums;

/// <summary>
/// The classification assigned to a work shift on the analytical sheet.
/// Determined once per shift code from the source template block:
/// Night: codes 1, 2, 12, and 13; Day: codes 3 through 11.
/// Status codes are not assigned a classification.
/// </summary>
public enum ShiftSegment
{
	/// <summary>Day shift.</summary>
	Day = 1,
	/// <summary>Night shift.</summary>
	Night = 2,
	/// <summary>Intermediate shift (reserved; not used by codes 1–13).</summary>
	Intermediate = 3
}