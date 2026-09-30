namespace Vardiologio.Application.Reports;

/// <summary>Renders the monthly reports (Σ.Ω. summary, hours report) to PDF bytes.</summary>
public interface IPdfReportRenderer
{
	/// <summary>Produces the PDF file bytes for the Σ.Ω. report model.</summary>
	byte[] Render(ReportModel model);

	/// <summary>Produces the PDF file bytes for the monthly hours report.</summary>
	byte[] Render(HoursReportModel model);

	/// <summary>Produces the PDF file bytes for one attendance report (staff or directorate).</summary>
	byte[] Render(AttendanceReportModel model);

	/// <summary>Produces the PDF file bytes for one employee's overtime certificate (Βεβαίωση ατομική).</summary>
	byte[] Render(OvertimeCertificateModel model);

	/// <summary>Produces the PDF file bytes for the official shift table (one A3 landscape page).</summary>
	byte[] Render(ShiftTableModel model);
}