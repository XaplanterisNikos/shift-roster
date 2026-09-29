using Microsoft.EntityFrameworkCore;
using Vardiologio.Application.Hours;
using Vardiologio.Application.Reports;
using Vardiologio.Domain.Entities;
using Vardiologio.Domain.Enums;
using Vardiologio.Infrastructure.Persistence;

namespace Vardiologio.Infrastructure.Reports;

/// <summary>EF Core implementation of <see cref="IHoursReportService"/>.</summary>
public class HoursReportService : IHoursReportService
{
	private readonly IDbContextFactory<AppDbContext> _factory;
	public HoursReportService(IDbContextFactory<AppDbContext> factory) => _factory = factory;

	/// <inheritdoc/>
	public async Task<HoursReportModel> BuildAsync(int year, int month)
	{
		var first = new DateOnly(year, month, 1);
		var last = new DateOnly(year, month, DateTime.DaysInMonth(year, month));

		await using var db = await _factory.CreateDbContextAsync();

		// Same employees and order as the Σ.Ω. report (the global filter keeps active ones only).
		var employees = await db.Employees
			.OrderBy(e => e.LastName).ThenBy(e => e.FirstName)
			.Select(e => new { e.Id, e.LastName, e.FirstName, Speciality = e.Speciality.Name })
			.ToListAsync();

		// The month's entries, grouped per employee in memory (one query for everyone).
		var entries = await db.ShiftDays
			.AsNoTracking()
			.Where(s => s.Date >= first && s.Date <= last)
			.ToListAsync();
		var byEmployee = entries.ToLookup(s => s.EmployeeId);

		// Rule data shared by every employee.
		var hours = await db.ShiftCodeHours.AsNoTracking().ToListAsync();
		var holidays = (await db.Holidays
			.Where(h => h.Date >= first && h.Date <= last)
			.Select(h => h.Date)
			.ToListAsync())
			.ToHashSet();
		var limits = await db.HourLimits.AsNoTracking().OrderBy(l => l.Id).FirstOrDefaultAsync()
			?? throw new InvalidOperationException("HourLimits row not found.");

		var model = new HoursReportModel
		{
			Year = year,
			Month = month,
			HolidaysInMonth = holidays.Count,
			CombinedLimit = limits.ToCompletePlusHolidayMonthly,
			Limits = Enum.GetValues<HourCategory>()
				.ToDictionary(c => c, c => MonthlyHoursCalculator.Limit(c, limits, holidays.Count))
		};

		var index = 1;
		foreach (var e in employees)
		{
			model.Rows.Add(new HoursReportRow
			{
				Index = index++,
				LastName = e.LastName,
				FirstName = e.FirstName,
				Speciality = e.Speciality,
				// Same calculation as the entry screen, so both always show the same numbers.
				Hours = MonthlyHoursCalculator.Calculate(year, month, byEmployee[e.Id], hours, holidays, limits)
			});
		}

		return model;
	}
}
