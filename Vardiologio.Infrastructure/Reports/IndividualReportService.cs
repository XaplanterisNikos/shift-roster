using Microsoft.EntityFrameworkCore;
using Vardiologio.Application.Parameters;
using Vardiologio.Application.Reports;
using Vardiologio.Domain.Enums;
using Vardiologio.Infrastructure.Persistence;
using Vardiologio.Infrastructure.ReportColors;
using Vardiologio.Infrastructure.ReportTexts;

namespace Vardiologio.Infrastructure.Reports;

/// <summary>EF Core implementation of <see cref="IIndividualReportService"/>.</summary>
public class IndividualReportService : IIndividualReportService
{
	private readonly IDbContextFactory<AppDbContext> _factory;
	public IndividualReportService(IDbContextFactory<AppDbContext> factory) => _factory = factory;

	/// <inheritdoc/>
	public async Task<IndividualReportModel> BuildAsync(int employeeId, int year, int month)
	{
		var first = new DateOnly(year, month, 1);
		var last = new DateOnly(year, month, DateTime.DaysInMonth(year, month));

		await using var db = await _factory.CreateDbContextAsync();

		// Employee name and contract code (ΕΙΔΟΣ ΕΡΓΑΣΙΑΣ, from Παράμετροι → Εργαζομένων → Συμβάσεις).
		var emp = await db.Employees
			.Where(e => e.Id == employeeId)
			.Select(e => new
			{
				e.LastName,
				e.FirstName,
				Contract = e.EmploymentType != null ? e.EmploymentType.Code : null,
				Speciality = e.Speciality.Name,
				e.SignatoryId
			})
			.FirstOrDefaultAsync()
			?? throw new InvalidOperationException($"Employee {employeeId} not found.");

		// The employee's entries of the month.
		var entries = await db.ShiftDays
			.AsNoTracking()
			.Where(s => s.EmployeeId == employeeId && s.Date >= first && s.Date <= last)
			.ToListAsync();

		// The codes those entries use (retired ones included, so their days still print).
		var usedIds = entries.Select(s => s.ShiftCodeId).Distinct().ToList();
		var codes = await db.ShiftCodes
			.IgnoreQueryFilters()
			.AsNoTracking()
			.Where(c => usedIds.Contains(c.Id))
			.ToDictionaryAsync(c => c.Id);

		// Rule data — same inputs as the hours report and the certificate.
		var hours = await db.ShiftCodeHours.AsNoTracking().ToListAsync();
		var holidays = (await db.Holidays
			.Where(h => h.Date >= first && h.Date <= last)
			.Select(h => h.Date)
			.ToListAsync())
			.ToHashSet();
		var limits = await db.HourLimits.AsNoTracking().OrderBy(l => l.Id).FirstOrDefaultAsync()
			?? throw new InvalidOperationException("HourLimits row not found.");

		// All the rules live in the pure builder (Application), next to the calculator.
		var model = IndividualReportBuilder.Build(
			year, month, emp.LastName, emp.FirstName, emp.Contract, entries, codes, hours, holidays, limits);

		// Presentation data, not a rule: added here, the builder stays pure.
		model.Colors = await ReportColorService.LoadSetAsync(db);

		// Official print (Excel / PDF): texts, the employee's own signature, his supervisor and
		// the date at the top right. The page may change the supervisor and the date before it renders.
		model.Texts = await ReportTextService.LoadSetAsync(db, ReportForm.IndividualMonthly);
		model.FirstName = emp.FirstName.Trim();
		model.Speciality = emp.Speciality;
		model.IsFemale = GreekText.IsFemaleName(emp.FirstName, emp.LastName);
		model.SignatoryId = emp.SignatoryId;
		model.IssueDate = first.AddMonths(1);   // 1st of the next month, as on the organisation's sheets

		if (emp.SignatoryId is { } signatoryId)
		{
			var signatory = await db.Signatories.AsNoTracking().FirstOrDefaultAsync(s => s.Id == signatoryId);
			if (signatory is not null)
				model.Supervisor = new IndividualSigner(
					SignatoryItem.SplitLines(signatory.Role), signatory.FullName, signatory.Title);
		}
		return model;
	}
}
