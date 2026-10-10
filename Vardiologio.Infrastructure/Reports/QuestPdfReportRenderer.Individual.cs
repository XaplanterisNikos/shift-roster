using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Vardiologio.Application.ReportColors;
using Vardiologio.Application.Reports;

namespace Vardiologio.Infrastructure.Reports;

/// <summary>
/// Individual analytical sheet ("ΑΝΑΛΥΤΙΚΟΣ ΗΜΕΡΗΣΙΟΣ ΠΙΝΑΚΑΣ ΠΑΡΟΥΣΙΩΝ ΚΑΙ ΩΡΑΡΙΩΝ") of one
/// employee — one A4 landscape page: title, the day table and the summary blocks. Columns and
/// values come from <see cref="IndividualSheetLayout"/>, the same source as the preview and
/// the Excel file. The page sits in ScaleToFit, so a 31-day month never flows onto a second page.
/// </summary>
public partial class QuestPdfReportRenderer
{
	/// <inheritdoc/>
	public byte[] Render(IndividualReportModel m)
	{
		var doc = Document.Create(container =>
		{
			container.Page(page =>
			{
				page.Size(PageSizes.A4.Landscape());
				page.Margin(20);
				page.DefaultTextStyle(x => x.FontFamily("Lato").FontSize(7));

				page.Content().ScaleToFit().Column(col =>
				{
					col.Item().AlignCenter().Text(IndividualSheetLayout.Title).Bold().FontSize(11);
					col.Item().AlignCenter().Text(IndividualSheetLayout.Subtitle(m)).Bold().FontSize(9.5f);
					col.Item().PaddingTop(6).Element(c => IndivTable(c, m));
					col.Item().PaddingTop(8).Element(c => IndivSummary(c, m));
				});
			});
		});

		return doc.GeneratePdf();
	}

	/// <summary>The day table: two header rows, then one row per day of the month.</summary>
	private static void IndivTable(IContainer c, IndividualReportModel m) => c.Table(t =>
	{
		t.ColumnsDefinition(cols =>
		{
			cols.RelativeColumn(1.1f);   // date
			cols.RelativeColumn(0.7f);   // Α/Α
			cols.RelativeColumn(3.6f);   // name
			cols.RelativeColumn(1.3f);   // contract
			for (var i = 4; i < IndividualSheetLayout.ColumnCount; i++) cols.RelativeColumn(1.15f);
		});

		// Row 1: single headers (two rows high) and group headers (over their sub-columns).
		foreach (var header in IndividualSheetLayout.Headers)
		{
			var text = header.Text;   // copied before the lambda-free call chain, for clarity
			if (header.Subs.Length == 0)
				t.Cell().RowSpan(2).Element(IndivH).Text(text).Bold().FontSize(6.5f);
			else
				t.Cell().ColumnSpan((uint)header.Span).Element(IndivH).Text(text).Bold().FontSize(6.5f);
		}

		// Row 2: the sub-column headers, in the same order.
		foreach (var header in IndividualSheetLayout.Headers)
			foreach (var sub in header.Subs)
				t.Cell().Element(IndivH).Text(sub).Bold().FontSize(6);

		var sundayBg = m.Colors[ReportColorCatalog.IndividualSunday];   // Παράμετροι → Αναφορών → Χρώματα
		foreach (var day in m.Days)
		{
			var cells = IndividualSheetLayout.Cells(m, day);
			var shaded = day.IsSunday;   // copied before the lambdas
			for (var i = 0; i < cells.Length; i++)
			{
				var isName = i == IndividualSheetLayout.NameColumn;
				t.Cell().Element(x => IndivD(shaded ? x.Background(sundayBg) : x, isName)).Text(cells[i]);
			}
		}
	});

	/// <summary>
	/// The summary blocks side by side: title, a note saying where the numbers come from,
	/// then one header + value per number.
	/// </summary>
	private static void IndivSummary(IContainer c, IndividualReportModel m) => c.Row(row =>
	{
		var blocks = IndividualSheetLayout.Summary(m);
		for (var b = 0; b < blocks.Length; b++)
		{
			var block = blocks[b];   // copied before the lambdas
			if (b > 0) row.ConstantItem(8);   // gap between blocks

			// Wider blocks get more of the row: the width follows the number of values.
			row.RelativeItem(block.Cells.Length).Border(1).Column(col =>
			{
				col.Item().BorderBottom(0.5f).Padding(2).AlignCenter().Text(block.Title).Bold().FontSize(7);
				col.Item().BorderBottom(0.5f).Padding(2).AlignCenter().Text(block.Note).Italic().FontSize(5.5f);
				col.Item().Table(t =>
				{
					t.ColumnsDefinition(cols =>
					{
						for (var i = 0; i < block.Cells.Length; i++) cols.RelativeColumn();
					});
					foreach (var (header, _) in block.Cells)
						t.Cell().Element(IndivH).Text(header).FontSize(5.5f);
					foreach (var (_, value) in block.Cells)
						t.Cell().Element(IndivH).Text(value).Bold().FontSize(9);
				});
			});
		}
	});

	// Individual-sheet cell styles.
	private static IContainer IndivH(IContainer c) => c.Border(0.75f).Padding(2).AlignCenter().AlignMiddle();   // header / summary cell

	/// <summary>A day cell: centred, except the name column which is left-aligned.</summary>
	private static IContainer IndivD(IContainer c, bool leftAligned)
	{
		var cell = c.Border(0.5f).MinHeight(12).PaddingHorizontal(2);
		return (leftAligned ? cell.AlignLeft() : cell.AlignCenter()).AlignMiddle();
	}
}
