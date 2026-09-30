using System.Globalization;
using Vardiologio.Application.Reports;

namespace Vardiologio.Infrastructure.Reports;

/// <summary>
/// Fixed texts of the individual overtime certificate, copied as they are from the
/// organisation's sheet (declaration and certification wording, decision numbers).
/// The emblem and the letterhead are shared with the attendance report (<see cref="AttendanceSheetTexts"/>).
/// </summary>
internal static class OvertimeCertificateTexts
{
	private static readonly string[] MonthsGen =
		{ "ΙΑΝΟΥΑΡΙΟΥ","ΦΕΒΡΟΥΑΡΙΟΥ","ΜΑΡΤΙΟΥ","ΑΠΡΙΛΙΟΥ","ΜΑΪΟΥ","ΙΟΥΝΙΟΥ",
		  "ΙΟΥΛΙΟΥ","ΑΥΓΟΥΣΤΟΥ","ΣΕΠΤΕΜΒΡΙΟΥ","ΟΚΤΩΒΡΙΟΥ","ΝΟΕΜΒΡΙΟΥ","ΔΕΚΕΜΒΡΙΟΥ" };

	/// <summary>
	/// Labels on the right of the letterhead, filled in by hand; keyed by the letterhead
	/// line they sit on (ΕΛΛΗΝΙΚΗ ΔΗΜΟΚΡΑΤΙΑ, ΕΙΔΙΚΟΣ ΔΙΑΒΑΘΜΙΔΙΚΟΣ ΣΥΝΔΕΣΜΟΣ, ΕΔΡΑ).
	/// </summary>
	public static readonly (int Line, string Text)[] HandFilled =
	{
		(0, "Αθήνα  :"),
		(2, "Αρ. Πρωτ.  :"),
		(4, "Προς :")
	};

	/// <summary>First title line, with the month ("… ΜΗΝΟΣ ΑΥΓΟΥΣΤΟΥ 2026").</summary>
	public static string Title(int year, int month) => $"Δ/ΝΣΗΣ ΣΤΑΘΜΩΝ ΜΕΤΑΦΟΡΤΩΣΗΣ  ΜΗΝΟΣ {MonthsGen[month - 1]} {year}";

	/// <summary>Second title line.</summary>
	public const string Subtitle = "ΠΙΣΤΟΠΟΙΗΣΗ ΥΠΕΡΩΡΙΑΚΗΣ ΚΑΙ ΛΟΙΠΗΣ ΕΡΓΑΣΙΑΣ";

	/// <summary>Introduction under the titles (four centred lines; the decision numbers are fixed).</summary>
	public static string[] Intro(DateOnly from, DateOnly to) => new[]
	{
		"Βεβαιώνεται ότι ο παρακάτω υπάλληλος εργάστηκε υπερωριακά κατά τις απογευματινές ώρες",
		"και κατά τις Κυριακές, τις αντίστοιχες ώρες που αναφέρονται, για την αντιμετώπιση υπηρεσικών αναγκών της",
		"της Δ/νσης Σταθμών Μεταφόρτωσης , σύμφωνα με την αριθμ πρωτ. 15373/12-12-25 Απόφασης Προέδρου και της υπ'",
		$"αριθμ.8247/23-07-2026 Απόφασης Σύστασης Συνεργείου, κατά το χρονικό διάστημα από {Date(from)} έως {Date(to)}"
	};

	// Table headers: four single columns, then three groups of two sub-columns.
	public const string HeadIndex = "A/A", HeadLastName = "Επίθετο", HeadFirstName = "Όνομα", HeadSpeciality = "κλάδος/ειδικότητα";

	/// <summary>Group headers, each over two sub-columns.</summary>
	public static readonly string[] Groups =
	{
		"Υπερωριακή Εργασία σε ώρες",
		"Υπερωρική Εργασία\nΚυριακές και εξαιρέσιμες",
		"Εργασία προς Συμπλήρωση"
	};

