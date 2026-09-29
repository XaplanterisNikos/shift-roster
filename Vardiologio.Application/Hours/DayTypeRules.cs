using Vardiologio.Domain.Enums;

namespace Vardiologio.Application.Hours;

/// <summary>
/// Pure rules for the kind of a calendar day and whether a shift code is allowed on it.
/// No EF, no UI — easy to test and reuse (entry warnings, monthly calculation, reports).
/// </summary>
public static class DayTypeRules
{
	/// <summary>
	/// The kind of day for a date. A holiday wins over Saturday/Sunday
	/// (e.g. 15 August 2026 is a Saturday but counts as a holiday).
	/// </summary>
	/// <param name="date">The roster date (the day the entry is written on).</param>
	/// <param name="holidays">All holiday dates known to the app.</param>
	public static DayType Resolve(DateOnly date, IReadOnlySet<DateOnly> holidays)
	{
		// Holiday first: it overrides the weekday/weekend classification.
		if (holidays.Contains(date)) return DayType.Holiday;

		return date.DayOfWeek switch
		{
			DayOfWeek.Saturday => DayType.Saturday,
			DayOfWeek.Sunday => DayType.Sunday,
			_ => DayType.Weekday
		};
	}

	/// <summary>
	/// True when a code with the given allowed days may be entered on a day of the given kind.
	/// Codes with <see cref="DayType.None"/> (status codes) are allowed on any day.
	/// </summary>
	/// <param name="allowedDays">The code's <c>AllowedDays</c> flags.</param>
	/// <param name="day">The kind of the day (a single flag, from <see cref="Resolve"/>).</param>
	public static bool IsAllowed(DayType allowedDays, DayType day) =>
		allowedDays == DayType.None || (allowedDays & day) != 0;
}
