using Vardiologio.Application.Reports;
using Microsoft.EntityFrameworkCore;
using Vardiologio.Application.Hours;
using Vardiologio.Domain.Enums;
using Vardiologio.Infrastructure.Persistence;
using Vardiologio.Infrastructure.ReportColors;
using Vardiologio.Infrastructure.ReportTexts;

namespace Vardiologio.Infrastructure.Reports;

/// <summary>EF Core implementation of <see cref="IReportService"/>.</summary>
public class ReportService : IReportService
{
	private readonly IDbContextFactory<AppDbContext> _factory;
	public ReportService(IDbContextFactory<AppDbContext> factory) => _factory = factory;

	/// <inheritdoc/>
	public async Task<ReportModel> BuildMonthlyRosterAsync(int year, int month)
	{
		var first = new DateOnly(year, month, 1);
		var days = DateTime.DaysInMonth(year, month);
		var last = new DateOnly(year, month, days);

		await using var db = await _factory.CreateDbContextAsync();

		var employees = (await db.Employees
				.OrderBy(e => e.LastName).ThenBy(e => e.FirstName)
				.Select(e => new
				{
					e.Id,
					e.LastName,
					e.FirstName,
					Speciality = e.Speciality.Name,
					ContractCode = e.EmploymentType != null ? e.EmploymentType.Code : null,
					ContractName = e.EmploymentType != null ? e.EmploymentType.Name : null
				})
				.ToListAsync())
			.Select(e => new
			{
				e.Id,
				e.LastName,
				e.FirstName,
				e.Speciality,
				Contract = ContractRules.Resolve(e.ContractCode, e.ContractName)
			})
			.OrderBy(e => e.Contract ?? ContractKind.Permanent)
			.ToList();

		var entries = await db.ShiftDays
			.AsNoTracking()
			.Where(s => s.Date >= first && s.Date <= last)
			.ToListAsync();

		var usedIds = entries.Select(x => x.ShiftCodeId).Distinct().ToList();
		var codes = await db.ShiftCodes
			.IgnoreQueryFilters()
			.AsNoTracking()
			.Where(c => c.IsActive || usedIds.Contains(c.Id))
			.ToListAsync();
		var codeById = codes.ToDictionary(c => c.Id, c => c.Code);

		var workCodes = codes
			.Where(c => c.StartTime != null)
			.OrderBy(c => int.TryParse(c.Code, out var n) ? n : int.MaxValue).ThenBy(c => c.Code)
			.ToList();
		var statusCodes = codes.Where(c => c.StartTime == null).OrderBy(c => c.Id).ToList();

		var hours = await db.ShiftCodeHours.AsNoTracking().ToListAsync();
		var holidays = (await db.Holidays
			.Where(h => h.Date >= first && h.Date <= last)
			.Select(h => h.Date)
			.ToListAsync())
			.ToHashSet();
		var limits = await db.HourLimits.AsNoTracking().OrderBy(l => l.Id).FirstOrDefaultAsync()
			?? throw new InvalidOperationException("HourLimits row not found.");

		var byEmp = entries.ToLookup(x => x.EmployeeId);

		var model = new ReportModel
		{
			Year = year,
			Month = month,
			DaysInMonth = days,
			WorkCodes = workCodes.Select(c => (c.Code, c.Description)).ToList(),
			StatusCodes = statusCodes.Select(c => (c.Code, c.Description)).ToList(),
			WithoutContract = employees
				.Where(e => e.Contract is null)
				.Select(e => $"{e.LastName.Trim()} {e.FirstName.Trim()}".Trim())
				.ToList(),
			Texts = await ReportTextService.LoadSetAsync(db, ReportForm.MonthlyTotal),
			Colors = await ReportColorService.LoadSetAsync(db)
		};
		var idx = 1;
		foreach (var e in employees)
		{
			var mine = byEmp[e.Id].ToList();

			var dayCodes = new string?[days];
			foreach (var s in mine)
				dayCodes[s.Date.Day - 1] = codeById.TryGetValue(s.ShiftCodeId, out var code) ? code : null;

			var calc = MonthlyHoursCalculator.Calculate(year, month, mine, hours, holidays, limits);
			decimal Paid(HourCategory c) => calc.Single(h => h.Category == c).Paid;

			model.Rows.Add(new ReportRow
			{
				Index = idx++,
				LastName = e.LastName,
				FirstName = e.FirstName,
				Speciality = e.Speciality,
				Group = e.Contract ?? ContractKind.Permanent,
				DayCodes = dayCodes,
				ProgramCounts = workCodes.Select(w => mine.Count(s => s.ShiftCodeId == w.Id)).ToArray(),
				DailyToComplete = Paid(HourCategory.ToComplete),
				SundayHolidayToComplete = Paid(HourCategory.Holiday)
			});
		}
		return model;
	}
}