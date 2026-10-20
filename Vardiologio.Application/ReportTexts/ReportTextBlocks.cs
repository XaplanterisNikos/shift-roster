using Vardiologio.Domain.Enums;

namespace Vardiologio.Application.ReportTexts;

/// <summary>
/// Keys of the text blocks of the official forms (the value stored in ReportText.Block), with
/// their Greek labels for the settings page. One place for the keys, so the seed, the settings
/// page and the renderers always agree.
/// </summary>
public static class ReportTextBlocks
{
	/// <summary>Line above the letterhead / beside the titles saying who the form is sent to.</summary>
	public const string Addressee = "Addressee";

	/// <summary>Service details under the emblem (name, address, phones, e-mail).</summary>
	public const string Letterhead = "Letterhead";

	/// <summary>Centre title lines (the month / period line is added automatically).</summary>
	public const string Titles = "Titles";

	/// <summary>Certificate only: labels on the right of the letterhead that are filled in by hand.</summary>
	public const string HandFilled = "HandFilled";

	/// <summary>Certificate only: introduction under the titles (the period dates are added automatically).</summary>
	public const string Intro = "Intro";

	/// <summary>Contract certificate only: band above the first group of the table ("ΤΜΗΜΑ: …").</summary>
	public const string Department = "Department";

	/// <summary>
	/// Contract certificate only: title of each contract group, one line per contract in the
	/// order of the ContractKind enum (permanent, open-ended, fixed-term).
	/// </summary>
	public const string ContractGroups = "ContractGroups";

	/// <summary>
	/// Σ.Ω. only: the separator row inside the table that starts the fixed-term (ΙΔΟΧ) employees,
	/// who follow the permanent / open-ended ones in a second alphabetical list.
	/// </summary>
	public const string FixedTermBand = "FixedTermBand";

	/// <summary>Default text of <see cref="FixedTermBand"/> (seeded once, then edited by the user).</summary>
	public const string FixedTermBandDefault = "ΟΡΙΣΜΕΝΟΥ ΧΡΟΝΟΥ";

	/// <summary>Individual monthly only: label before the date at the top right ("Σ.Μ.Α:").</summary>
	public const string DateLabel = "DateLabel";

	/// <summary>
	/// Individual monthly only: headers of the overtime table under the days, one line per
	/// header, left to right (see the defaults in <see cref="IndividualMonthlyDefaults"/>).
	/// </summary>
	public const string SummaryGroups = "SummaryGroups";

	/// <summary>
	/// Default lines of the individual monthly form's own blocks (the letterhead is copied from
	/// the overtime certificate instead), as on the organisation's "ΚΑΡΤΑ" sheet. Seeded once per
	/// missing line; afterwards the user edits them in Παράμετροι → Αναφορών.
	/// </summary>
	public static readonly (string Block, bool Footer, string[] Lines)[] IndividualMonthlyDefaults =
	{
		(DateLabel, false, ["Σ.Μ.Α:"]),
		(Titles, false, ["ΑΝΑΛΥΤΙΚΟΣ ΗΜΕΡΗΣΙΟΣ ΠΙΝΑΚΑΣ ΠΑΡΟΥΣΙΩΝ ΚΑΙ ΩΡΑΡΙΩΝ", ""]),
		(SummaryGroups, true,
		[
			"ΥΠΕΡΩΡΙΕΣ",
			"Α/Α",
			"Επίθετο",
			"κλάδος ειδικότητα",
			"ΥΠΕΡΩΡΙΕΣ ΜΗΝΟΣ ΑΠΟ ΚΑΡΤΑ",
			"ΥΠΕΡΩΡ. ΚΥΡΙΑΚΩΝ ΑΠΟ ΚΑΡΤΑ",
			"Εργασία προς Συμπλήρωση (ΕΒΔΟΜΑΔΙΑΙΑ)",
			"Εργασία προς Συμπλήρωση (ΚΥΡΙΑΚΩΝ-ΕΞΑΙΡΕΣΙΜΩΝ)",
			"Υπερωριακή Εργασία σε ώρες",
			"Υπερωριακή Εργασία Κυριακές και εξαιρέσιμες",
			"Εργασία προς Συμπλήρωση"
		]),
		// The employee's own role; "Ο" / "Η" and his name / speciality are added when printing.
		(SignatureRole(1), true, ["Υπάλληλος Τμήματος Κεντρικών Σ.Μ.Α."])
	};

