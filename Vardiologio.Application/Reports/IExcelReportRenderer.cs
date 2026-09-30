namespace Vardiologio.Application.Reports;

/// <summary>Renders the monthly reports (Σ.Ω. summary, hours report) to .xlsx bytes.</summary>
public interface IExcelReportRenderer
{
	/// <summary>Produces the Excel file bytes for the Σ.Ω. report model.</summary>
	byte[] Render(ReportModel model);

	/// <summary>Produces the Excel file bytes for the monthly hours report.</summary>
	byte[] Render(HoursReportModel model);

	/// <summary>Produces the Excel file bytes for one attendance report (staff or directorate), laid out as the printed pages.</summary>
	byte[] Render(AttendanceReportModel model);

	/// <summary>Produces the Excel file bytes for one employee's overtime certificate (Βεβαίωση ατομική).</summary>
	byte[] Render(OvertimeCertificateModel model);

	/// <summary>Produces the Excel file bytes for the official shift table (one A3 landscape page).</summary>
	byte[] Render(ShiftTableModel model);
}