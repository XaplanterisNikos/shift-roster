using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Vardiologio.Infrastructure.Reports;

/// <summary>
/// Page rules shared by every PDF form:
/// <list type="bullet">
/// <item>"Σελίδα X από Y" at the bottom right of every page (<see cref="PageNumbers"/>);</item>
/// <item>the closing block of a form (signatures, legend) never prints on a page of its own: it is
/// joined to the last row of the table (<see cref="RowsWithClosing"/>).</item>
/// </list>
/// </summary>
public partial class QuestPdfReportRenderer
{
	/// <summary>Font size of the page number.</summary>
	private const float PageNumberSize = 7;

	/// <summary>
	/// One printed row of a table, kept as a drawing step so it can be drawn either in the table
	/// itself or, for the last rows, in the closing block (see <see cref="RowsWithClosing"/>).
	/// </summary>
	/// <param name="IsBand">True for a full-width title row (contract, department, "ΟΡΙΣΜΕΝΟΥ ΧΡΟΝΟΥ"),
	/// false for an employee row.</param>
	/// <param name="Draw">Adds the cells of the row to the given table.</param>
	private readonly record struct TableItem(bool IsBand, Action<TableDescriptor> Draw);

	/// <summary>
	/// "Σελίδα X από Y", right-aligned; meant for <c>page.Footer()</c>.
	/// </summary>
	/// <param name="c">The footer container.</param>
	/// <param name="section">
	/// Counts the pages of this <c>Section</c> only (each fortnight of the attendance report is a
	/// document of its own); null counts the pages of the whole file.
	/// </param>
	private static void PageNumbers(IContainer c, string? section = null) => c.AlignRight().Text(t =>
	{
		t.DefaultTextStyle(s => s.FontSize(PageNumberSize).NormalWeight());

		t.Span(PagingTexts.Page);
		if (section is null) t.CurrentPageNumber();
		else t.PageNumberWithinSection(section);

		t.Span(PagingTexts.Of);
		if (section is null) t.TotalPages();
		else t.TotalPagesWithinSection(section);
	});

	/// <summary>
	/// Draws the rows of a table and closes it with the form's closing block (signatures, legend),
	/// so that the closing block never prints on a page without employees.
	/// <para>
	/// The last row — together with any band right above it, so a band never stays alone at the
	/// bottom of a page — is not drawn in the table itself: it is drawn in a small table with the
	/// same columns, placed with the closing block in ONE last cell of the table that spans every
	/// column and is never split (<c>ShowEntire</c>). The two look exactly like the rest of the
	/// table when they fit at the bottom of the page; when they do not, they move to the next page
	/// together. That page then gets the table header only if the table already repeats it on
	/// every page (Σ.Ω., certificate per contract): nothing changes in how each form behaves.
	/// </para>
	/// </summary>
	/// <param name="table">The table of the form.</param>
	/// <param name="columnCount">Number of columns of the table (the last cell spans them all).</param>
	/// <param name="columns">The column definitions of the table, repeated in the small table.</param>
	/// <param name="items">The rows of the table, in print order.</param>
	/// <param name="closing">The closing block; null when this table is not the last one of the form.</param>
	private static void RowsWithClosing(TableDescriptor table, uint columnCount,
		Action<TableColumnsDefinitionDescriptor> columns, IReadOnlyList<TableItem> items, Action<IContainer>? closing)
	{
		// Not the last table of the form: its rows are drawn as usual.
		if (closing is null)
		{
			foreach (var item in items) item.Draw(table);
			return;
		}

		// The rows that go with the closing block: the last one and every band right above it.
		var tailStart = Math.Max(items.Count - 1, 0);
		while (tailStart > 0 && items[tailStart - 1].IsBand) tailStart--;

		for (var i = 0; i < tailStart; i++) items[i].Draw(table);

		// One cell across the whole table, never split between two pages.
		table.Cell().ColumnSpan(columnCount).ShowEntire().Column(col =>
		{
			if (tailStart < items.Count)
			{
				col.Item().Table(last =>
				{
					last.ColumnsDefinition(columns);   // same widths as the table above it
					for (var i = tailStart; i < items.Count; i++) items[i].Draw(last);
				});
			}

			col.Item().Element(closing);
		});
	}
}
