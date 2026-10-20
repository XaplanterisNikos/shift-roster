using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Vardiologio.Application.ReportColors;
using Vardiologio.Application.Reports;

namespace Vardiologio.Infrastructure.Reports;

/// <summary>
/// Individual analytical sheet ("ΑΝΑΛΥΤΙΚΟΣ ΗΜΕΡΗΣΙΟΣ ΠΙΝΑΚΑΣ ΠΑΡΟΥΣΙΩΝ ΚΑΙ ΩΡΑΡΙΩΝ") of one
/// employee in the organisation's official form, on one A4 landscape page: emblem + letterhead
/// and the date top right, the title, the day table with the overtime row under it (one table,
/// so their columns line up) and the two signatures. Layout and texts come from
/// <see cref="IndividualSheetTexts"/> (shared with the Excel file). The page sits in
/// ScaleToFit, so a 31-day month never flows onto a second page.
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
					col.Item().Element(c => IndivHead(c, m));
					foreach (var title in IndividualSheetTexts.Titles(m.Texts))
						col.Item().PaddingTop(4).AlignCenter().Text(title).Bold().FontSize(11);
					col.Item().PaddingTop(6).Element(c => IndivTable(c, m));
					col.Item().PaddingTop(10).Element(c => IndivSignatures(c, m));
				});

				// Outside the scaled content, so the page number keeps its size.
				page.Footer().PaddingTop(4).Element(c => PageNumbers(c));
			});
		});

		return doc.GeneratePdf();
	}

	/// <summary>Emblem and letterhead on the left; the date label and date on the right, level with the directorate line.</summary>
	private static void IndivHead(IContainer c, IndividualReportModel m) => c.Row(row =>
	{
		row.ConstantItem(260).Column(left =>
		{
			left.Item().PaddingLeft(20).Width(40).Height(38).Element(EmblemImage);
			left.Item().Height(3);
			foreach (var line in IndividualSheetTexts.Letterhead(m.Texts))
				left.Item().Text(line).FontSize(7);
		});

		row.RelativeItem();

		var label = IndividualSheetTexts.DateLabel(m.Texts);
		row.ConstantItem(160).Column(right =>
		{
			// Emblem + gap, then empty lines down to the directorate line of the letterhead.
			right.Item().Height(41);
			for (var i = 0; i < IndividualSheetTexts.DateRow; i++)
				right.Item().Text(" ").FontSize(7);
			if (label.Length > 0)
				right.Item().Text(t =>
				{
					t.Span(label + " ").FontSize(9);
					t.Span(IndividualSheetTexts.Date(m.IssueDate)).Bold().FontSize(9);
				});
		});
	});

	/// <summary>
	/// The day table and the overtime rows in one table (positions set explicitly):
	/// band row, two header rows, one row per day, then the three overtime rows.
	/// </summary>
	private static void IndivTable(IContainer c, IndividualReportModel m) => c.Table(t =>
	{
		t.ColumnsDefinition(cols =>
		{
			foreach (var width in IndividualSheetTexts.Widths) cols.RelativeColumn((float)width);
		});

		// Row 1: the bands over the schedule columns.
		foreach (var (text, from, to) in IndividualSheetTexts.ScheduleBands)
			t.Cell().Row(1).Column((uint)from).ColumnSpan((uint)(to - from + 1))
				.Element(IndivH).Text(text).Bold().FontSize(6.5f);

		// Rows 2–3: headers (a header without sub-columns is two rows high).
		uint col = 1;
		foreach (var header in IndividualSheetTexts.Headers)
		{
			var text = header.Text;
			if (header.Subs.Length == 0)
			{
				t.Cell().Row(2).Column(col).RowSpan(2).Element(IndivH).Text(text).Bold().FontSize(6.5f);
			}
			else
			{
				t.Cell().Row(2).Column(col).ColumnSpan((uint)header.Span).Element(IndivH).Text(text).Bold().FontSize(6.5f);
				for (var i = 0; i < header.Subs.Length; i++)
					t.Cell().Row(3).Column(col + (uint)i).Element(IndivH).Text(header.Subs[i]).Bold().FontSize(5.5f);
			}
			col += (uint)header.Span;
		}

		// One row per day.
		var sundayBg = m.Colors[ReportColorCatalog.IndividualSunday];   // Παράμετροι → Αναφορών → Χρώματα
		uint r = 4;
		foreach (var day in m.Days)
		{
			var cells = IndividualSheetTexts.Cells(m, day);
			var shaded = day.IsSunday;   // copied before the lambdas
			for (var i = 0; i < cells.Length; i++)
			{
				var isName = i == IndividualSheetTexts.NameColumn;
				t.Cell().Row(r).Column((uint)i + 1)
					.Element(x => IndivD(shaded ? x.Background(sundayBg) : x, isName)).Text(cells[i]);
			}
			r++;
		}

		// The overtime rows: caption three rows high, single columns two rows high + value,
		// groups with their sub-headers and values.
		var s1 = r;
		foreach (var group in IndividualSheetTexts.Summary(m))
		{
			var span = (uint)(group.To - group.From + 1);
			if (group.Caption)
			{
				t.Cell().Row(s1).Column((uint)group.From).RowSpan(3).ColumnSpan(span)
					.Element(IndivH).Text(group.Text).Bold().FontSize(6.5f);
				continue;
			}

			var single = group.Columns.Length == 1 && group.Columns[0].Header.Length == 0;
			t.Cell().Row(s1).Column((uint)group.From).RowSpan(single ? 2u : 1u).ColumnSpan(span)
				.Element(IndivH).Text(group.Text).Bold().FontSize(6.5f);

			foreach (var column in group.Columns)
			{
				var columnSpan = (uint)(column.To - column.From + 1);
				if (!single)
					t.Cell().Row(s1 + 1).Column((uint)column.From).ColumnSpan(columnSpan)
						.Element(IndivH).Text(column.Header).FontSize(5.5f);
				t.Cell().Row(s1 + 2).Column((uint)column.From).ColumnSpan(columnSpan)
					.Element(IndivH).Text(column.Value).Bold().FontSize(8);
			}
		}
	});

	/// <summary>The employee's signature on the left, his supervisor's on the right (role, space, name, title).</summary>
	private static void IndivSignatures(IContainer c, IndividualReportModel m) => c.Row(row =>
	{
		var signatures = IndividualSheetTexts.Signatures(m);
		var roleLines = Math.Max(1, signatures.Max(s => s.Role.Length));

		row.RelativeItem(1);   // margin before the left block
		for (var k = 0; k < signatures.Length; k++)
		{
			var signature = signatures[k];   // copied before the lambda
			if (k > 0) row.RelativeItem(4);   // gap between the blocks
			row.RelativeItem(4).Column(block =>
			{
				for (var i = 0; i < roleLines; i++)
					block.Item().AlignCenter().Text(i < signature.Role.Length ? signature.Role[i] : " ").Bold().FontSize(9);
				block.Item().Height(45);   // space to sign
				block.Item().AlignCenter().Text(signature.Name.Length > 0 ? signature.Name : " ").Bold().FontSize(9);
				block.Item().AlignCenter().Text(signature.Title.Length > 0 ? signature.Title : " ").FontSize(9);
			});
		}
		row.RelativeItem(2);   // margin after the right block
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
