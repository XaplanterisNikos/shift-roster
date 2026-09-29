namespace Vardiologio.Application.Reports;

/// <summary>Renders the monthly reports (Σ.Ω. summary, hours report) to PDF bytes.</summary>
public interface IPdfReportRenderer
{
	/// <summary>Produces the PDF file bytes for the Σ.Ω. report model.</summary>
	byte[] Render(ReportModel model);

	/// <summary>Produces the PDF file bytes for the monthly hours report.</summary>
	byte[] Render(HoursReportModel model);
}