namespace Vardiologio.Domain.Enums;

/// <summary>
/// Pay category that a shift's hours are counted in. Each category has its own monthly
/// limit (see <see cref="Entities.HourLimits"/>); hours above a limit are not paid.
/// </summary>
public enum HourCategory
{
	/// <summary>Προς συμπλήρωση: night hours (22:00–06:00) inside the regular schedule.</summary>
	ToComplete = 1,

	/// <summary>Απλή: simple overtime on a weekday or a Saturday.</summary>
	Simple = 2,

	/// <summary>Νυχτερινή: night overtime.</summary>
	Night = 3,

	/// <summary>Κυριακής: Sunday overtime.</summary>
	Sunday = 4,

	/// <summary>Αργίας: holiday overtime.</summary>
	Holiday = 5
}
