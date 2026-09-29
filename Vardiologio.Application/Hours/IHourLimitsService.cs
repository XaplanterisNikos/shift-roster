using System.ComponentModel.DataAnnotations;

namespace Vardiologio.Application.Hours;

/// <summary>Editable monthly limits (mirrors the single HourLimits row).</summary>
public class HourLimitsDetail
{
	// Range mirrors decimal(5,2) in AppDbContext (max 999.99); negative limits make no sense.
	private const string RangeMessage = "Επιτρέπονται τιμές από 0 έως 999,99.";

	/// <summary>Monthly limit for Προς συμπλήρωση.</summary>
	[Range(0, 999.99, ErrorMessage = RangeMessage)]
	public decimal ToCompleteMonthly { get; set; }

	/// <summary>Monthly limit for Απλή.</summary>
	[Range(0, 999.99, ErrorMessage = RangeMessage)]
	public decimal SimpleMonthly { get; set; }

	/// <summary>Monthly limit for Νυχτερινή.</summary>
	[Range(0, 999.99, ErrorMessage = RangeMessage)]
	public decimal NightMonthly { get; set; }

	/// <summary>Monthly limit for Κυριακής.</summary>
	[Range(0, 999.99, ErrorMessage = RangeMessage)]
	public decimal SundayMonthly { get; set; }

	/// <summary>Αργίας hours allowed per holiday of the month.</summary>
	[Range(0, 999.99, ErrorMessage = RangeMessage)]
	public decimal HolidayPerHoliday { get; set; }

	/// <summary>Combined monthly limit for Προς συμπλήρωση + Αργίας.</summary>
	[Range(0, 999.99, ErrorMessage = RangeMessage)]
	public decimal ToCompletePlusHolidayMonthly { get; set; }
}

/// <summary>Reads and updates the monthly limits (single-row settings).</summary>
public interface IHourLimitsService
{
	/// <summary>The current limits. Throws InvalidOperationException if the row is missing (seeded at startup).</summary>
	Task<HourLimitsDetail> GetAsync();

	/// <summary>Saves the limits.</summary>
	Task UpdateAsync(HourLimitsDetail detail);
}
