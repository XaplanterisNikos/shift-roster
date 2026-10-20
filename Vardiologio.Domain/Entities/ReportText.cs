using Vardiologio.Domain.Enums;

namespace Vardiologio.Domain.Entities;

/// <summary>
/// One editable line of fixed text on an official form (e.g. a letterhead line, a title,
/// a signatory's name). The rows are created by the seeder and only ever updated:
/// the user cannot add or delete lines, and a line left empty is simply not printed.
/// A line is identified by its form, its block and its position inside the block.
/// </summary>
public class ReportText
{
	/// <summary>Primary key.</summary>
	public int Id { get; set; }

	/// <summary>Form the line belongs to.</summary>
	public ReportForm Form { get; set; }

	/// <summary>Above or below the table (drives the grouping on the settings page).</summary>
	public ReportSection Section { get; set; }

	/// <summary>Block of lines inside the form, e.g. "Letterhead" (keys in ReportTextBlocks).</summary>
	public string Block { get; set; } = "";

	/// <summary>Position inside the block (0-based). The formatting of a line follows its position.</summary>
	public int Line { get; set; }

	/// <summary>The text as printed; empty when the line is not printed.</summary>
	public string Text { get; set; } = "";

	/// <summary>
	/// The text before the last save of the form (null until the first save).
	/// Lets the user undo the last change.
	/// </summary>
	public string? PreviousText { get; set; }
}
