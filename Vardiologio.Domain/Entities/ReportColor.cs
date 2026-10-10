namespace Vardiologio.Domain.Entities;

/// <summary>
/// One editable colour of a report (e.g. the shading of the Sunday columns of the Σ.Ω.).
/// The rows are created by the seeder, one per colour key, and only ever updated: the user
/// changes the colour, not the list. What a key colours is described in code (ReportColorCatalog).
/// </summary>
public class ReportColor
{
	/// <summary>Primary key.</summary>
	public int Id { get; set; }

	/// <summary>Which colour this is, e.g. "MonthlyTotal.Sunday" (keys in ReportColorCatalog).</summary>
	public string Key { get; set; } = "";

	/// <summary>The colour as "#RRGGBB".</summary>
	public string Hex { get; set; } = "";

	/// <summary>The colour before the last save (null until the first save); lets the user undo it.</summary>
	public string? PreviousHex { get; set; }
}
