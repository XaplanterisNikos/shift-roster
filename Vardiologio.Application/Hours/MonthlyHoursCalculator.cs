using Vardiologio.Domain.Entities;
using Vardiologio.Domain.Enums;

namespace Vardiologio.Application.Hours;

/// <summary>
/// One pay category of an employee's month: hours declared by the roster and hours paid
/// after the monthly limits. <see cref="Excess"/> is what is declared but not paid.
/// </summary>
/// <param name="Category">The pay category.</param>
/// <param name="Declared">Hours produced by the roster entries of the month.</param>
/// <param name="Paid">Hours paid after the category limit and the combined limit.</param>
/// <param name="Limit">The category's own monthly limit (before the combined limit).</param>
public sealed record CategoryHours(HourCategory Category, decimal Declared, decimal Paid, decimal Limit)
{
	/// <summary>Declared hours above the limits (not paid).</summary>
	public decimal Excess => Declared - Paid;
}

/// <summary>
/// Pure monthly calculation for one employee: turns roster entries into hours per pay
/// category (via the per-code/per-day split) and applies the monthly limits.
/// No EF, no UI — the caller loads the data and passes it in.
/// </summary>
public static class MonthlyHoursCalculator
{
	/// <summary>
	/// Calculates declared and paid hours per category for one employee and one month.
	/// Steps: (1) each entry's kind of day (holiday wins) picks the code's hour rows;
	/// (2) each category is capped by its own limit; (3) if ToComplete + Holiday still exceed
	/// the combined limit, ToComplete is cut first, then Holiday.
	/// </summary>
	/// <param name="year">Year of the month.</param>
	/// <param name="month">Month (1–12).</param>
	/// <param name="days">The employee's roster entries; entries outside the month are ignored.</param>
	/// <param name="hours">The hour split rows of all shift codes.</param>
	/// <param name="holidays">All holiday dates (used for the kind of day and the holiday limit).</param>
	/// <param name="limits">The monthly limits.</param>
	/// <returns>One row per category, in <see cref="HourCategory"/> order.</returns>
	public static IReadOnlyList<CategoryHours> Calculate(
		int year,
		int month,
		IEnumerable<ShiftDay> days,
		IEnumerable<ShiftCodeHours> hours,
		IReadOnlySet<DateOnly> holidays,
		HourLimits limits)
	{
		var categories = Enum.GetValues<HourCategory>();

		// Index the split by (code, kind of day) so each entry is a single lookup.
		var hoursByCodeAndDay = hours.ToLookup(h => (h.ShiftCodeId, h.DayType));

		// (1) Declared hours: sum every entry's rows for its kind of day.
		var declared = categories.ToDictionary(c => c, _ => 0m);
		foreach (var day in days)
		{
			if (day.Date.Year != year || day.Date.Month != month) continue;   // not this month

			var dayType = DayTypeRules.Resolve(day.Date, holidays);

			// A code with no rows for this kind of day (e.g. status codes, or a code entered
			// on a day it is not allowed on) yields no hours; the entry screen warns instead.
			foreach (var row in hoursByCodeAndDay[(day.ShiftCodeId, dayType)])
				declared[row.Category] += row.Hours;
		}

		// (2) Per-category limits. The holiday limit depends on the month's holiday count.
		var holidaysInMonth = holidays.Count(h => h.Year == year && h.Month == month);
		var paid = categories.ToDictionary(
			c => c,
			c => Math.Min(declared[c], Limit(c, limits, holidaysInMonth)));

		// (3) Combined limit ToComplete + Holiday: cut ToComplete first, then Holiday.
		var over = paid[HourCategory.ToComplete] + paid[HourCategory.Holiday] - limits.ToCompletePlusHolidayMonthly;
		if (over > 0)
		{
			var cutToComplete = Math.Min(over, paid[HourCategory.ToComplete]);
			paid[HourCategory.ToComplete] -= cutToComplete;
			over -= cutToComplete;

			// Only reached if the combined limit is lower than the holiday hours alone.
			paid[HourCategory.Holiday] -= Math.Min(over, paid[HourCategory.Holiday]);
		}

		return categories
			.Select(c => new CategoryHours(c, declared[c], paid[c], Limit(c, limits, holidaysInMonth)))
			.ToList();
	}

	/// <summary>
	/// The monthly limit of one category on its own (before the combined limit).
	/// The holiday limit is the per-holiday value times the month's holidays (0 if none).
	/// </summary>
	/// <param name="category">The pay category.</param>
	/// <param name="limits">The monthly limits.</param>
	/// <param name="holidaysInMonth">Number of holidays in the month (weekend ones included).</param>
	public static decimal Limit(HourCategory category, HourLimits limits, int holidaysInMonth) =>
		category switch
		{
			HourCategory.ToComplete => limits.ToCompleteMonthly,
			HourCategory.Simple => limits.SimpleMonthly,
			HourCategory.Night => limits.NightMonthly,
			HourCategory.Sunday => limits.SundayMonthly,
			HourCategory.Holiday => limits.HolidayPerHoliday * holidaysInMonth,
			_ => throw new ArgumentOutOfRangeException(nameof(category), category, null)
		};
}
