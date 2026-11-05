namespace Vardiologio.Application.DatabaseTransfer;

/// <summary>
/// Rule of the weekly database-export reminder: every Friday an export is due, and it stays due
/// (Saturday, Monday, ...) until an export is made; then nothing is due until the next Friday.
/// </summary>
public static class ExportReminder
{
	/// <summary>Action written to the activity log for every database export; the reminder looks for it.</summary>
	public const string ExportAction = "Εξαγωγή βάσης";

	/// <summary>The most recent Friday, at midnight: today when today is a Friday.</summary>
	/// <param name="now">Local date and time.</param>
	public static DateTime LastFriday(DateTime now) =>
		now.Date.AddDays(-(((int)now.DayOfWeek - (int)DayOfWeek.Friday + 7) % 7));

	/// <summary>
	/// True when no export has been made since the most recent Friday began (also when there has
	/// never been one).
	/// </summary>
	/// <param name="lastExport">Local time of the latest export; null when there has been none.</param>
	/// <param name="now">Local date and time.</param>
	public static bool IsDue(DateTime? lastExport, DateTime now) =>
		lastExport is not { } last || last < LastFriday(now);
}
