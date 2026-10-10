using Vardiologio.Application.Activity;
using Vardiologio.Application.Files;

namespace Vardiologio.Infrastructure.Files;

/// <summary>
/// File-system implementation of <see cref="IReportFileStore"/> (folders from <see cref="AppFolders"/>).
/// Every report file goes through here, so this is also where exports enter the activity log.
/// </summary>
public class ReportFileStore : IReportFileStore
{
	private readonly IActivityLog _activity;
	public ReportFileStore(IActivityLog activity) => _activity = activity;

	/// <inheritdoc/>
	public async Task<string> SaveAsync(ReportFolder folder, int year, int month, string fileName, byte[] content)
	{
		var path = Path.Combine(AppFolders.ReportMonth(folder, year, month), fileName);
		await File.WriteAllBytesAsync(path, content);   // replaces an older export of the same month

		await _activity.RecordAsync("Εξαγωγή αναφοράς", ReportName(folder), $"{month:00}/{year:0000} · {fileName}");
		return path;
	}

	/// <summary>Name of each report, as shown in the activity log.</summary>
	private static string ReportName(ReportFolder folder) => folder switch
	{
		ReportFolder.MonthlyTotal => "Συνολική μηνιαία Σ.Ω.",
		ReportFolder.MonthlyHours => "Ωρών μηνιαία",
		ReportFolder.StaffAttendance => "Παρουσιών προσωπικού",
		ReportFolder.OvertimeCertificate => "Ατομική βεβαίωση",
		ReportFolder.ShiftTable => "Πίνακας βαρδιών εργασίας",
		ReportFolder.IndividualSheet => "Ατομική μηνιαία",
		ReportFolder.ContractCertificate => "ΙΔΟΧ / ΙΔΑΧ / Μόνιμοι",
		_ => folder.ToString()
	};
}
