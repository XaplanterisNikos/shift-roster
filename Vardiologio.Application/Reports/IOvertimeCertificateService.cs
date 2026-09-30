namespace Vardiologio.Application.Reports;

/// <summary>Builds the individual overtime certificate ("Βεβαίωση ατομική").</summary>
public interface IOvertimeCertificateService
{
	/// <summary>
	/// Calculates one employee's paid hours for the month (same calculation and limits as the
	/// hours report). Throws <see cref="InvalidOperationException"/> when the employee is not found.
	/// </summary>
	Task<OvertimeCertificateModel> BuildAsync(int employeeId, int year, int month);
}
