namespace Vardiologio.Application.Reports;

/// <summary>Builds the collective overtime certificates per contract ("ΙΔΟΧ / ΙΔΑΧ / Μόνιμοι").</summary>
public interface IContractCertificateService
{
	/// <summary>
	/// Calculates the paid hours of the month for every active employee of the form's contracts
	/// (same calculation and limits as the hours report and the individual certificate).
	/// The head of the directorate is left out (see <see cref="PresenceRules.IsDirector"/>).
	/// </summary>
	/// <param name="year">Certificate year.</param>
	/// <param name="month">Certificate month (1–12).</param>
	/// <param name="sheet">Which of the two forms to build.</param>
	Task<ContractCertificateModel> BuildAsync(int year, int month, ContractCertificateSheet sheet);
}
