namespace Vardiologio.Application.Reports;

/// <summary>Renders the monthly reports (Σ.Ω. summary, hours report) to .xlsx bytes.</summary>
public interface IExcelReportRenderer
{
	/// <summary>Produces the Excel file bytes for the Σ.Ω. report model.</summary>
	byte[] Render(ReportModel model);

	/// <summary>Produces the Excel file bytes for the monthly hours report.</summary>
	byte[] Render(HoursReportModel model);
}