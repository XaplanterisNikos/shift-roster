namespace Vardiologio.Application.Reports;

/// <summary>Builds the monthly staff attendance reports (staff and directorate).</summary>
public interface IAttendanceReportService
{
	/// <summary>
	/// Builds one of the two attendance reports for the month. The directorate report holds the
	/// head of the directorate (see <see cref="PresenceRules.IsDirector"/>); the staff report
	/// holds every other active employee.
	/// </summary>
	Task<AttendanceReportModel> BuildAsync(int year, int month, AttendanceSheet sheet);
}
