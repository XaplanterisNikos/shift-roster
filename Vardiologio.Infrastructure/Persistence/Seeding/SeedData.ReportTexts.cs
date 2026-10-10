using Vardiologio.Application.ReportTexts;
using Vardiologio.Domain.Enums;

namespace Vardiologio.Infrastructure.Persistence.Seeding;

/// <summary>
/// Default texts of the official forms, copied as they are from the organisation's sheets
/// (wording, spacing and capitals included). They only fill the ReportTexts table the first
/// time; after that the user edits them in Παράμετροι → Αναφορές.
/// </summary>
public static partial class SeedData
{
	/// <summary>
	/// One block of lines of a form. The number of lines is fixed: an empty string is a spare
	/// line the user can fill in later (an empty line is not printed).
	/// </summary>
	public sealed record ReportTextBlockSeed(ReportForm Form, ReportSection Section, string Block, string[] Lines);

	private const ReportSection Header = ReportSection.Header, Footer = ReportSection.Footer;

	/// <summary>Letterhead shared by the two attendance reports and the certificate (each form gets its own copy).</summary>
	private static readonly string[] CommonLetterhead =
	{
		"ΕΛΛΗΝΙΚΗ ΔΗΜΟΚΡΑΤΙΑ",
		"ΠΕΡΙΦΕΡΕΙΑ ΑΤΤΙΚΗΣ",
		"ΕΙΔΙΚΟΣ ΔΙΑΒΑΘΜΙΔΙΚΟΣ ΣΥΝΔΕΣΜΟΣ",
		"Δ/ΝΣΗ: ΣΤΑΘΜΩΝ ΜΕΤΑΦΟΡΤΩΣΗΣ ΑΠΟΡΡΙΜΜΑΤΩΝ",
		"ΕΔΡΑ: Άντερσεν 6 και Μωραΐτη 90, 11525 Αθήνα",
		"Πληρ.: Βρύνας Γεώργιος",
		"Τηλ.: 210 4015080, 210 4320581",
		"e-mail: vrinas@edsna.gr"
	};

