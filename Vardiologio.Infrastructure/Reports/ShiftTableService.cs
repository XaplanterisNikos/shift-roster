using Microsoft.EntityFrameworkCore;
using Vardiologio.Application.Hours;
using Vardiologio.Application.Reports;
using Vardiologio.Domain.Enums;
using Vardiologio.Infrastructure.Persistence;

namespace Vardiologio.Infrastructure.Reports;

/// <summary>EF Core implementation of <see cref="IShiftTableService"/>.</summary>
public class ShiftTableService : IShiftTableService
{
	private readonly IDbContextFactory<AppDbContext> _factory;
	public ShiftTableService(IDbContextFactory<AppDbContext> factory) => _factory = factory;

	/// <inheritdoc/>
	public async Task<ShiftTableModel> BuildAsync(int year, int month)
	{
		var first = new DateOnly(year, month, 1);
		var last = new DateOnly(year, month, DateTime.DaysInMonth(year, month));

		await using var db = await _factory.CreateDbContextAsync();

		// Active employees (global filter), same order as the other reports; the director is included.
		var employees = await db.Employees
			.OrderBy(e => e.LastName).ThenBy(e => e.FirstName)
			.Select(e => new { e.Id, e.LastName, e.FirstName, Speciality = e.Speciality.Name })
			.ToListAsync();

		// The month's entries (also the calculator input).
		var entries = await db.ShiftDays
			.AsNoTracking()
			.Where(s => s.Date >= first && s.Date <= last)
			.ToListAsync();
		var byEmployee = entries.ToLookup(s => s.EmployeeId);

		// Active codes, plus retired ones still used this month (so their days still print).
		var usedIds = entries.Select(s => s.ShiftCodeId).Distinct().ToList();
		var codes = await db.ShiftCodes
			.IgnoreQueryFilters()
			.AsNoTracking()
			.Where(c => c.IsActive || usedIds.Contains(c.Id))
			.ToListAsync();
		var codeById = codes.ToDictionary(c => c.Id, c => c.Code);

		// Work codes in numeric order (count columns + legend); statuses in entry order.
		var workCodes = codes
			.Where(c => c.StartTime != null)
			.OrderBy(c => int.TryParse(c.Code, out var n) ? n : int.MaxValue).ThenBy(c => c.Code)
			.ToList();
		var statusCodes = codes.Where(c => c.StartTime == null).OrderBy(c => c.Id).ToList();

		// Rule data for the paid hours — same inputs as the hours report and the certificate.
		var hours = await db.ShiftCodeHours.AsNoTracking().ToListAsync();
		var holidays = (await db.Holidays
			.Where(h => h.Date >= first && h.Date <= last)
			.Select(h => h.Date)
			.ToListAsync())
			.ToHashSet();
		var limits = await db.HourLimits.AsNoTracking().OrderBy(l => l.Id).FirstOrDefaultAsync()
			?? throw new InvalidOperationException("HourLimits row not found.");

		var model = new ShiftTableModel
		{
			Year = year,
			Month = month,
			HolidayDays = holidays.Select(h => h.Day).OrderBy(d => d).ToList(),
			WorkCodes = workCodes.Select(c => (c.Code, c.Description)).ToList(),
			StatusCodes = statusCodes.Select(c => (c.Code, c.Description)).ToList()
		};

		foreach (var e in employees)
		{
			var mine = byEmployee[e.Id].ToList();

			// Code of every day of the month.
			var dayCodes = new string?[model.DaysInMonth];
			foreach (var s in mine)
				dayCodes[s.Date.Day - 1] = codeById.TryGetValue(s.ShiftCodeId, out var code) ? code : null;

			// Paid hours after the limits.
			var calc = MonthlyHoursCalculator.Calculate(year, month, mine, hours, holidays, limits);
			decimal Paid(HourCategory c) => calc.Single(h => h.Category == c).Paid;

			model.Rows.Add(new ShiftTableRow
			{
				LastName = e.LastName,
				FirstName = e.FirstName,
				Speciality = e.Speciality,
				DayCodes = dayCodes,
				Counts = workCodes.Select(w => mine.Count(s => s.ShiftCodeId == w.Id)).ToArray(),
				DailyToComplete = Paid(HourCategory.ToComplete),
				SundayHolidayToComplete = Paid(HourCategory.Holiday)
			});
		}

		return model;
	}
}
