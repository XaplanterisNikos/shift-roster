using Vardiologio.Application.Reports;
using Microsoft.EntityFrameworkCore;              
using Vardiologio.Infrastructure.Persistence;     

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
			.Select(s => new { s.EmployeeId, s.Date, s.ShiftCode.Code })
			.ToListAsync();

		var byEmp = entries.ToLookup(x => x.EmployeeId);

		var model = new ReportModel { Year = year, Month = month, DaysInMonth = days };
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