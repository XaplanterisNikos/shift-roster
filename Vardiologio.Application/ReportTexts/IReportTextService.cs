using Vardiologio.Domain.Enums;

namespace Vardiologio.Application.ReportTexts;

/// <summary>One editable line of a form, as shown on the settings page.</summary>
public class ReportTextLine
{
	/// <summary>Longest text a line can hold (mirrors the column size in AppDbContext).</summary>
	public const int MaxLength = 1000;

	/// <summary>Id of the stored line.</summary>
	public int Id { get; set; }

	/// <summary>Above or below the table.</summary>
	public ReportSection Section { get; set; }

	/// <summary>Block key (see <see cref="ReportTextBlocks"/>).</summary>
	public string Block { get; set; } = "";

	/// <summary>Position inside the block (0-based).</summary>
	public int Line { get; set; }

	/// <summary>The text; empty means the line is not printed.</summary>
	public string Text { get; set; } = "";
}

/// <summary>
/// Reads and updates the fixed texts of the official forms. Lines are only ever updated:
/// there is no create and no delete.
/// </summary>
public interface IReportTextService
{
	/// <summary>All lines of a form, in the order they are printed.</summary>
	Task<List<ReportTextLine>> GetAsync(ReportForm form);

	/// <summary>
	/// Saves the lines of a form and remembers the texts they replace, so the save can be undone.
	/// Does nothing when no text actually changed. Lines are matched by Id; unknown Ids are ignored.
	/// </summary>
	Task SaveAsync(ReportForm form, IReadOnlyList<ReportTextLine> lines);

	/// <summary>True when the form has a previous state to go back to.</summary>
	Task<bool> CanUndoAsync(ReportForm form);

	/// <summary>
	/// Puts back the texts the form had before its last save. The two states are swapped,
	/// so undoing a second time brings the newer texts back.
	/// </summary>
	Task UndoLastSaveAsync(ReportForm form);
}