	/// <summary>Every block of every form.</summary>
	public static readonly ReportTextBlockSeed[] ReportTexts =
	{
		// ---- Πίνακας βαρδιών εργασίας (its letterhead differs from the other forms) ----
		new(ReportForm.ShiftTable, Header, ReportTextBlocks.Letterhead, new[]
		{
			"ΕΛΛΗΝΙΚΗ ΔΗΜΟΚΡΑΤΙΑ",
			"ΠΕΡΙΦΕΡΕΙΑ ΑΤΤΙΚΗΣ",
			"ΕΙΔΙΚΟΣ ΔΙΑΒΑΘΜΙΔΙΚΟΣ ΣΥΝΔΕΣΜΟΣ",
			"ΝΟΜΟΥ ΑΤΤΙΚΗΣ",
			"ΕΔΡΑ: Άντερσεν 6 και Μωραΐτη 90, 115 25,",
			"τηλ.: 213 214 8 478-479",
			"Δ/ΝΣΗ ΣΤΑΘΜΩΝ ΜΕΤΑΦΟΡΤΩΣΗΣ ΑΠΟΡΡΙΜΜΑΤΩΝ",
			"Τηλ: 210 43 20 581",
			"E-mail: iona@edsna.gr"
		}),
		new(ReportForm.ShiftTable, Header, ReportTextBlocks.Titles, new[]
		{
			"ΔΙΕΥΘΥΝΣΗ  ΣΤΑΘΜΩΝ ΜΕΤΑΦΟΡΤΩΣΗΣ",
			"ΠΙΝΑΚΑΣ   ΒΑΡΔΙΩΝ   ΕΡΓΑΣΙΑΣ  ΠΡΟΣΩΠΙΚΟΥ",
			"ΤΜΗΜΑΤΟΣ ΚΕΝΤΡΙΚΩΝ ΣΜΑ"
		}),
		new(ReportForm.ShiftTable, Header, ReportTextBlocks.Addressee, new[]
		{
			"ΠΡΟΣ: Αν. Προϊσταμένη Τμ. Λογιστηρίου",
			"κ. Αγιανίδου Δέσποινα"
		}),
		new(ReportForm.ShiftTable, Footer, ReportTextBlocks.SignatureRole(1), new[] { "Ο συντάξας" }),
		new(ReportForm.ShiftTable, Footer, ReportTextBlocks.SignatureName(1), new[] { "ΒΡΥΝΑΣ ΓΕΩΡΓΙΟΣ", "ΠΕ ΔΙΟΙΚΗΤΙΚΟΥ-ΟΙΚΟΝΟΜΙΚΟΥ", "" }),
		new(ReportForm.ShiftTable, Footer, ReportTextBlocks.SignatureRole(2), new[] { "Ο  Αν. Προϊστάμενος Δ/νσης Σ.Μ.Α." }),
		new(ReportForm.ShiftTable, Footer, ReportTextBlocks.SignatureName(2), new[] { "ΠΑΠΑΣΤΑΜΑΤΗΣ ΑΘΑΝΑΣΙΟΣ", "ΠΕ Χημικών Μηχανικών, MSc / Α'", "" }),

		// ---- Παρουσίες προσωπικού ----
		new(ReportForm.AttendanceStaff, Header, ReportTextBlocks.Letterhead, CommonLetterhead),
		new(ReportForm.AttendanceStaff, Header, ReportTextBlocks.Titles, new[]
		{
			"ΔΙΕΥΘΥΝΣΗ ΣΤΑΘΜΩΝ ΜΕΤΑΦΟΡΤΩΣΗΣ",
			"ΠΙΝΑΚΑΣ ΠΑΡΟΥΣΙΩΝ ΠΡΟΣΩΠΙΚΟΥ",
			"ΤΜΗΜΑΤΟΣ ΚΕΝΤΡΙΚΩΝ ΣΜΑ"
		}),
		new(ReportForm.AttendanceStaff, Footer, ReportTextBlocks.SignatureRole(1), new[] { "Ο Συντάξας", "", "" }),
		new(ReportForm.AttendanceStaff, Footer, ReportTextBlocks.SignatureName(1), new[] { "Βρύνας Γεώργιος", "ΠΕ Διοικητικού - Οικονομικού", "" }),
		new(ReportForm.AttendanceStaff, Footer, ReportTextBlocks.SignatureRole(2), new[] { "Ο Αν. Προϊστάμενος", "Διεύθυνσης", "Σταθμών Μεταφόρτωσης" }),
		new(ReportForm.AttendanceStaff, Footer, ReportTextBlocks.SignatureName(2), new[] { "Αθανάσιος Παπασταμάτης", "ΠΕ Χημικών Μηχανικών MSc / Α΄", "" }),

		// ---- Παρουσίες Διεύθυνσης (addressed to HR, signed by the Secretary General) ----
		new(ReportForm.AttendanceDirectorate, Header, ReportTextBlocks.Addressee, new[] { "Προς τη Δ/νση Διοικητικών Υπηρεσιών/Τμήμα Προσωπικού" }),
		new(ReportForm.AttendanceDirectorate, Header, ReportTextBlocks.Letterhead, CommonLetterhead),
		new(ReportForm.AttendanceDirectorate, Header, ReportTextBlocks.Titles, new[]
		{
			"Δ/ΝΣΗ ΣΤΑΘΜΩΝ ΜΕΤΑΦΟΡΤΩΣΗΣ ΑΠΟΡ/ΤΩΝ ΣΜΑ",
			"ΠΙΝΑΚΑΣ ΠΑΡΟΥΣΙΩΝ ΥΠΑΛΛΗΛΟΥ Δ/ΝΣΗΣ ΣΜΑ ΠΑΠΑΣΤΑΜΑΤΗ ΑΘΑΝΑΣΙΟΥ"
		}),
		new(ReportForm.AttendanceDirectorate, Footer, ReportTextBlocks.SignatureRole(1), new[] { "Με εντολή Προέδρου", "Ο Γενικός Γραμματέας του Ε.Δ.Σ.Ν.Α.", "" }),
		new(ReportForm.AttendanceDirectorate, Footer, ReportTextBlocks.SignatureName(1), new[] { "Γεώργιος Αποστολόπουλος", "", "" }),

		// ---- Ατομική βεβαίωση ----
		new(ReportForm.OvertimeCertificate, Header, ReportTextBlocks.Letterhead, CommonLetterhead),
		new(ReportForm.OvertimeCertificate, Header, ReportTextBlocks.HandFilled, new[] { "Αθήνα  :", "Αρ. Πρωτ.  :", "Προς :" }),
		// First title line: the month ("ΜΗΝΟΣ ΑΥΓΟΥΣΤΟΥ 2026") is added after it automatically.
		new(ReportForm.OvertimeCertificate, Header, ReportTextBlocks.Titles, new[]
		{
			"Δ/ΝΣΗΣ ΣΤΑΘΜΩΝ ΜΕΤΑΦΟΡΤΩΣΗΣ",
			"ΠΙΣΤΟΠΟΙΗΣΗ ΥΠΕΡΩΡΙΑΚΗΣ ΚΑΙ ΛΟΙΠΗΣ ΕΡΓΑΣΙΑΣ"
		}),
		// The period ("από 01/08/2026 έως 31/08/2026") is added after the last line automatically.
		new(ReportForm.OvertimeCertificate, Header, ReportTextBlocks.Intro, new[]
		{
			"Βεβαιώνεται ότι ο παρακάτω υπάλληλος εργάστηκε υπερωριακά κατά τις απογευματινές ώρες",
			"και κατά τις Κυριακές, τις αντίστοιχες ώρες που αναφέρονται, για την αντιμετώπιση υπηρεσικών αναγκών της",
			"της Δ/νσης Σταθμών Μεταφόρτωσης , σύμφωνα με την αριθμ πρωτ. 15373/12-12-25 Απόφασης Προέδρου και της υπ'",
			"αριθμ.8247/23-07-2026 Απόφασης Σύστασης Συνεργείου, κατά το χρονικό διάστημα"
		}),
		new(ReportForm.OvertimeCertificate, Footer, ReportTextBlocks.DeclarationTitle, new[] { "Υπεύθυνη Δήλωση" }),
		new(ReportForm.OvertimeCertificate, Footer, ReportTextBlocks.Declarations, new[]
		{
			"Ο υπάλληλος που υπογράφει την κατάσταση αυτή, δηλώνει ότι εργάστηκε υπερωριακά κατά το χρονικό διάστημα που αναγράφεται στην κατάσταση, τις ώρες που αναγράφονται και το ωρομίσθιο του κλιμακίου.",
			"Δεν συμμετέχω σε άλλο συνεργείο υπερωριακής εργασίας στον ανωτέρω μήνα.",
			"Οι πάσης φύσεως πρόσθετες μηνιαίες αποδοχές μου δεν είναι ανώτερες του συνόλου των μηνιαίων αποδοχών της οργανικής μου θέσης, κατά τον ανωτέρω μήνα, σύμφωνα με την παρ. 2 του αριθ. 104 του Συντάγματος.",
			"Οι πάσης φύσεως μεικτές αποδοχές, αποζημιώσεις και πρόσθετες αμοιβές ή απολαβές μου εν γένει μηνιαίως δεν υπερβαίνουν το ποσό της παρ. 1 του αρ. 2 του Ν 3833/2010."
		}),
		new(ReportForm.OvertimeCertificate, Footer, ReportTextBlocks.Certification, new[]
		{
			"ΒΕΒΑΙΩΣΗ: Βεβαιώνεται ότι ο ανωτέρω αναφερόμενος υπάλληλος εργάστηκε κατά το ως άνω χρονικό διάστημα, για τι ώρες που αναφέρονται πλάι στο ονοματεπώνυμο μου και δεν έχω υπερβεί το συνολικό αριθμό ωρών που προβλέπεται από την απόφαση στην επικεφαλίδα. Επίσης βεβαιούται το γνήσιο της υπογραφής."
		}),

		// ---- Συνολική μηνιαία Σ.Ω.: starts with the same texts as the shift table (its own copy) ----
		new(ReportForm.MonthlyTotal, Header, ReportTextBlocks.Letterhead, new[]
		{
			"ΕΛΛΗΝΙΚΗ ΔΗΜΟΚΡΑΤΙΑ",
			"ΠΕΡΙΦΕΡΕΙΑ ΑΤΤΙΚΗΣ",
			"ΕΙΔΙΚΟΣ ΔΙΑΒΑΘΜΙΔΙΚΟΣ ΣΥΝΔΕΣΜΟΣ",
			"ΝΟΜΟΥ ΑΤΤΙΚΗΣ",
			"ΕΔΡΑ: Άντερσεν 6 και Μωραΐτη 90, 115 25,",
			"τηλ.: 213 214 8 478-479",
			"Δ/ΝΣΗ ΣΤΑΘΜΩΝ ΜΕΤΑΦΟΡΤΩΣΗΣ ΑΠΟΡΡΙΜΜΑΤΩΝ",
			"Τηλ: 210 43 20 581",
			"E-mail: iona@edsna.gr"
		}),
		new(ReportForm.MonthlyTotal, Header, ReportTextBlocks.Titles, new[]
		{
			"ΔΙΕΥΘΥΝΣΗ  ΣΤΑΘΜΩΝ ΜΕΤΑΦΟΡΤΩΣΗΣ",
			"ΠΙΝΑΚΑΣ   ΒΑΡΔΙΩΝ   ΕΡΓΑΣΙΑΣ  ΠΡΟΣΩΠΙΚΟΥ",
			"ΤΜΗΜΑΤΟΣ ΚΕΝΤΡΙΚΩΝ ΣΜΑ"
		}),
		new(ReportForm.MonthlyTotal, Header, ReportTextBlocks.Addressee, new[]
		{
			"ΠΡΟΣ: Αν. Προϊσταμένη Τμ. Λογιστηρίου",
			"κ. Αγιανίδου Δέσποινα"
		}),
		new(ReportForm.MonthlyTotal, Footer, ReportTextBlocks.SignatureRole(1), new[] { "Ο συντάξας" }),
		new(ReportForm.MonthlyTotal, Footer, ReportTextBlocks.SignatureName(1), new[] { "ΒΡΥΝΑΣ ΓΕΩΡΓΙΟΣ", "ΠΕ ΔΙΟΙΚΗΤΙΚΟΥ-ΟΙΚΟΝΟΜΙΚΟΥ", "" }),
		new(ReportForm.MonthlyTotal, Footer, ReportTextBlocks.SignatureRole(2), new[] { "Ο  Αν. Προϊστάμενος Δ/νσης Σ.Μ.Α." }),
		new(ReportForm.MonthlyTotal, Footer, ReportTextBlocks.SignatureName(2), new[] { "ΠΑΠΑΣΤΑΜΑΤΗΣ ΑΘΑΝΑΣΙΟΣ", "ΠΕ Χημικών Μηχανικών, MSc / Α'", "" })
	};
}
