using Vardiologio.Application.Hours;
using Vardiologio.Domain.Enums;

namespace Vardiologio.Application.ShiftCodes;

/// <summary>
/// Pure validation of a shift code's allowed days and hour split. No EF, no UI.
/// Complements <see cref="ShiftTimeRules"/> (which validates the times themselves).
/// </summary>
public static class ShiftCodeHoursRules
{
	/// <summary>Largest value accepted in one cell (the longest allowed shift).</summary>
	public static readonly decimal MaxCellHours = (decimal)ShiftTimeRules.MaxDuration.TotalHours;

	/// <summary>
	/// Validates the split against the code's times and allowed days.
	/// Returns a Greek error message, or null when valid.
	/// </summary>
	/// <param name="start">Shift start (null for status codes).</param>
	/// <param name="end">Shift end (null for status codes).</param>
	/// <param name="allowedDays">The code's allowed days.</param>
	/// <param name="hours">The non-zero cells of the split.</param>
	public static string? Validate(TimeOnly? start, TimeOnly? end, DayType allowedDays, IReadOnlyList<ShiftCodeHoursItem> hours)
	{
		// Status codes (no times) carry neither allowed days nor hours.
		if (start is null || end is null)
			return allowedDays == DayType.None && hours.Count == 0
				? null
				: "Επιτρεπτές μέρες και κατανομή ωρών ορίζονται μόνο για βάρδιες με ώρες.";

		foreach (var cell in hours)
		{
			if (cell.Hours <= 0 || cell.Hours > MaxCellHours)
				return $"Οι ώρες κάθε κελιού πρέπει να είναι από 0 έως {MaxCellHours:0}.";

			// Every cell must be on a single allowed kind of day.
			if (!HoursLabels.DayTypes.Contains(cell.DayType) || (allowedDays & cell.DayType) == 0)
				return $"Υπάρχουν ώρες για «{HoursLabels.Day(cell.DayType)}», που δεν είναι επιτρεπτή μέρα.";
		}

		// One cell per (day, category).
		if (hours.GroupBy(h => (h.DayType, h.Category)).Any(g => g.Count() > 1))
			return "Η ίδια κατηγορία εμφανίζεται δύο φορές για την ίδια μέρα.";

		// The hours of one day cannot exceed the shift length.
		var duration = (decimal)ShiftTimeRules.Duration(start.Value, end.Value).TotalHours;
		foreach (var day in hours.GroupBy(h => h.DayType))
		{
			var total = day.Sum(h => h.Hours);
			if (total > duration)
				return $"«{HoursLabels.Day(day.Key)}»: {total:0.##} ώρες, περισσότερες από τη διάρκεια της βάρδιας ({duration:0.##}).";
		}

		return null;
	}
}
