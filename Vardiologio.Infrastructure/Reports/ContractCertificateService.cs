using Microsoft.EntityFrameworkCore;
using Vardiologio.Application.Hours;
using Vardiologio.Application.Reports;
using Vardiologio.Domain.Enums;
using Vardiologio.Infrastructure.Persistence;
using Vardiologio.Infrastructure.ReportColors;
using Vardiologio.Infrastructure.ReportTexts;

namespace Vardiologio.Infrastructure.Reports;

/// <summary>EF Core implementation of <see cref="IContractCertificateService"/>.</summary>
public class ContractCertificateService : IContractCertificateService
{
	private readonly IDbContextFactory<AppDbContext> _factory;
	public ContractCertificateService(IDbContextFactory<AppDbContext> factory) => _factory = factory;

	/// <inheritdoc/>
	public async Task<ContractCertificateModel> BuildAsync(int year, int month, ContractCertificateSheet sheet)
	{
		var first = new DateOnly(year, month, 1);
		var last = new DateOnly(year, month, DateTime.DaysInMonth(year, month));

		await using var db = await _factory.CreateDbContextAsync();

		// Active employees only (global filter), in the order of the other reports, each with
		// the contract and the work position the grouping rules need.
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

		// The head of the directorate is not on these forms (same rule as the attendance report).
		var anyoneHasPosition = all.Any(e => PresenceRules.IsDirectorPosition(e.Position));
		var staff = all
			.Where(e => !PresenceRules.IsDirector(e.LastName, e.Position, anyoneHasPosition))
			.Select(e => new { Employee = e, Kind = ContractRules.Resolve(e.ContractCode, e.ContractName) })
			.ToList();

		// The month's entries, grouped per employee in memory (one query for everyone).
		var entries = await db.ShiftDays
			.AsNoTracking()
			.Where(s => s.Date >= first && s.Date <= last)
			.ToListAsync();
		var byEmployee = entries.ToLookup(s => s.EmployeeId);

		// Rule data shared by every employee — same inputs as the hours report.
		var hours = await db.ShiftCodeHours.AsNoTracking().ToListAsync();
		var holidays = (await db.Holidays
			.Where(h => h.Date >= first && h.Date <= last)
			.Select(h => h.Date)
			.ToListAsync())
			.ToHashSet();
		var limits = await db.HourLimits.AsNoTracking().OrderBy(l => l.Id).FirstOrDefaultAsync()
			?? throw new InvalidOperationException("HourLimits row not found.");

		var model = new ContractCertificateModel
		{
			Year = year,
			Month = month,
			Sheet = sheet,
			Texts = await ReportTextService.LoadSetAsync(db, ReportForm.ContractCertificate),
			Colors = await ReportColorService.LoadSetAsync(db),
			// Employees whose contract is missing or not recognised appear on neither form.
			WithoutContract = staff
				.Where(x => x.Kind is null)
				.Select(x => $"{x.Employee.LastName.Trim()} {x.Employee.FirstName.Trim()}".Trim())
				.ToList()
		};

		// One group per kind of contract of this form; the Α/Α runs on from group to group.
		var index = 1;
		foreach (var kind in ContractRules.KindsOf(sheet))
		{
			var group = new ContractCertificateGroup { Kind = kind };
			foreach (var e in staff.Where(x => x.Kind == kind).Select(x => x.Employee))
			{
				// Same calculation as the hours report, so the row matches its "Πληρ." column.
				var paid = CertificateHours.FromPaid(
					MonthlyHoursCalculator.Calculate(year, month, byEmployee[e.Id], hours, holidays, limits));

				group.Rows.Add(new ContractCertificateRow
				{
					Index = index++,
					LastName = e.LastName,
					FirstName = e.FirstName,
					Speciality = e.Speciality,
					AfternoonOvertime = paid.AfternoonOvertime,
					NightOvertime = paid.NightOvertime,
					SundayDayOvertime = paid.SundayDayOvertime,
					SundayNightOvertime = paid.SundayNightOvertime,
					WeekdayNightToComplete = paid.WeekdayNightToComplete,
					SundayHolidayToComplete = paid.SundayHolidayToComplete
				});
			}
			model.Groups.Add(group);
		}

		return model;
	}
}
