namespace Vardiologio.Application.Files;

/// <summary>
/// Saves exported report files in the user's Documents folder, one folder per report and one
/// sub-folder per month: Documents\Vardiologio\Reports\&lt;report&gt;\&lt;yyyy-MM Month&gt;\.
/// Folder and file names use Latin characters only.
/// </summary>
public interface IReportFileStore
{
	/// <summary>
	/// Writes the file (replacing an older one with the same name) and returns its full path.
	/// </summary>
	/// <param name="folder">The report the file belongs to.</param>
	/// <param name="year">Year of the reported month (picks the month folder).</param>
	/// <param name="month">Reported month, 1–12 (picks the month folder).</param>
	/// <param name="fileName">File name with extension, Latin characters only (e.g. "Monthly_Hours_2026_08.pdf").</param>
	/// <param name="content">The file bytes.</param>
	Task<string> SaveAsync(ReportFolder folder, int year, int month, string fileName, byte[] content);
}
