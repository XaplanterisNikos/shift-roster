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
		Letterhead, HandFilled, Titles, Addressee, Intro, DeclarationTitle, Declarations, Certification
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
		ReportForm.AttendanceStaff => "Παρουσίες προσωπικού",
		ReportForm.AttendanceDirectorate => "Παρουσίες Διεύθυνσης",
		ReportForm.OvertimeCertificate => "Ατομική βεβαίωση",
		ReportForm.MonthlyTotal => "Συνολική μηνιαία Σ.Ω.",
		_ => form.ToString()
	};

	/// <summary>Greek name of a section (card title on the settings page).</summary>
	public static string SectionName(ReportSection section) =>
		section == ReportSection.Header ? "Κεφαλίδα" : "Υποσέλιδο";
}
