namespace Vardiologio.Application.Reports;

/// <summary>Renders a <see cref="ReportModel"/> (Σ.Ω. summary) to PDF bytes.</summary>
public interface IPdfReportRenderer
{
	/// <summary>Produces the PDF file bytes for the given report model.</summary>
	byte[] Render(ReportModel model);
}