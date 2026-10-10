using Vardiologio.Application.Hours;
using Vardiologio.Domain.Entities;
using Vardiologio.Domain.Enums;

namespace Vardiologio.Application.Reports;

/// <summary>
/// Pure builder of the individual analytical sheet: turns one employee's roster entries of a
/// month, the shift codes and the hours rules into the sheet's model. No EF, no UI — the
/// report service loads the data and passes it in (same split as <see cref="MonthlyHoursCalculator"/>).
/// </summary>
public static class IndividualReportBuilder
{
	/// <summary>Builds the sheet of one employee for one month.</summary>
	/// <param name="year">Year of the month.</param>
	/// <param name="month">Month (1–12).</param>
	/// <param name="lastName">Employee surname.</param>
	/// <param name="firstName">Employee first name.</param>
	/// <param name="contractCode">Code of the employee's contract (ΕΙΔΟΣ ΕΡΓΑΣΙΑΣ), or null when none is set.</param>
	/// <param name="entries">The employee's roster entries of the month.</param>
	/// <param name="codes">The shift / status codes those entries use, by Id.</param>
	/// <param name="hours">The hour split rows of all shift codes.</param>
	/// <param name="holidays">Holiday dates of the month.</param>
	/// <param name="limits">The monthly limits.</param>
	public static IndividualReportModel Build(
		int year,
		int month,
		string lastName,
		string firstName,
		string? contractCode,
		IReadOnlyCollection<ShiftDay> entries,
		IReadOnlyDictionary<int, ShiftCode> codes,
		IReadOnlyCollection<ShiftCodeHours> hours,
		IReadOnlySet<DateOnly> holidays,
		HourLimits limits)
	{
		var days = DateTime.DaysInMonth(year, month);
		var byDate = entries.ToDictionary(e => e.Date);
		var hoursByCodeAndDay = hours.ToLookup(h => (h.ShiftCodeId, h.DayType));

		var model = new IndividualReportModel
		{
			Year = year,
			Month = month,
			EmployeeName = $"{lastName} {firstName}".Trim(),
			LastName = lastName.Trim(),
			WorkType = contractCode ?? "",

			// Declared / paid hours per category: the summary under the table reads these.
			Hours = MonthlyHoursCalculator.Calculate(year, month, entries, hours, holidays, limits)
		};

		for (int d = 1; d <= days; d++)
		{
			var date = new DateOnly(year, month, d);
			var row = new IndividualDayRow { Date = date, Index = d };

			if (byDate.TryGetValue(date, out var entry) && codes.TryGetValue(entry.ShiftCodeId, out var code))
			{
				row.Code = code.Code;

				if (code.StartTime.HasValue && code.EndTime.HasValue)
				{
					var start = code.StartTime.Value;
					var end = code.EndTime.Value;

					// Working shift: show its time window and mark presence.
					row.ShiftStart = start.ToString("HH:mm");
					row.ShiftEnd = end.ToString("HH:mm");
					row.Presence = PresenceRules.Mark(code.Code, isWorkShift: true);

					// Holidays decide the kind of day (a holiday wins over Saturday/Sunday).
					var dayType = DayTypeRules.Resolve(date, holidays);
					row.IsSundayOrHoliday = dayType is DayType.Sunday or DayType.Holiday;
					row.IsNight = code.Segment == ShiftSegment.Night;

					// The code's hours on this kind of day, per pay category.
					var split = hoursByCodeAndDay[(code.Id, dayType)].ToList();
					decimal Of(HourCategory c) => split.Where(h => h.Category == c).Sum(h => h.Hours);

					// ΣΥΜΠΛΗΡ. ΕΡΓΑΣΙΑ: Προς συμπλήρωση -> ΗΜΕΡ., Αργίας -> ΚΥΡΙΑΚΗΣ (as on the certificate).
					var toComplete = Of(HourCategory.ToComplete);
					var holiday = Of(HourCategory.Holiday);
					if (toComplete > 0) row.SupplementWeekday = toComplete;
					if (holiday > 0) row.SupplementSunday = holiday;

					// Everything beyond the regular schedule (every category except Προς συμπλήρωση)
					// is worked as the last part of the shift: that is the ΩΡΑΡΙΟ ΥΠΕΡΩΡΙΑΣ window.
					var overtime = Of(HourCategory.Simple) + Of(HourCategory.Night) + Of(HourCategory.Sunday) + holiday;
					if (ShiftHoursRules.OvertimeWindow(start, end, overtime) is { } window)
					{
						row.OvertimeStart = window.Start.ToString("HH:mm");
						row.OvertimeEnd = window.End.ToString("HH:mm");
					}
					row.RegularHours = ShiftHoursRules.RegularHours(start, end, overtime);
				}
				else
				{
					// Status day (leave/rest/etc.): show the code in place of times and presence.
					row.StatusCode = code.Code;
					row.Presence = PresenceRules.Mark(code.Code, isWorkShift: false);
				}
			}
			// else: no entry -> blank row (date + index + name only).

			model.Days.Add(row);
		}

		// Fortnight totals of the two ΣΥΜΠΛΗΡ. ΕΡΓΑΣΙΑ columns (declared hours, before the limits).
		bool FirstHalf(IndividualDayRow r) => r.Date.Day <= 15;
		model.WeekdayFirstHalf = model.Days.Where(FirstHalf).Sum(r => r.SupplementWeekday ?? 0);
		model.WeekdaySecondHalf = model.Days.Where(r => !FirstHalf(r)).Sum(r => r.SupplementWeekday ?? 0);
		model.SundayFirstHalf = model.Days.Where(FirstHalf).Sum(r => r.SupplementSunday ?? 0);
		model.SundaySecondHalf = model.Days.Where(r => !FirstHalf(r)).Sum(r => r.SupplementSunday ?? 0);

		return model;
	}
}
