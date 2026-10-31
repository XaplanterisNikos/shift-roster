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
	public async Task<ContractCertificateModel> BuildAsync(int year, int month)
	{
		var first = new DateOnly(year, month, 1);
		var last = new DateOnly(year, month, DateTime.DaysInMonth(year, month));

		await using var db = await _factory.CreateDbContextAsync();

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

		var anyoneHasPosition = all.Any(e => PresenceRules.IsDirectorPosition(e.Position));
		var staff = all
			.Where(e => !PresenceRules.IsDirector(e.LastName, e.Position, anyoneHasPosition))
			.Select(e => new { Employee = e, Kind = ContractRules.Resolve(e.ContractCode, e.ContractName) })
			.ToList();

		var entries = await db.ShiftDays
			.AsNoTracking()
			.Where(s => s.Date >= first && s.Date <= last)
			.ToListAsync();
		var byEmployee = entries.ToLookup(s => s.EmployeeId);

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
			Texts = await ReportTextService.LoadSetAsync(db, ReportForm.ContractCertificate),
			Colors = await ReportColorService.LoadSetAsync(db),
			WithoutContract = staff
				.Where(x => x.Kind is null)
				.Select(x => $"{x.Employee.LastName.Trim()} {x.Employee.FirstName.Trim()}".Trim())
				.ToList()
		};

		var index = 1;
		foreach (var kind in ContractRules.PrintOrder)
		{
			var members = staff.Where(x => x.Kind == kind).Select(x => x.Employee).ToList();
			if (members.Count == 0) continue;

			var group = new ContractCertificateGroup { Kind = kind };
			foreach (var e in members)
			{
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
