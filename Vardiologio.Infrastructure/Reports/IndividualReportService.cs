using Microsoft.EntityFrameworkCore;
using Vardiologio.Application.Reports;
using Vardiologio.Infrastructure.Persistence;

namespace Vardiologio.Infrastructure.Reports;

/// <summary>EF Core implementation of <see cref="IIndividualReportService"/>.</summary>
public class IndividualReportService : IIndividualReportService
{
	private readonly IDbContextFactory<AppDbContext> _factory;
	public IndividualReportService(IDbContextFactory<AppDbContext> factory) => _factory = factory;

	/// <inheritdoc/>
	public async Task<IndividualReportModel> BuildAsync(int employeeId, int year, int month)
	{
		var days = DateTime.DaysInMonth(year, month);
		var first = new DateOnly(year, month, 1);
		var last = new DateOnly(year, month, days);

		await using var db = await _factory.CreateDbContextAsync();

		// Employee display name.
		var emp = await db.Employees
			.Where(e => e.Id == employeeId)
			.Select(e => new { e.LastName, e.FirstName })
			.FirstOrDefaultAsync()
			?? throw new InvalidOperationException($"Employee {employeeId} not found.");

		// Shift entries for the month, projected with the code's time window.
		var entries = await db.ShiftDays
			.Where(s => s.EmployeeId == employeeId && s.Date >= first && s.Date <= last)
			.Select(s => new
			{
				s.Date,
				s.ShiftCodeId,
				s.ShiftCode.Code,
				s.ShiftCode.StartTime,
				s.ShiftCode.EndTime
			})
			.ToListAsync();
		var byDate = entries.ToDictionary(x => x.Date);

		// Extra hours lookup (shift code id -> hours).
		var extraByCodeId = await db.ShiftExtraHours
			.ToDictionaryAsync(x => x.ShiftCodeId, x => x.Hours);

		var model = new IndividualReportModel
		{
			Year = year,
			Month = month,
			EmployeeName = $"{emp.LastName} {emp.FirstName}".Trim(),
			WorkType = "" // empty slot on purpose (see model doc comment)
		};

		for (int d = 1; d <= days; d++)
		{
			var date = new DateOnly(year, month, d);
			var row = new IndividualDayRow { Date = date, Index = d };

			if (byDate.TryGetValue(date, out var e))
			{
				// Extra (supplementary) hours for this shift code, if any.
				decimal? extra = extraByCodeId.TryGetValue(e.ShiftCodeId, out var h) ? h : null;

				if (e.StartTime.HasValue)
				{
					// Working shift: show its time window and mark presence.
					row.ShiftStart = e.StartTime.Value.ToString("HH:mm");
					row.ShiftEnd = e.EndTime?.ToString("HH:mm");
					row.Presence = "√";

					// Route extra hours to weekday vs Sunday bucket by the day of week.
					if (extra.HasValue)
					{
						if (date.DayOfWeek == DayOfWeek.Sunday) row.SupplementSunday = extra;
						else row.SupplementWeekday = extra;
					}
				}
				else
				{
					// Status day (leave/rest/etc.): show the code in place of times and presence.
					row.StatusCode = e.Code;
					row.Presence = e.Code;
				}
			}
			// else: no entry -> blank row (date + index + name only).

			model.Days.Add(row);
		}

		// Fortnight totals for "Εργασία προς Συμπλήρωση".
		bool FirstHalf(IndividualDayRow r) => r.Date.Day <= 15;
		model.WeekdayFirstHalf = model.Days.Where(FirstHalf).Sum(r => r.SupplementWeekday ?? 0);
		model.WeekdaySecondHalf = model.Days.Where(r => !FirstHalf(r)).Sum(r => r.SupplementWeekday ?? 0);
		model.SundayFirstHalf = model.Days.Where(FirstHalf).Sum(r => r.SupplementSunday ?? 0);
		model.SundaySecondHalf = model.Days.Where(r => !FirstHalf(r)).Sum(r => r.SupplementSunday ?? 0);

		return model;
	}
}