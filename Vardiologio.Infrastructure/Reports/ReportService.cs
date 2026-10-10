using Vardiologio.Application.Reports;
using Microsoft.EntityFrameworkCore;              
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

		var employees = await db.Employees
			.OrderBy(e => e.LastName).ThenBy(e => e.FirstName)
			.Select(e => new { e.Id, e.LastName, e.FirstName, Speciality = e.Speciality.Name })
			.ToListAsync();

		var entries = await db.ShiftDays
			.Where(s => s.Date >= first && s.Date <= last)
			.Select(s => new { s.EmployeeId, s.Date, s.ShiftCodeId, s.ShiftCode.Code })
			.ToListAsync();

		// Legend: active codes, plus retired ones still used this month (as on the shift table).
		var usedIds = entries.Select(x => x.ShiftCodeId).Distinct().ToList();
		var codes = await db.ShiftCodes
			.IgnoreQueryFilters()
			.AsNoTracking()
			.Where(c => c.IsActive || usedIds.Contains(c.Id))
			.ToListAsync();
		var workCodes = codes
			.Where(c => c.StartTime != null)
			.OrderBy(c => int.TryParse(c.Code, out var n) ? n : int.MaxValue).ThenBy(c => c.Code)
			.ToList();
		var statusCodes = codes.Where(c => c.StartTime == null).OrderBy(c => c.Id).ToList();

		var byEmp = entries.ToLookup(x => x.EmployeeId);

		var model = new ReportModel
		{
			Year = year,
			Month = month,
			DaysInMonth = days,
			WorkCodes = workCodes.Select(c => (c.Code, c.Description)).ToList(),
			StatusCodes = statusCodes.Select(c => (c.Code, c.Description)).ToList(),
			Texts = await ReportTextService.LoadSetAsync(db, ReportForm.MonthlyTotal),
			Colors = await ReportColorService.LoadSetAsync(db)
		};
		var idx = 1;
		foreach (var e in employees)
		{
			var row = new ReportRow
			{
				Index = idx++,
				LastName = e.LastName,
				FirstName = e.FirstName,
				Speciality = e.Speciality,
				DayCodes = new string?[days],
				ProgramCounts = new int[7],
			};

			foreach (var ent in byEmp[e.Id])
			{
				row.DayCodes[ent.Date.Day - 1] = ent.Code;// place the code in its day slot
				// count how many times each program 1..7 was worked (matches the Σ.Ω. count block)
				for (var k = 1; k <= 7; k++)
					if (ent.Code == k.ToString()) row.ProgramCounts[k - 1]++;
			}
			model.Rows.Add(row);
		}
		return model;
	}
}