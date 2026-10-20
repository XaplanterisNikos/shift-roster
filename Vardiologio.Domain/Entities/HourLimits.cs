namespace Vardiologio.Domain.Entities;

/// <summary>
/// Monthly per-employee limits of paid hours, one value per pay category, plus the combined
/// limit of ToComplete + Holiday. Hours above a limit are declared but not paid.
/// Single-row settings table (the app keeps exactly one row), editable by the user.
/// </summary>
public class HourLimits
{
	/// <summary>Primary key (single row).</summary>
	public int Id { get; set; }

	/// <summary>Monthly limit for Προς συμπλήρωση hours (120).</summary>
	public decimal ToCompleteMonthly { get; set; }

	/// <summary>Monthly limit for Απλή overtime (20).</summary>
	public decimal SimpleMonthly { get; set; }

	/// <summary>Monthly limit for Νυχτερινή overtime (30).</summary>
	public decimal NightMonthly { get; set; }

	/// <summary>Monthly limit for Κυριακής overtime (30).</summary>
	public decimal SundayMonthly { get; set; }

	/// <summary>
	/// Holiday-overtime hours allowed per holiday in the month (10): the month's limit is
	/// this value times the number of holidays in that month (weekend holidays included).
	/// </summary>
	public decimal HolidayPerHoliday { get; set; }

	/// <summary>
	/// Combined monthly limit for Προς συμπλήρωση + Αργίας (138). When exceeded,
	/// Προς συμπλήρωση hours are cut first.
	/// </summary>
	public decimal ToCompletePlusHolidayMonthly { get; set; }
}
