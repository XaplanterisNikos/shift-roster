namespace Vardiologio.Application.Reports;

/// <summary>The three kinds of contract the collective certificate groups employees by.</summary>
public enum ContractKind
{
	/// <summary>Μόνιμοι (permanent staff).</summary>
	Permanent = 0,

	/// <summary>Ιδιωτικού δικαίου αορίστου χρόνου (ΙΔΑΧ).</summary>
	OpenEnded = 1,

	/// <summary>Ιδιωτικού δικαίου ορισμένου χρόνου (ΙΔΟΧ).</summary>
	FixedTerm = 2
}

/// <summary>
/// Tells which kind of contract an employee's contract (Παράμετροι → Εργαζομένων → Συμβάσεις)
/// is. The contracts are a list the user edits, so the kind is recognised from the contract's
/// name first and from its code second, without accents, case, dots or spaces.
/// </summary>
public static class ContractRules
{
	/// <summary>
	/// The kind of a contract, or null when it is not one of the three (or the employee has none).
	/// Recognised names: "Μόνιμος…", "ΙΔΑΧ" / "…αορίστου…", "ΙΔΟΧ" / "…ορισμένου…".
	/// Recognised codes (the ones the application ships with): "Μ", "Α.Χ", "Ο.Χ".
	/// </summary>
	/// <param name="code">Short code of the contract, or null when the employee has no contract.</param>
	/// <param name="name">Name of the contract, or null when the employee has no contract.</param>
	public static ContractKind? Resolve(string? code, string? name)
	{
		var n = Fold(name);
		if (n.StartsWith("ΜΟΝΙΜ", StringComparison.Ordinal)) return ContractKind.Permanent;
		if (n.Contains("ΙΔΑΧ", StringComparison.Ordinal) || n.Contains("ΑΟΡΙΣΤ", StringComparison.Ordinal)) return ContractKind.OpenEnded;
		if (n.Contains("ΙΔΟΧ", StringComparison.Ordinal) || n.Contains("ΟΡΙΣΜΕΝ", StringComparison.Ordinal)) return ContractKind.FixedTerm;

		return Fold(code) switch
		{
			"Μ" => ContractKind.Permanent,
			"ΑΧ" => ContractKind.OpenEnded,
			"ΟΧ" => ContractKind.FixedTerm,
			_ => null
		};
	}

	/// <summary>
	/// The kinds of contract printed on each of the two forms, in print order:
	/// permanent then open-ended on the first, fixed-term alone on the second.
	/// </summary>
	public static ContractKind[] KindsOf(ContractCertificateSheet sheet) =>
		sheet == ContractCertificateSheet.FixedTerm
			? [ContractKind.FixedTerm]
			: [ContractKind.Permanent, ContractKind.OpenEnded];

	/// <summary>Upper case without accents, dots or spaces ("Α.Χ" → "ΑΧ"); "" for null.</summary>
	private static string Fold(string? text) =>
		text is null ? "" : GreekText.Upper(text).Replace(".", "").Replace(" ", "");
}
