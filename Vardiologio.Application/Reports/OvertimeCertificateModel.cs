namespace Vardiologio.Application.Reports;

/// <summary>
/// Data for one employee's monthly "ΠΙΣΤΟΠΟΙΗΣΗ ΥΠΕΡΩΡΙΑΚΗΣ ΚΑΙ ΛΟΙΠΗΣ ΕΡΓΑΣΙΑΣ" (individual
/// certificate). The hours are the <b>paid</b> hours per pay category — the same numbers as the
/// "Πληρ." column of the monthly hours report.
/// </summary>
public class OvertimeCertificateModel
{
	/// <summary>Certificate year.</summary>
	public int Year { get; set; }

	/// <summary>Certificate month (1–12).</summary>
	public int Month { get; set; }

	/// <summary>Α/Α: the employee's position in the list of active employees (same order as the other reports).</summary>
	public int Index { get; set; }

	public string LastName { get; set; } = "";
	public string FirstName { get; set; } = "";
	public string Speciality { get; set; } = "";

	/// <summary>"Η Βεβαιών Υπάλληλος" instead of "Ο" (see <see cref="GreekText.IsFemaleName"/>).</summary>
	public bool IsFemale { get; set; }

	/// <summary>Υπερωριακή εργασία — Απογευματινή έως 22:00 (paid Απλή hours).</summary>
	public decimal AfternoonOvertime { get; set; }

	/// <summary>Υπερωριακή εργασία — Νυχτερινή 22:00–06:00 (paid Νυχτερινή hours).</summary>
	public decimal NightOvertime { get; set; }

	/// <summary>Υπερωριακή εργασία Κυριακές και εξαιρέσιμες — Ημερήσιες 6:00–22:00 (paid Κυριακής hours).</summary>
	public decimal SundayDayOvertime { get; set; }

	/// <summary>Υπερωριακή εργασία Κυριακές και εξαιρέσιμες — Νυχτερινή 22:00–06:00. No pay category feeds it yet, so it prints 0:00 as on the original form.</summary>
	public decimal SundayNightOvertime { get; set; }

	/// <summary>Εργασία προς Συμπλήρωση — Νυχτερινή εργασίμων ημερών (paid Προς συμπλήρωση hours).</summary>
	public decimal WeekdayNightToComplete { get; set; }

	/// <summary>Εργασία προς Συμπλήρωση — Κυριακών και εξαιρέσιμων ημερών (paid Αργίας hours).</summary>
	public decimal SundayHolidayToComplete { get; set; }

	/// <summary>First day of the month (start of the certified period).</summary>
	public DateOnly From => new(Year, Month, 1);

	/// <summary>Last day of the month (end of the certified period).</summary>
	public DateOnly To => new(Year, Month, DateTime.DaysInMonth(Year, Month));
}
