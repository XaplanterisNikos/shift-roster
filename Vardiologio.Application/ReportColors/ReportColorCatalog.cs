namespace Vardiologio.Application.ReportColors;

/// <summary>
/// Catalogue of the colours the reports use: the key stored in the database, the report and
/// the fields the colour is applied to (shown on the settings page), and the default colour.
/// One place for the keys, so the seed, the settings page and the renderers always agree.
/// </summary>
public static class ReportColorCatalog
{
	// Συνολική μηνιαία Σ.Ω.
	public const string MonthlyTotalHeader = "MonthlyTotal.Header";
	public const string MonthlyTotalSaturday = "MonthlyTotal.Saturday";
	public const string MonthlyTotalSunday = "MonthlyTotal.Sunday";
	public const string MonthlyTotalTotals = "MonthlyTotal.Totals";

	// Ωρών μηνιαία
	public const string HoursHeader = "Hours.Header";
	public const string HoursCategories = "Hours.Categories";
	public const string HoursExcess = "Hours.Excess";

	// Πίνακας βαρδιών εργασίας
	public const string ShiftTableWeekend = "ShiftTable.Weekend";

	// Ατομική μηνιαία
	public const string IndividualSunday = "Individual.Sunday";

	// ΙΔΟΧ / ΙΔΑΧ / Μόνιμοι (συγκεντρωτική πιστοποίηση ανά σύμβαση)
	public const string ContractCertificateBand = "ContractCertificate.Band";

	/// <summary>One colour of the catalogue.</summary>
	/// <param name="Key">Key stored in the database.</param>
	/// <param name="Report">Greek name of the report (group title on the settings page).</param>
	/// <param name="Fields">The fields of that report the colour is applied to.</param>
	/// <param name="DefaultHex">The colour the application ships with, "#RRGGBB".</param>
	public sealed record Entry(string Key, string Report, string Fields, string DefaultHex);

	/// <summary>Every colour, in the order of the settings page (grouped by report).</summary>
	public static readonly Entry[] All =
	{
		new(MonthlyTotalHeader, "Συνολική μηνιαία Σ.Ω.", "Επικεφαλίδες πίνακα, ημέρες καθημερινών και υπόμνημα", "#D9E1F2"),
		new(MonthlyTotalSaturday, "Συνολική μηνιαία Σ.Ω.", "Στήλες Σαββάτου", "#FFF2CC"),
		new(MonthlyTotalSunday, "Συνολική μηνιαία Σ.Ω.", "Στήλες Κυριακής", "#FCE4D6"),
		new(MonthlyTotalTotals, "Συνολική μηνιαία Σ.Ω.", "Επικεφαλίδες «Εργασία προς συμπλήρωση»", "#E2EFDA"),

		new(HoursHeader, "Ωρών μηνιαία", "Επικεφαλίδες Α/Α, ονοματεπώνυμου και ειδικότητας", "#D9E1F2"),
		new(HoursCategories, "Ωρών μηνιαία", "Επικεφαλίδες κατηγοριών και γραμμή συνόλων", "#E2EFDA"),
		new(HoursExcess, "Ωρών μηνιαία", "Ώρες που περισσεύουν (δεν πληρώνονται)", "#FDE2E1"),

		new(ShiftTableWeekend, "Πίνακας βαρδιών εργασίας", "Στήλες Σαββάτου, Κυριακής και αργιών", "#D6DCE4"),

		new(IndividualSunday, "Ατομική μηνιαία", "Γραμμές Κυριακής", "#FCE4D6"),

		new(ContractCertificateBand, "ΙΔΟΧ / ΙΔΑΧ / Μόνιμοι", "Ζώνες τμήματος και σύμβασης", "#92D050")
	};

	/// <summary>The catalogue entry of a key, or null for an unknown key.</summary>
	public static Entry? Find(string key) => All.FirstOrDefault(e => e.Key == key);

	/// <summary>True for a colour written as "#RRGGBB" (the only form the reports accept).</summary>
	public static bool IsHex(string? value) =>
		value is { Length: 7 } && value[0] == '#' && value.Skip(1).All(Uri.IsHexDigit);
}

/// <summary>
/// The colours as the renderers read them: the stored colour of a key, or the catalogue
/// default when nothing (valid) is stored — so a report never prints without its colours.
/// </summary>
public sealed class ReportColorSet
{
	/// <summary>Only the defaults (model default, before the report service fills it).</summary>
	public static readonly ReportColorSet Default = new([]);

	private readonly Dictionary<string, string> _stored;

	/// <summary>Builds the set from the stored colours; invalid values are ignored.</summary>
	public ReportColorSet(IEnumerable<(string Key, string Hex)> colors)
	{
		_stored = new Dictionary<string, string>();
		foreach (var (key, hex) in colors)
			if (ReportColorCatalog.IsHex(hex)) _stored[key] = hex;
	}

	/// <summary>The colour of a key as "#RRGGBB".</summary>
	public string this[string key] =>
		_stored.TryGetValue(key, out var hex) ? hex : ReportColorCatalog.Find(key)?.DefaultHex ?? "#FFFFFF";
}
