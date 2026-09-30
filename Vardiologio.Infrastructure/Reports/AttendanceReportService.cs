using Microsoft.EntityFrameworkCore;
using Vardiologio.Application.Reports;
using Vardiologio.Infrastructure.Persistence;

namespace Vardiologio.Infrastructure.Reports;

/// <summary>EF Core implementation of <see cref="IAttendanceReportService"/>.</summary>
public class AttendanceReportService : IAttendanceReportService
{
	private readonly IDbContextFactory<AppDbContext> _factory;
	public AttendanceReportService(IDbContextFactory<AppDbContext> factory) => _factory = factory;

	/// <inheritdoc/>
	public async Task<AttendanceReportModel> BuildAsync(int year, int month, AttendanceSheet sheet)
	{
		var first = new DateOnly(year, month, 1);
		var last = new DateOnly(year, month, DateTime.DaysInMonth(year, month));

		await using var db = await _factory.CreateDbContextAsync();

		// Active employees only (global filter), same order as the Σ.Ω. report.
		var all = await db.Employees
			.OrderBy(e => e.LastName).ThenBy(e => e.FirstName)
			.Select(e => new
			{
				e.Id,
				e.LastName,
				e.FirstName,
				Speciality = e.Speciality.Name,
				Position = e.WorkPosition != null ? e.WorkPosition.Name : null
			})
			.ToListAsync();

		// The head of the directorate goes to its own report (by position, or by surname as a fallback).
		var anyoneHasPosition = all.Any(e => PresenceRules.IsDirectorPosition(e.Position));
		var wantDirector = sheet == AttendanceSheet.Directorate;
		var employees = all
			.Where(e => PresenceRules.IsDirector(e.LastName, e.Position, anyoneHasPosition) == wantDirector)
			.ToList();
		var ids = employees.Select(e => e.Id).ToList();

		// The month's entries of these employees, with what the code means (work shift or status).
		var entries = await db.ShiftDays
			.AsNoTracking()
			.Where(s => ids.Contains(s.EmployeeId) && s.Date >= first && s.Date <= last)
			.Select(s => new { s.EmployeeId, s.Date, s.ShiftCode.Code, IsWorkShift = s.ShiftCode.StartTime != null })
			.ToListAsync();
		var byEmployeeAndDate = entries.ToDictionary(s => (s.EmployeeId, s.Date));

		var model = new AttendanceReportModel { Year = year, Month = month, Sheet = sheet };

		foreach (var e in employees)
		{
			// One mark per day; a day without an entry stays empty (holidays included).
			var marks = new string[model.DaysInMonth];
			for (var d = 1; d <= marks.Length; d++)
			{
				marks[d - 1] = byEmployeeAndDate.TryGetValue((e.Id, new DateOnly(year, month, d)), out var s)
					? PresenceRules.Mark(s.Code, s.IsWorkShift)
					: "";
			}

			model.Rows.Add(new AttendanceReportRow
			{
				LastName = e.LastName,
				FirstName = e.FirstName,
				Speciality = e.Speciality,
				Marks = marks
			});
		}

		return model;
	}
}
