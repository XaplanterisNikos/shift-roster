using Vardiologio.Application.Hours;
using Vardiologio.Domain.Enums;

namespace Vardiologio.Infrastructure.Reports;

/// <summary>
/// The six hour columns of the overtime certificate, taken from an employee's monthly
/// calculation. One mapping for the individual certificate and for the collective one per
/// contract, so a row of the collective form always agrees with the employee's own certificate.
/// </summary>
/// <param name="AfternoonOvertime">Afternoon overtime until 22:00 (paid "simple" hours).</param>
/// <param name="NightOvertime">Night overtime 22:00–06:00 (paid night hours).</param>
/// <param name="SundayDayOvertime">Sunday and holiday overtime by day (paid Sunday hours).</param>
/// <param name="SundayNightOvertime">Sunday and holiday overtime at night (no pay category feeds it yet).</param>
/// <param name="WeekdayNightToComplete">Work "to complete" on working days (paid "to complete" hours).</param>
/// <param name="SundayHolidayToComplete">Work "to complete" on Sundays and holidays (paid holiday hours).</param>
internal readonly record struct CertificateHours(
	decimal AfternoonOvertime,
	decimal NightOvertime,
	decimal SundayDayOvertime,
	decimal SundayNightOvertime,
	decimal WeekdayNightToComplete,
	decimal SundayHolidayToComplete)
{
	/// <summary>
	/// Reads the columns from the result of <see cref="MonthlyHoursCalculator.Calculate"/>:
	/// always the <b>paid</b> hours, i.e. the "Πληρ." column of the monthly hours report.
	/// </summary>
	public static CertificateHours FromPaid(IReadOnlyList<CategoryHours> calc)
	{
		decimal Paid(HourCategory c) => calc.Single(h => h.Category == c).Paid;

		return new CertificateHours(
			AfternoonOvertime: Paid(HourCategory.Simple),
			NightOvertime: Paid(HourCategory.Night),
			SundayDayOvertime: Paid(HourCategory.Sunday),
			SundayNightOvertime: 0,
			WeekdayNightToComplete: Paid(HourCategory.ToComplete),
			SundayHolidayToComplete: Paid(HourCategory.Holiday));
	}
}