	/// <summary>Certificate only: heading of the declaration.</summary>
	public const string DeclarationTitle = "DeclarationTitle";

	/// <summary>Certificate only: the numbered declaration items.</summary>
	public const string Declarations = "Declarations";

	/// <summary>Certificate only: certification paragraph under the declaration.</summary>
	public const string Certification = "Certification";

	/// <summary>Role lines of the n-th signature block (1-based), printed above the signing space.</summary>
	public static string SignatureRole(int n) => $"Signature{n}Role";

	/// <summary>Name / title lines of the n-th signature block (1-based), printed below the signing space.</summary>
	public static string SignatureName(int n) => $"Signature{n}Name";

	/// <summary>Blocks in the order they are listed on the settings page (signatures come after them).</summary>
	private static readonly string[] DisplayOrder =
	{
		Letterhead, HandFilled, DateLabel, Titles, Addressee, Intro, Department, ContractGroups, FixedTermBand,
		SummaryGroups, DeclarationTitle, Declarations, Certification
	};

	/// <summary>
	/// Sort key of a block on the settings page: the fixed blocks first, then the signature
	/// blocks by number, the role lines of each before its name lines.
	/// </summary>
	public static int Order(string block)
	{
		var index = Array.IndexOf(DisplayOrder, block);
		if (index >= 0) return index;

		for (var n = 1; n <= 9; n++)
		{
			if (block == SignatureRole(n)) return 100 + n * 2;
			if (block == SignatureName(n)) return 100 + n * 2 + 1;
		}
		return int.MaxValue;   // unknown key: last
	}

	/// <summary>Greek label of a block, shown above its lines on the settings page.</summary>
	public static string Label(string block)
	{
		switch (block)
		{
			case Addressee: return "Αποδέκτης («Προς»)";
			case Letterhead: return "Στοιχεία υπηρεσίας";
			case Titles: return "Τίτλοι";
			case HandFilled: return "Πεδία που συμπληρώνονται με το χέρι";
			case Intro: return "Εισαγωγικό κείμενο";
			case Department: return "Τμήμα (ζώνη πάνω από τον πίνακα)";
			case ContractGroups: return "Τίτλοι ομάδων σύμβασης (ζώνες του πίνακα)";
			case FixedTermBand: return "Χώρισμα ορισμένου χρόνου (γραμμή μέσα στον πίνακα)";
			case DateLabel: return "Ετικέτα ημερομηνίας (πάνω δεξιά)";
			case SummaryGroups: return "Επικεφαλίδες πίνακα υπερωριών (κάτω από τις ημέρες)";
			case DeclarationTitle: return "Τίτλος δήλωσης";
			case Declarations: return "Σημεία δήλωσης";
			case Certification: return "Βεβαίωση";
		}

		// Signature blocks: "Signature2Role" -> "Υπογραφή 2 – ιδιότητα υπογράφοντος".
		const string prefix = "Signature";
		if (block.StartsWith(prefix, StringComparison.Ordinal))
		{
			var isRole = block.EndsWith("Role", StringComparison.Ordinal);
			var number = block[prefix.Length..^4];   // both suffixes ("Role", "Name") are 4 letters
			return isRole
				? $"Υπογραφή {number} – τίτλος πάνω από την υπογραφή"
				: $"Υπογραφή {number} – ονοματεπώνυμο και ιδιότητα";
		}

		return block;   // unknown key: show it as stored
	}

	/// <summary>Greek name of a form (tab title on the settings page).</summary>
	public static string FormName(ReportForm form) => form switch
	{
		ReportForm.ShiftTable => "Πίνακας βαρδιών",
		ReportForm.AttendanceStaff => "Παρουσίες ΙΔΑΧ / Μόνιμοι",
		ReportForm.AttendanceFixedTerm => "Παρουσίες ΙΔΟΧ",
		ReportForm.AttendanceDirectorate => "Παρουσίες Διεύθυνσης",
		ReportForm.OvertimeCertificate => "Ατομική βεβαίωση",
		ReportForm.MonthlyTotal => "Συνολική μηνιαία Σ.Ω.",
		ReportForm.ContractCertificate => "ΙΔΟΧ / ΙΔΑΧ / Μόνιμοι",
		ReportForm.IndividualMonthly => "Ατομική μηνιαία",
		_ => form.ToString()
	};

	/// <summary>Greek name of a section (card title on the settings page).</summary>
	public static string SectionName(ReportSection section) =>
		section == ReportSection.Header ? "Κεφαλίδα" : "Υποσέλιδο";
}
