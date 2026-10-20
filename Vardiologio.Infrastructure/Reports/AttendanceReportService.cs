using Microsoft.EntityFrameworkCore;
using Vardiologio.Application.Reports;
using Vardiologio.Domain.Enums;
using Vardiologio.Infrastructure.Persistence;
using Vardiologio.Infrastructure.ReportTexts;

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
				Position = e.WorkPosition != null ? e.WorkPosition.Name : null,
				ContractCode = e.EmploymentType != null ? e.EmploymentType.Code : null,
				ContractName = e.EmploymentType != null ? e.EmploymentType.Name : null
			})
			.ToListAsync();

		// The head of the directorate goes to its own report (by position, or by surname as a fallback).
		var anyoneHasPosition = all.Any(e => PresenceRules.IsDirectorPosition(e.Position));

		// Everyone else is split by contract: fixed-term staff have their own form; permanent and
		// open-ended staff share one, which also takes anyone whose contract is not recognised.
		var employees = all
			.Where(e => SheetOf(
				PresenceRules.IsDirector(e.LastName, e.Position, anyoneHasPosition),
				ContractRules.Resolve(e.ContractCode, e.ContractName)) == sheet)
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

		// Each attendance report is its own form, with its own titles and signatures.
		model.Texts = await ReportTextService.LoadSetAsync(db, sheet switch
		{
			AttendanceSheet.Directorate => ReportForm.AttendanceDirectorate,
			AttendanceSheet.FixedTerm => ReportForm.AttendanceFixedTerm,
			_ => ReportForm.AttendanceStaff
		});

		// Staff report only: who is printed there for lack of a recognised contract.
		if (sheet == AttendanceSheet.Staff)
		{
			model.WithoutContract = employees
				.Where(e => ContractRules.Resolve(e.ContractCode, e.ContractName) is null)
				.Select(e => $"{e.LastName.Trim()} {e.FirstName.Trim()}".Trim())
				.ToList();
		}

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

	/// <summary>The report an employee is printed on.</summary>
	/// <param name="isDirector">True for the head of the directorate (see <see cref="PresenceRules.IsDirector"/>).</param>
	/// <param name="contract">Kind of the employee's contract, or null when it is missing or not recognised.</param>
	private static AttendanceSheet SheetOf(bool isDirector, ContractKind? contract)
	{
		if (isDirector) return AttendanceSheet.Directorate;
		return contract == ContractKind.FixedTerm ? AttendanceSheet.FixedTerm : AttendanceSheet.Staff;
	}
}