	/// <summary>Sub-column headers, left to right (two per group).</summary>
	public static readonly string[] SubHeads =
	{
		"Απογευματινή  Εως  22:00'",
		"Νυχτερινή  22:00 - 06:00",
		"Ημερήσιες\n6:00 - 22:00",
		"Νυχτερινή  22:00 - 06:00",
		"Νυχτερινή εργασίμων ημερών",
		"Κυριακών και εξαιρέσιμων ημερών"
	};

	/// <summary>Heading of the declaration block.</summary>
	public const string DeclarationTitle = "Υπεύθυνη Δήλωση";

	/// <summary>The four numbered declaration items.</summary>
	public static readonly string[] Declarations =
	{
		"Ο υπάλληλος που υπογράφει την κατάσταση αυτή, δηλώνει ότι εργάστηκε υπερωριακά κατά το χρονικό διάστημα που αναγράφεται στην κατάσταση, τις ώρες που αναγράφονται και το ωρομίσθιο του κλιμακίου.",
		"Δεν συμμετέχω σε άλλο συνεργείο υπερωριακής εργασίας στον ανωτέρω μήνα.",
		"Οι πάσης φύσεως πρόσθετες μηνιαίες αποδοχές μου δεν είναι ανώτερες του συνόλου των μηνιαίων αποδοχών της οργανικής μου θέσης, κατά τον ανωτέρω μήνα, σύμφωνα με την παρ. 2 του αριθ. 104 του Συντάγματος.",
		"Οι πάσης φύσεως μεικτές αποδοχές, αποζημιώσεις και πρόσθετες αμοιβές ή απολαβές μου εν γένει μηνιαίως δεν υπερβαίνουν το ποσό της παρ. 1 του αρ. 2 του Ν 3833/2010."
	};

	/// <summary>Certification paragraph under the declaration.</summary>
	public const string Certification =
		"ΒΕΒΑΙΩΣΗ: Βεβαιώνεται ότι ο ανωτέρω αναφερόμενος υπάλληλος εργάστηκε κατά το ως άνω χρονικό διάστημα, για τι ώρες που αναφέρονται πλάι στο ονοματεπώνυμο μου και δεν έχω υπερβεί το συνολικό αριθμό ωρών που προβλέπεται από την απόφαση στην επικεφαλίδα. Επίσης βεβαιούται το γνήσιο της υπογραφής.";

	/// <summary>Signature role: "Ο" for men, "Η" for women.</summary>
	public static string SignatureRole(bool isFemale) => isFemale ? "Η Βεβαιών Υπάλληλος" : "Ο Βεβαιών Υπάλληλος";

	/// <summary>Name under the signature ("ΒΡΥΝΑΣ      ΓΕΩΡΓΙΟΣ"), as on the form.</summary>
	public static string SignatureName(OvertimeCertificateModel m) => $"{GreekText.Upper(m.LastName)}      {GreekText.Upper(m.FirstName)}";

	/// <summary>A date as printed in the introduction ("01/08/2026").</summary>
	public static string Date(DateOnly date) => date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

	/// <summary>Hours as a clock value ("10:00", "6:30", "0:00") for the four overtime columns.</summary>
	public static string Clock(decimal hours)
	{
		var minutes = (int)Math.Round(hours * 60, MidpointRounding.AwayFromZero);
		return $"{minutes / 60}:{minutes % 60:00}";
	}

	/// <summary>Hours as a number ("33", "0", "6,5") for the two "προς συμπλήρωση" columns.</summary>
	public static string Number(decimal hours) => hours.ToString("0.##", CultureInfo.GetCultureInfo("el-GR"));

	/// <summary>The six values in column order, each already formatted as printed.</summary>
	public static string[] Values(OvertimeCertificateModel m) => new[]
	{
		Clock(m.AfternoonOvertime),
		Clock(m.NightOvertime),
		Clock(m.SundayDayOvertime),
		Clock(m.SundayNightOvertime),
		Number(m.WeekdayNightToComplete),
		Number(m.SundayHolidayToComplete)
	};
}
