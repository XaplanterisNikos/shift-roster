using Vardiologio.Application.ReportColors;
using Vardiologio.Application.ReportTexts;

namespace Vardiologio.Application.Reports;

/// <summary>
/// Data for one collective "ΠΙΣΤΟΠΟΙΗΣΗ ΥΠΕΡΩΡΙΑΚΗΣ ΚΑΙ ΛΟΙΠΗΣ ΕΡΓΑΣΙΑΣ" per contract: the same six
/// hour columns as the individual certificate, one row per employee, grouped by contract.
/// The hours are the <b>paid</b> hours, so every row agrees with that employee's own certificate.
/// One form for all three contracts: permanent, then open-ended (ΙΔΑΧ), then fixed-term (ΙΔΟΧ),
/// one under the other, each under its own band.
/// </summary>
public class ContractCertificateModel
{
	/// <summary>Certificate year.</summary>
	public int Year { get; set; }

	/// <summary>Certificate month (1–12).</summary>
	public int Month { get; set; }

	/// <summary>
	/// The contract groups of the form, in print order (permanent, open-ended, fixed-term).
	/// A contract without employees this month has no group, so its band is not printed.
	/// </summary>
	public List<ContractCertificateGroup> Groups { get; set; } = new();

	/// <summary>
	/// Active employees left out of the form because their contract is missing or is not one
	/// of the three kinds ("Επώνυμο Όνομα"). The page lists them so the user can fix the contract.
	/// </summary>
	public List<string> WithoutContract { get; set; } = new();

	/// <summary>The form's editable texts (letterhead, titles, bands, signatures…), as set in Παράμετροι → Αναφορών.</summary>
	public ReportTextSet Texts { get; set; } = ReportTextSet.Empty;

	/// <summary>The reports' colours (Παράμετροι → Αναφορών → Χρώματα).</summary>
	public ReportColorSet Colors { get; set; } = ReportColorSet.Default;

	/// <summary>First day of the month (start of the certified period).</summary>
	public DateOnly From => new(Year, Month, 1);

	/// <summary>Last day of the month (end of the certified period).</summary>
	public DateOnly To => new(Year, Month, DateTime.DaysInMonth(Year, Month));
}

/// <summary>The employees of one kind of contract on a collective certificate.</summary>
public class ContractCertificateGroup
{
	/// <summary>The kind of contract (picks the group's title among the editable texts).</summary>
	public ContractKind Kind { get; set; }

	/// <summary>One row per employee, ordered by name.</summary>
	public List<ContractCertificateRow> Rows { get; set; } = new();
}

/// <summary>One employee's row on a collective certificate.</summary>
public class ContractCertificateRow
{
	/// <summary>Α/Α: runs on through the whole form, from one group to the next.</summary>
	public int Index { get; set; }

	public string LastName { get; set; } = "";
	public string FirstName { get; set; } = "";
	public string Speciality { get; set; } = "";

	/// <summary>Afternoon overtime until 22:00 (paid "simple" hours).</summary>
	public decimal AfternoonOvertime { get; set; }

	/// <summary>Night overtime 22:00–06:00 (paid night hours).</summary>
	public decimal NightOvertime { get; set; }

	/// <summary>Sunday and holiday overtime by day, 06:00–22:00 (paid Sunday hours).</summary>
	public decimal SundayDayOvertime { get; set; }

	/// <summary>Sunday and holiday overtime at night, 22:00–06:00 (no pay category feeds it yet).</summary>
	public decimal SundayNightOvertime { get; set; }

	/// <summary>Work "to complete" on working days (paid "to complete" hours).</summary>
	public decimal WeekdayNightToComplete { get; set; }

	/// <summary>Work "to complete" on Sundays and holidays (paid holiday hours).</summary>
	public decimal SundayHolidayToComplete { get; set; }
}
