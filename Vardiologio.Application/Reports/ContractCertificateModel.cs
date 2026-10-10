using Vardiologio.Application.ReportColors;
using Vardiologio.Application.ReportTexts;

namespace Vardiologio.Application.Reports;

/// <summary>Which of the two collective certificates a model holds (one printed form each).</summary>
public enum ContractCertificateSheet
{
	/// <summary>Permanent staff, then open-ended contracts (ΙΔΑΧ) — one form, two groups.</summary>
	PermanentAndOpenEnded,

	/// <summary>Fixed-term contracts (ΙΔΟΧ) — one form, one group.</summary>
	FixedTerm
}

/// <summary>
/// Data for one collective "ΠΙΣΤΟΠΟΙΗΣΗ ΥΠΕΡΩΡΙΑΚΗΣ ΚΑΙ ΛΟΙΠΗΣ ΕΡΓΑΣΙΑΣ" per contract: the same six
/// hour columns as the individual certificate, one row per employee, grouped by contract.
/// The hours are the <b>paid</b> hours, so every row agrees with that employee's own certificate.
/// </summary>
public class ContractCertificateModel
{
	/// <summary>Certificate year.</summary>
	public int Year { get; set; }

	/// <summary>Certificate month (1–12).</summary>
	public int Month { get; set; }

	/// <summary>Which of the two forms this is.</summary>
	public ContractCertificateSheet Sheet { get; set; }

	/// <summary>The contract groups of the form, in print order (a group may have no rows).</summary>
	public List<ContractCertificateGroup> Groups { get; set; } = new();

	/// <summary>
	/// Active employees left out of both forms because their contract is missing or is not one
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
	/// <summary>Α/Α: runs on from one group to the next inside a form.</summary>
	public int Index { get; set; }

	public string LastName { get; set; } = "";
	public string FirstName { get; set; } = "";
	public string Speciality { get; set; } = "";

	/// <summary>Υπερωριακή εργασία — Απογευματινή έως 22:00 (paid Απλή hours).</summary>
	public decimal AfternoonOvertime { get; set; }

	/// <summary>Υπερωριακή εργασία — Νυχτερινή 22:00–06:00 (paid Νυχτερινή hours).</summary>
	public decimal NightOvertime { get; set; }

	/// <summary>Υπερωριακή εργασία Κυριακές και εξαιρέσιμες — Ημερήσιες 6:00–22:00 (paid Κυριακής hours).</summary>
	public decimal SundayDayOvertime { get; set; }

	/// <summary>Υπερωριακή εργασία Κυριακές και εξαιρέσιμες — Νυχτερινή 22:00–06:00 (no pay category feeds it yet).</summary>
	public decimal SundayNightOvertime { get; set; }

	/// <summary>Εργασία προς Συμπλήρωση — Νυχτερινή εργασίμων ημερών (paid Προς συμπλήρωση hours).</summary>
	public decimal WeekdayNightToComplete { get; set; }

	/// <summary>Εργασία προς Συμπλήρωση — Κυριακών και εξαιρέσιμων ημερών (paid Αργίας hours).</summary>
	public decimal SundayHolidayToComplete { get; set; }
}
