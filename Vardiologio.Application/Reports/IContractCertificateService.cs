namespace Vardiologio.Application.Reports;

/// <summary>Builds the collective overtime certificate per contract ("ΙΔΟΧ / ΙΔΑΧ / Μόνιμοι").</summary>
public interface IContractCertificateService
{
	/// <summary>
	/// Calculates the paid hours of the month for every active employee of the three contracts
	/// (same calculation and limits as the hours report and the individual certificate).
	/// The head of the directorate is left out (see <see cref="PresenceRules.IsDirector"/>).
	/// </summary>
	/// <param name="year">Certificate year.</param>
	/// <param name="month">Certificate month (1–12).</param>
	Task<ContractCertificateModel> BuildAsync(int year, int month);
}
