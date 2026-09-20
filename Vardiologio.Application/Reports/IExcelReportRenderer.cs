namespace Vardiologio.Application.Reports;

/// <summary>Renders a <see cref="ReportModel"/> (Σ.Ω. summary) to .xlsx bytes.</summary>
public interface IExcelReportRenderer
{
	/// <summary>Produces the Excel file bytes for the given report model.</summary>
	byte[] Render(ReportModel model);
}