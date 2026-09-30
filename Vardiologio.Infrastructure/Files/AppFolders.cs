using System.Globalization;
using Vardiologio.Application.Files;

namespace Vardiologio.Infrastructure.Files;

/// <summary>
/// Single source of truth for the folders the app writes to in the user's Documents
/// (the database stays in %LOCALAPPDATA%\Vardiologio, see <c>AppPaths</c>). Latin names only:
/// <code>
/// Documents\Vardiologio\
///   Logs\                                   daily error logs (yyyy-MM-dd.log)
///   Reports\&lt;report&gt;\&lt;yyyy-MM Month&gt;\   exported reports, e.g. Reports\Monthly-Hours\2026-08 August\
/// </code>
/// Every property / method creates the folder if it does not exist yet.
/// </summary>
public static class AppFolders
{
	/// <summary>Documents\Vardiologio.</summary>
	public static string Root => Ensure(Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Vardiologio"));

	/// <summary>Documents\Vardiologio\Logs.</summary>
	public static string Logs => Ensure(Path.Combine(Root, "Logs"));

	/// <summary>Documents\Vardiologio\Reports.</summary>
	public static string Reports => Ensure(Path.Combine(Root, "Reports"));

	/// <summary>
	/// Month folder of one report, e.g. Reports\Staff-Attendance\2026-08 August.
	/// "yyyy-MM" first so the months sort in order in Explorer.
	/// </summary>
	public static string ReportMonth(ReportFolder folder, int year, int month)
	{
		var monthName = CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(month);   // "August"
		return Ensure(Path.Combine(Reports, FolderName(folder), $"{year:0000}-{month:00} {monthName}"));
	}

	/// <summary>Folder name of each report.</summary>
	private static string FolderName(ReportFolder folder) => folder switch
	{
		ReportFolder.MonthlyTotal => "Monthly-Total",
		ReportFolder.MonthlyHours => "Monthly-Hours",
		ReportFolder.StaffAttendance => "Staff-Attendance",
		ReportFolder.OvertimeCertificate => "Overtime-Certificate",
		ReportFolder.ShiftTable => "Shift-Table",
		_ => throw new ArgumentOutOfRangeException(nameof(folder), folder, null)
	};

	/// <summary>Creates the folder when missing and returns its path.</summary>
	private static string Ensure(string path)
	{
		Directory.CreateDirectory(path);
		return path;
	}
}
