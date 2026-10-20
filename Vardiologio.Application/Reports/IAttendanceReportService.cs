namespace Vardiologio.Application.Reports;

/// <summary>Builds the monthly staff attendance reports (staff, fixed-term staff and directorate).</summary>
public interface IAttendanceReportService
{
	/// <summary>
	/// Builds one of the three attendance reports for the month. The directorate report holds the
	/// head of the directorate (see <see cref="PresenceRules.IsDirector"/>); of the other active
	/// employees, the fixed-term report holds those on a fixed-term contract (see
	/// <see cref="ContractRules.Resolve"/>) and the staff report holds everyone else.
	/// </summary>
	Task<AttendanceReportModel> BuildAsync(int year, int month, AttendanceSheet sheet);
}
