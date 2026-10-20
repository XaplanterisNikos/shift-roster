using Vardiologio.Application.Reports;
using Vardiologio.Application.ReportTexts;

namespace Vardiologio.Infrastructure.Reports;

/// <summary>
/// Texts of the collective overtime certificate per contract ("ΙΔΟΧ / ΙΔΑΧ / Μόνιμοι"). Its top
/// (letterhead, titles, introduction) and its hour columns are those of the individual
/// certificate, read through <see cref="OvertimeCertificateTexts"/> with this form's own
/// editable texts; this class adds what only the collective form has: the bands of the table
/// and the way a row's hours are printed. Read through here so the Excel and the PDF renderer
/// always print the same wording.
/// </summary>
internal static class ContractCertificateTexts
{
	/// <summary>Header of the speciality column, as on the collective form.</summary>
	public const string HeadSpeciality = "Ειδικότητα";

	/// <summary>Worksheet name of each form.</summary>
	public static string SheetName(ContractCertificateSheet sheet) =>
		sheet == ContractCertificateSheet.FixedTerm ? "ΙΔΟΧ" : "ΜΟΝΙΜΟΙ - ΙΔΑΧ";

	/// <summary>Band above the first group ("ΤΜΗΜΑ: …"); "" when it was left empty (then it is not printed).</summary>
	public static string Department(ReportTextSet texts) => texts.Line(ReportTextBlocks.Department, 0);

	/// <summary>
	/// Band above a group's rows ("ΜΟΝΙΜΟΙ", "ΑΟΡΙΣΤΟΥ ΧΡΟΝΟΥ", "ΟΡΙΣΜΕΝΟΥ ΧΡΟΝΟΥ"): the line of the
	/// "ContractGroups" block at the position of the contract kind; "" when it was left empty.
	/// </summary>
	public static string GroupTitle(ReportTextSet texts, ContractKind kind) =>
		texts.Line(ReportTextBlocks.ContractGroups, (int)kind);

	/// <summary>
	/// The six values of a row in column order, formatted as on the individual certificate
	/// (clock value for the four overtime columns, number for the two "προς συμπλήρωση" ones),
	/// except that a zero is left blank, as on the printed form.
	/// </summary>
	public static string[] Values(ContractCertificateRow r) => new[]
	{
		Clock(r.AfternoonOvertime),
		Clock(r.NightOvertime),
		Clock(r.SundayDayOvertime),
		Clock(r.SundayNightOvertime),
		Number(r.WeekdayNightToComplete),
		Number(r.SundayHolidayToComplete)
	};

	/// <summary>Hours as a clock value ("20:00", "25:30"); "" for zero.</summary>
	private static string Clock(decimal hours) => hours == 0 ? "" : OvertimeCertificateTexts.Clock(hours);

	/// <summary>Hours as a number ("36", "119,5"); "" for zero.</summary>
	private static string Number(decimal hours) => hours == 0 ? "" : OvertimeCertificateTexts.Number(hours);
}
