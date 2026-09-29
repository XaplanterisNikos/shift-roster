namespace Vardiologio.Domain.Enums;

/// <summary>
/// The kind of calendar day a roster entry falls on. It decides which hours a shift code
/// yields and in which category (see <see cref="Entities.ShiftCodeHours"/>).
/// [Flags] so that <see cref="Entities.ShiftCode.AllowedDays"/> can hold several kinds at once;
/// a single day (and a <see cref="Entities.ShiftCodeHours"/> row) always has exactly one flag.
/// A holiday overrides Saturday/Sunday: a holiday on a Saturday is <see cref="Holiday"/>.
/// </summary>
[Flags]
public enum DayType
{
	/// <summary>No day kind (status codes such as Ρ/Α/Κ are not tied to any day kind).</summary>
	None = 0,

	/// <summary>Monday–Friday that is not a holiday (Καθημερινή).</summary>
	Weekday = 1,

	/// <summary>Saturday that is not a holiday (Σάββατο).</summary>
	Saturday = 2,

	/// <summary>Sunday that is not a holiday (Κυριακή).</summary>
	Sunday = 4,

	/// <summary>A public holiday from the holidays list, whatever the weekday (Αργία).</summary>
	Holiday = 8
}
