using Microsoft.EntityFrameworkCore;
using Vardiologio.Application.Hours;
using Vardiologio.Application.Reports;
using Vardiologio.Domain.Enums;
using Vardiologio.Infrastructure.Persistence;

namespace Vardiologio.Infrastructure.Reports;

/// <summary>EF Core implementation of <see cref="IOvertimeCertificateService"/>.</summary>
public class OvertimeCertificateService : IOvertimeCertificateService
{
	private readonly IDbContextFactory<AppDbContext> _factory;
	public OvertimeCertificateService(IDbContextFactory<AppDbContext> factory) => _factory = factory;

	/// <inheritdoc/>
	public async Task<OvertimeCertificateModel> BuildAsync(int employeeId, int year, int month)
	{
		var first = new DateOnly(year, month, 1);
		var last = new DateOnly(year, month, DateTime.DaysInMonth(year, month));

		await using var db = await _factory.CreateDbContextAsync();

		// Active employees in the order of the other reports: the position gives the Α/Α.
		var employees = await db.Employees
			.OrderBy(e => e.LastName).ThenBy(e => e.FirstName)
			.Select(e => new { e.Id, e.LastName, e.FirstName, Speciality = e.Speciality.Name })
			.ToListAsync();
		var index = employees.FindIndex(e => e.Id == employeeId);
		if (index < 0) throw new InvalidOperationException($"Employee {employeeId} not found.");
		var emp = employees[index];

		// The employee's entries of the month and the rule data — same inputs as the hours report.
		var entries = await db.ShiftDays
			.AsNoTracking()
			.Where(s => s.EmployeeId == employeeId && s.Date >= first && s.Date <= last)
			.ToListAsync();
		var hours = await db.ShiftCodeHours.AsNoTracking().ToListAsync();
		var holidays = (await db.Holidays
			.Where(h => h.Date >= first && h.Date <= last)
			.Select(h => h.Date)
			.ToListAsync())
			.ToHashSet();
		var limits = await db.HourLimits.AsNoTracking().OrderBy(l => l.Id).FirstOrDefaultAsync()
			?? throw new InvalidOperationException("HourLimits row not found.");

		// Same calculation as the hours report, so the certificate always matches its "Πληρ." column.
		var calc = MonthlyHoursCalculator.Calculate(year, month, entries, hours, holidays, limits);
		decimal Paid(HourCategory c) => calc.Single(h => h.Category == c).Paid;

		return new OvertimeCertificateModel
		{
			Year = year,
			Month = month,
			Index = index + 1,
			LastName = emp.LastName,
			FirstName = emp.FirstName,
			Speciality = emp.Speciality,
			IsFemale = GreekText.IsFemaleName(emp.FirstName, emp.LastName),
			AfternoonOvertime = Paid(HourCategory.Simple),
			NightOvertime = Paid(HourCategory.Night),
			SundayDayOvertime = Paid(HourCategory.Sunday),
			SundayNightOvertime = 0,   // no pay category feeds this column yet
			WeekdayNightToComplete = Paid(HourCategory.ToComplete),
			SundayHolidayToComplete = Paid(HourCategory.Holiday)
		};
	}
}
