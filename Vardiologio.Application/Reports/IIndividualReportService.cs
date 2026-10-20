namespace Vardiologio.Application.Reports;

/// <summary>Builds the individual report model (employee).</summary>
public interface IIndividualReportService
{
	/// <summary>Builds the analytical sheet for one employee and month.</summary>
	Task<IndividualReportModel> BuildAsync(int employeeId, int year, int month);
}