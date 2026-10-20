namespace Vardiologio.Application.ReportColors;

/// <summary>One editable colour, as shown on the settings page.</summary>
public class ReportColorItem
{
	/// <summary>Id of the stored colour.</summary>
	public int Id { get; set; }

	/// <summary>Colour key (see <see cref="ReportColorCatalog"/>).</summary>
	public string Key { get; set; } = "";

	/// <summary>The colour as "#RRGGBB".</summary>
	public string Hex { get; set; } = "";
}

/// <summary>
/// Reads and updates the colours of the reports. Colours are only ever updated: there is no
/// create and no delete.
/// </summary>
public interface IReportColorService
{
	/// <summary>All colours, in the order of the catalogue (grouped by report).</summary>
	Task<List<ReportColorItem>> GetAsync();

	/// <summary>
	/// Saves the colours and remembers the ones they replace, so the save can be undone.
	/// Does nothing when no colour actually changed. Colours are matched by Id; a value that is
	/// not "#RRGGBB" keeps the stored colour.
	/// </summary>
	Task SaveAsync(IReadOnlyList<ReportColorItem> colors);

	/// <summary>True when there is a previous state to go back to.</summary>
	Task<bool> CanUndoAsync();

	/// <summary>
	/// Puts back the colours before the last save. The two states are swapped, so undoing a
	/// second time brings the newer colours back.
	/// </summary>
	Task UndoLastSaveAsync();
}
