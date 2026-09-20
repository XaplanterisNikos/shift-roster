namespace Vardiologio.Application.Reports;

/// <summary>Builds the monthly roster report model (the Σ.Ω. summary sheet).</summary>
public interface IReportService
{
	/// <summary>Aggregates all employees' shift entries for a month into a ReportModel.</summary>
	Task<ReportModel> BuildMonthlyRosterAsync(int year, int month);
}