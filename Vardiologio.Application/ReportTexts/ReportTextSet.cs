namespace Vardiologio.Application.ReportTexts;

/// <summary>
/// The editable texts of one form, as the renderers read them: lines grouped by block, in
/// order. Empty lines are skipped (an empty line is not printed), but every line keeps its
/// original position, because the formatting of a line (bold, size) follows that position.
/// </summary>
public sealed class ReportTextSet
{
	/// <summary>A set without any text (model default, before the report service fills it).</summary>
	public static readonly ReportTextSet Empty = new([]);

	// Block key -> all its lines in order, empty ones included.
	private readonly Dictionary<string, (int Line, string Text)[]> _blocks;

	/// <summary>Builds the set from the stored lines of one form.</summary>
	public ReportTextSet(IEnumerable<(string Block, int Line, string Text)> lines)
	{
		_blocks = lines
			.GroupBy(l => l.Block)
			.ToDictionary(g => g.Key, g => g.OrderBy(l => l.Line).Select(l => (l.Line, l.Text)).ToArray());
	}

	/// <summary>True when the form has this block at all (even if every line of it is empty).</summary>
	public bool HasBlock(string block) => _blocks.ContainsKey(block);

	/// <summary>The non-empty lines of a block in order, each with its original position.</summary>
	public (int Line, string Text)[] Lines(string block) =>
		_blocks.TryGetValue(block, out var lines)
			? lines.Where(l => !string.IsNullOrWhiteSpace(l.Text)).ToArray()
			: [];

	/// <summary>The non-empty lines of a block in order, text only.</summary>
	public string[] Texts(string block) => Lines(block).Select(l => l.Text).ToArray();

	/// <summary>The text at one position of a block; "" when that line is empty or missing.</summary>
	public string Line(string block, int line) =>
		Lines(block).FirstOrDefault(l => l.Line == line).Text ?? "";

	/// <summary>
	/// The signature blocks of the form, left to right: role lines (above the signing space)
	/// and name / title lines (below it). A block is kept even when all its lines are empty,
	/// so the other signatures do not move.
	/// </summary>
	public (string[] Role, string[] Name)[] Signatures()
	{
		var blocks = new List<(string[] Role, string[] Name)>();
		for (var n = 1; HasBlock(ReportTextBlocks.SignatureRole(n)) || HasBlock(ReportTextBlocks.SignatureName(n)); n++)
			blocks.Add((Texts(ReportTextBlocks.SignatureRole(n)), Texts(ReportTextBlocks.SignatureName(n))));
		return blocks.ToArray();
	}
}
