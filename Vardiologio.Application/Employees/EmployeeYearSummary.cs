using Vardiologio.Application.Hours;
using Vardiologio.Domain.Entities;
using Vardiologio.Domain.Enums;

namespace Vardiologio.Application.Employees;

/// <summary>Kind of a slice of the "days of the year" chart.</summary>
public enum DayKind
{
	/// <summary>Days with a day shift.</summary>
	Day,

	/// <summary>Days with a night shift.</summary>
	Night,

	/// <summary>Days with an intermediate shift.</summary>
	Intermediate,

	/// <summary>Days with one status code (rest day, leave, ...): one slice per code.</summary>
	Status,

	/// <summary>Days without an entry, counted only in the months that have entries.</summary>
	Empty
}

/// <summary>One slice of the "days of the year" chart.</summary>
/// <param name="Label">What the days are ("Ημέρας", "Ρεπό", "Κενές").</param>
/// <param name="Days">How many days.</param>
/// <param name="Kind">The kind, which decides the colour.</param>
public sealed record DayKindCount(string Label, int Days, DayKind Kind);

/// <summary>
/// One employee's year for the statistics of the employee card: the hours of every month and
/// how the days were spent, with three totals.
/// </summary>
/// <param name="Year">The year.</param>
/// <param name="Months">Twelve months (January first), each with one row per pay category, as in the hours report.</param>
/// <param name="Days">Slices of the days chart, without empty slices.</param>
/// <param name="PaidHours">Paid hours of the year, every category.</param>
/// <param name="NightShifts">Entries with a night shift.</param>
/// <param name="SundayHolidayShifts">Entries with a shift on a Sunday or a holiday.</param>
public sealed record EmployeeYearSummary(
	int Year,
	IReadOnlyList<IReadOnlyList<CategoryHours>> Months,
	IReadOnlyList<DayKindCount> Days,
	decimal PaidHours,
	int NightShifts,
	int SundayHolidayShifts);

/// <summary>
/// Pure builder of <see cref="EmployeeYearSummary"/>: the months come from
/// <see cref="MonthlyHoursCalculator"/>, so the hours are the same as in the hours report and the
/// certificates. No EF — the caller loads the data.
/// </summary>
public static class EmployeeYearSummaryBuilder
{
	/// <summary>Builds the summary of one employee's year.</summary>
	/// <param name="year">The year.</param>
	/// <param name="days">The employee's roster entries of the year (others are ignored).</param>
	/// <param name="codes">Every shift code by id, inactive ones too.</param>
	/// <param name="hours">The hour split rows of all shift codes.</param>
	/// <param name="holidays">The holidays of the year.</param>
	/// <param name="limits">The monthly limits.</param>
	public static EmployeeYearSummary Build(
		int year,
		IReadOnlyCollection<ShiftDay> days,
		IReadOnlyDictionary<int, ShiftCode> codes,
		IReadOnlyCollection<ShiftCodeHours> hours,
		IReadOnlySet<DateOnly> holidays,
		HourLimits limits)
	{
		var ofYear = days.Where(d => d.Date.Year == year).ToList();

		var months = Enumerable.Range(1, 12)
			.Select(m => MonthlyHoursCalculator.Calculate(year, m, ofYear, hours, holidays, limits))
			.ToList();

		var work = ofYear
			.Select(d => (d.Date, Code: codes.GetValueOrDefault(d.ShiftCodeId)))
			.Where(x => x.Code is not null)
			.Select(x => (x.Date, Code: x.Code!))
			.ToList();

		var slices = new List<DayKindCount>();
		AddSlice(slices, "Ημέρας", work.Count(x => x.Code.Segment == ShiftSegment.Day), DayKind.Day);
		AddSlice(slices, "Νύχτας", work.Count(x => x.Code.Segment == ShiftSegment.Night), DayKind.Night);
		AddSlice(slices, "Ενδιάμεσης", work.Count(x => x.Code.Segment == ShiftSegment.Intermediate), DayKind.Intermediate);

		foreach (var status in work
			.Where(x => x.Code.Segment is null)
			.GroupBy(x => x.Code.Id)
			.OrderByDescending(g => g.Count())
			.ThenBy(g => g.First().Code.Code, StringComparer.Ordinal))
		{
			var code = status.First().Code;
			AddSlice(slices, string.IsNullOrWhiteSpace(code.Description) ? code.Code : code.Description,
				status.Count(), DayKind.Status);
		}

		var emptyDays = ofYear
			.GroupBy(d => d.Date.Month)
			.Sum(g => DateTime.DaysInMonth(year, g.Key) - g.Select(d => d.Date).Distinct().Count());
		AddSlice(slices, "Κενές", emptyDays, DayKind.Empty);

		return new EmployeeYearSummary(
			year,
			months,
			slices,
			months.Sum(m => m.Sum(h => h.Paid)),
			work.Count(x => x.Code.Segment == ShiftSegment.Night),
			work.Count(x => x.Code.Segment is not null
				&& DayTypeRules.Resolve(x.Date, holidays) is DayType.Sunday or DayType.Holiday));
	}

	/// <summary>Adds a slice when it has days; an empty slice would only clutter the legend.</summary>
	private static void AddSlice(List<DayKindCount> slices, string label, int days, DayKind kind)
	{
		if (days > 0) slices.Add(new DayKindCount(label, days, kind));
	}
}
