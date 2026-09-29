namespace Vardiologio.Application.Reports;

/// <summary>Builds the monthly hours report (declared / paid / excess per pay category).</summary>
public interface IHoursReportService
{
	/// <summary>Calculates every active employee's hours for the month with the limits applied.</summary>
	Task<HoursReportModel> BuildAsync(int year, int month);
}
