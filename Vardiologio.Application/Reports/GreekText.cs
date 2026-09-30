using System.Globalization;
using System.Text;

namespace Vardiologio.Application.Reports;

/// <summary>Small Greek-text helpers shared by the official reports.</summary>
public static class GreekText
{
	private static readonly CultureInfo Greek = CultureInfo.GetCultureInfo("el-GR");

	/// <summary>Greek vowels (upper case, no accents) — a Greek first name ending in one is a woman's.</summary>
	private const string Vowels = "ΑΕΗΙΟΥΩ";

	/// <summary>
	/// Upper case without accents, as official forms print names ("Βρύνας" → "ΒΡΥΝΑΣ").
	/// Surrounding spaces are removed; inner spaces are kept.
	/// </summary>
	public static string Upper(string text)
	{
		// Decompose (ά → α + ´), drop the accent marks, recompose, then upper-case with Greek rules.
		var decomposed = text.Trim().Normalize(NormalizationForm.FormD);
		var sb = new StringBuilder(decomposed.Length);
		foreach (var ch in decomposed)
			if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
				sb.Append(ch);
		return sb.ToString().Normalize(NormalizationForm.FormC).ToUpper(Greek);
	}

	/// <summary>
	/// True when the name is a woman's, for "Ο"/"Η" on the forms. Greek men's first names end
	/// in a consonant (Γεώργιος, Ηλίας, and foreign ones such as Ίον), women's in a vowel
	/// (Μαρία, Κασσιανή, Όλγα). When the first name is only an initial ("Γ."), the same rule is
	/// applied to the surname (Μακρή, Τόλιου → woman; Βρύνας → man).
	/// </summary>
	/// <param name="firstName">First name as stored (may be an initial).</param>
	/// <param name="lastName">Surname as stored.</param>
	public static bool IsFemaleName(string firstName, string lastName)
	{
		var first = Upper(firstName);
		var name = first.Length > 2 && !first.EndsWith('.') ? first : Upper(lastName);
		return name.Length > 0 && Vowels.Contains(name[^1]);
	}

	// Greek → Latin (ELOT 743 style), for file names. Two-letter groups are checked first.
	private static readonly (string Greek, string Latin)[] Pairs =
		{ ("ΟΥ", "OU"), ("ΑΥ", "AV"), ("ΕΥ", "EV") };

	private static readonly Dictionary<char, string> Letters = new()
	{
		['Α'] = "A", ['Β'] = "V", ['Γ'] = "G", ['Δ'] = "D", ['Ε'] = "E", ['Ζ'] = "Z", ['Η'] = "I", ['Θ'] = "TH",
		['Ι'] = "I", ['Κ'] = "K", ['Λ'] = "L", ['Μ'] = "M", ['Ν'] = "N", ['Ξ'] = "X", ['Ο'] = "O", ['Π'] = "P",
		['Ρ'] = "R", ['Σ'] = "S", ['Τ'] = "T", ['Υ'] = "Y", ['Φ'] = "F", ['Χ'] = "CH", ['Ψ'] = "PS", ['Ω'] = "O"
	};

	/// <summary>
	/// Upper-case Latin transliteration for file names ("Βρύνας" → "VRYNAS",
	/// "Κοτσεκίδου - Κουτσούκου" → "KOTSEKIDOU_KOUTSOUKOU"). Latin letters and digits are kept,
	/// anything else becomes a single "_", so the result is always a safe Windows file-name part.
	/// </summary>
	public static string ToLatin(string text)
	{
		var upper = Upper(text);
		var sb = new StringBuilder(upper.Length);
		for (var i = 0; i < upper.Length; i++)
		{
			// Two-letter groups first (ΟΥ → OU, ΑΥ → AV, ΕΥ → EV).
			var pair = Pairs.FirstOrDefault(p => string.CompareOrdinal(upper, i, p.Greek, 0, 2) == 0);
			if (pair.Greek is not null) { sb.Append(pair.Latin); i++; continue; }

			var ch = upper[i];
			if (Letters.TryGetValue(ch, out var latin)) sb.Append(latin);
			else if (ch is >= 'A' and <= 'Z' or >= '0' and <= '9') sb.Append(ch);
			else if (sb.Length > 0 && sb[^1] != '_') sb.Append('_');   // spaces, dashes, dots → one "_"
		}
		return sb.ToString().Trim('_');
	}
}
