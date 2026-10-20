using System.Globalization;
using System.Text;

namespace Vardiologio.Application.ShiftEntry;

/// <summary>
/// Search of the employee pickers: finds employees by what is typed (surname and / or first
/// name, in any order, ignoring case and accents) and says which letters of a name matched,
/// so the picker can highlight them. Pure text logic, no UI.
/// </summary>
public static class EmployeeSearch
{
	/// <summary>Separator between the name and the speciality in <see cref="EmployeeListItem.Display"/>.</summary>
	private const string NameSeparator = " — ";

	/// <summary>The name part of an option ("Βρεττός Γεώργιος"), without the speciality.</summary>
	public static string Name(EmployeeListItem item)
	{
		var cut = item.Display.IndexOf(NameSeparator, StringComparison.Ordinal);
		return cut < 0 ? item.Display : item.Display[..cut];
	}

	/// <summary>The speciality part of an option ("ΠΕ Διοικητικού"), or "" when there is none.</summary>
	public static string Speciality(EmployeeListItem item)
	{
		var cut = item.Display.IndexOf(NameSeparator, StringComparison.Ordinal);
		return cut < 0 ? "" : item.Display[(cut + NameSeparator.Length)..];
	}

	/// <summary>
	/// The employees whose name contains every typed word. Names where a typed word starts a
	/// name word come first ("βρ" → Βρεττός before Λάμπρου); inside each group the given order
	/// (alphabetical) is kept. An empty text returns everyone.
	/// </summary>
	/// <param name="items">All employees, already in alphabetical order.</param>
	/// <param name="text">What the user typed.</param>
	public static IReadOnlyList<EmployeeListItem> Filter(IEnumerable<EmployeeListItem> items, string? text)
	{
		var words = Words(text);
		if (words.Length == 0) return items.ToList();

		return items
			.Select(item => (item, name: Fold(Name(item))))
			.Where(x => words.All(w => x.name.Contains(w, StringComparison.Ordinal)))
			// OrderBy is stable, so the alphabetical order survives inside each group.
			.OrderBy(x => words.Any(w => StartsAWord(x.name, w)) ? 0 : 1)
			.Select(x => x.item)
			.ToList();
	}

	/// <summary>
	/// Splits a name into pieces, marking the letters that match a typed word, e.g.
	/// "Βρεττός" with "ρετ" → ("Β", false), ("ρετ", true), ("τός", false). The pieces put together
	/// give the name back exactly (accents and case as stored).
	/// </summary>
	public static IReadOnlyList<(string Text, bool Hit)> Highlight(string name, string? text)
	{
		var words = Words(text);
		var folded = Fold(name);   // same length as the name: one folded letter per letter
		var hit = new bool[name.Length];

		// Mark every occurrence of every typed word.
		foreach (var word in words)
		{
			for (var at = folded.IndexOf(word, StringComparison.Ordinal); at >= 0;
			     at = folded.IndexOf(word, at + word.Length, StringComparison.Ordinal))
			{
				for (var i = at; i < at + word.Length; i++) hit[i] = true;
			}
		}

		// Group neighbouring letters with the same mark into one piece.
		var pieces = new List<(string Text, bool Hit)>();
		var start = 0;
		for (var i = 1; i <= name.Length; i++)
		{
			if (i < name.Length && hit[i] == hit[start]) continue;
			pieces.Add((name[start..i], hit[start]));
			start = i;
		}
		return pieces;
	}

	/// <summary>The typed text as folded words ("βρεττός γε" → "ΒΡΕΤΤΟΣ", "ΓΕ").</summary>
	private static string[] Words(string? text) =>
		Fold(text ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);

	/// <summary>True when <paramref name="word"/> is at the start of the name or right after a space or hyphen.</summary>
	private static bool StartsAWord(string foldedName, string word)
	{
		for (var at = foldedName.IndexOf(word, StringComparison.Ordinal); at >= 0;
		     at = foldedName.IndexOf(word, at + 1, StringComparison.Ordinal))
		{
			if (at == 0 || foldedName[at - 1] is ' ' or '-') return true;
		}
		return false;
	}

	/// <summary>
	/// Upper case without accents, letter by letter, so the result has exactly one character
	/// per character of the input ("Βρεττός" → "ΒΡΕΤΤΟΣ") and positions in the two strings match.
	/// </summary>
	private static string Fold(string text)
	{
		var sb = new StringBuilder(text.Length);
		foreach (var ch in text)
		{
			// Decompose the single letter (ύ → υ + ´) and keep its base letter only.
			var decomposed = ch.ToString().Normalize(NormalizationForm.FormD);
			var letter = decomposed.FirstOrDefault(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark, ch);
			sb.Append(char.ToUpperInvariant(letter));   // ς and σ both become Σ
		}
		return sb.ToString();
	}
}
