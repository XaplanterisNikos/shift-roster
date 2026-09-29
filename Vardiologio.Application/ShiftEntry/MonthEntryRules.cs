using Vardiologio.Application.Hours;
using Vardiologio.Domain.Entities;
using Vardiologio.Domain.Enums;

namespace Vardiologio.Application.ShiftEntry;

/// <summary>
/// Result of checking one employee's month in the entry screen: per-day warnings and the
/// month's hours per pay category. Warnings never block saving.
/// </summary>
public sealed class MonthCheck
{
	/// <summary>Warning text per date (only dates with a warning are present).</summary>
	public IReadOnlyDictionary<DateOnly, string> DayWarnings { get; init; } = new Dictionary<DateOnly, string>();

	/// <summary>Declared / paid / limit per category, in HourCategory order.</summary>
	public IReadOnlyList<CategoryHours> Hours { get; init; } = Array.Empty<CategoryHours>();

	/// <summary>The combined monthly limit of Προς συμπλήρωση + Αργίας (e.g. 138).</summary>
	public decimal CombinedLimit { get; init; }

	/// <summary>True when any day has a warning or any category has unpaid hours.</summary>
	public bool HasWarnings => DayWarnings.Count > 0 || Hours.Any(h => h.Excess > 0);
}

/// <summary>
/// Pure checks for a month being entered (no EF, no UI): allowed days per code, the
/// 1st-of-month rule for code 12, and the monthly hours with their limits.
/// Works on the unsaved grid, so the screen can show the result live.
/// </summary>
public static class MonthEntryRules
{
	/// <summary>
	/// Codes that must not be used on the 1st of a month, with the code to use instead.
	/// Code 12 (Friday 22:00 → Saturday 06:00) starts in the previous month, so on a Saturday
	/// that is the 1st the manager enters code 2 (00:00–06:00) instead (confirmed rule).
	/// Keyed by Code, which is immutable after creation.
	/// </summary>
	public static readonly IReadOnlyDictionary<string, string> FirstOfMonthReplacements =
		new Dictionary<string, string> { ["12"] = "2" };

	/// <summary>Checks the month's entries and calculates its hours.</summary>
	/// <param name="entry">The month grid (unsaved values included; empty days are skipped).</param>
	/// <param name="codes">Shift codes by Id (active and inactive).</param>
	/// <param name="hours">The hour split rows of all codes.</param>
	/// <param name="holidays">Holiday dates (at least those of the month).</param>
	/// <param name="limits">The monthly limits.</param>
	public static MonthCheck Check(
		MonthEntry entry,
		IReadOnlyDictionary<int, ShiftCode> codes,
		IEnumerable<ShiftCodeHours> hours,
		IReadOnlySet<DateOnly> holidays,
		HourLimits limits)
	{
		var warnings = new Dictionary<DateOnly, string>();
		var days = new List<ShiftDay>();

		foreach (var day in entry.Days)
		{
			if (day.ShiftCodeId is not int codeId) continue;   // empty day: nothing to check

			// Input for the hours calculation (only Date and ShiftCodeId are used).
			days.Add(new ShiftDay { EmployeeId = entry.EmployeeId, Date = day.Date, ShiftCodeId = codeId });

			if (!codes.TryGetValue(codeId, out var code)) continue;   // unknown id: no rules to apply

			var messages = new List<string>();

			// (1) Allowed kinds of day (status codes have None = allowed anywhere).
			var dayType = DayTypeRules.Resolve(day.Date, holidays);
			if (!DayTypeRules.IsAllowed(code.AllowedDays, dayType))
				messages.Add($"Ο κωδικός {code.Code} δεν προβλέπεται για {HoursLabels.Day(dayType)}.");

			// (2) Codes replaced on the 1st of the month (12 -> 2).
			if (day.Date.Day == 1 && FirstOfMonthReplacements.TryGetValue(code.Code, out var replacement))
				messages.Add($"Την 1η του μήνα μπαίνει ο κωδικός {replacement} αντί για τον {code.Code} " +
				             $"(ο {code.Code} ξεκινά στον προηγούμενο μήνα).");

			if (messages.Count > 0) warnings[day.Date] = string.Join(" ", messages);
		}

		// (3) Monthly hours per category with the limits applied.
		var result = MonthlyHoursCalculator.Calculate(entry.Year, entry.Month, days, hours, holidays, limits);

		return new MonthCheck
		{
			DayWarnings = warnings,
			Hours = result,
			CombinedLimit = limits.ToCompletePlusHolidayMonthly
		};
	}
}
