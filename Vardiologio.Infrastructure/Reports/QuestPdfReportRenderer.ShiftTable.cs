using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Vardiologio.Application.Reports;

namespace Vardiologio.Infrastructure.Reports;

/// <summary>
/// Official shift table ("ΠΙΝΑΚΑΣ ΒΑΡΔΙΩΝ ΕΡΓΑΣΙΑΣ ΠΡΟΣΩΠΙΚΟΥ") — one A3 landscape page laid out as
/// the "Σ.Ω." sheet. The whole page sits in ScaleToFit: when the employees or codes do not fit,
/// everything is scaled down proportionally instead of flowing onto a second page.
/// </summary>
public partial class QuestPdfReportRenderer
{
	// Relative column widths: name, speciality, one per day, one per count, the two hour columns.
	private const float StName = 5.2f, StSpec = 4.4f, StDay = 1f, StCount = 0.85f, StDaily = 1.9f, StSunHol = 2.1f;

	/// <inheritdoc/>
	public byte[] Render(ShiftTableModel m)
	{
		var doc = Document.Create(container =>
		{
			container.Page(page =>
			{
				page.Size(PageSizes.A3.Landscape());
				page.Margin(20);
				page.DefaultTextStyle(x => x.FontFamily("Lato").FontSize(8));

				// Everything on one page: scaled down if needed.
				page.Content().ScaleToFit().Column(col =>
				{
					col.Item().Element(c => StHead(c, m));
					col.Item().PaddingTop(6).Element(c => StTable(c, m));
					col.Item().PaddingTop(10).Element(c => StBottom(c, m));
				});
			});
		});

		return doc.GeneratePdf();
	}

	/// <summary>Emblem + letterhead (left), titles (centre), addressee (right).</summary>
	private static void StHead(IContainer c, ShiftTableModel m) => c.Row(row =>
	{
		row.ConstantItem(260).Column(left =>
		{
			left.Item().PaddingLeft(20).Width(48).Height(46).Image(AttendanceSheetTexts.Emblem).FitArea();
			left.Item().Height(4);
			foreach (var (text, bold) in ShiftTableTexts.Letterhead)
			{
				// Only the bold part is bold (whole line, nothing, or just the e-mail address).
				left.Item().Text(t =>
				{
					var plain = bold.Length == 0 ? text : text[..text.IndexOf(bold, StringComparison.Ordinal)];
					if (plain.Length > 0) t.Span(plain).FontSize(8);
					if (bold.Length > 0) t.Span(bold).FontSize(8).Bold();
				});
			}
		});

		row.RelativeItem().PaddingTop(4).Column(mid =>
		{
			var titles = ShiftTableTexts.Titles(m.Year, m.Month);
			for (var i = 0; i < titles.Length; i++)
			{
				var span = mid.Item().PaddingBottom(i == 0 ? 8 : 3).AlignCenter().Text(titles[i].Text).FontSize(i == 0 ? 15 : 12);
				if (titles[i].Bold) span.Bold();
			}
		});

		row.ConstantItem(260).PaddingTop(22).Column(right =>
		{
			foreach (var line in ShiftTableTexts.Addressee)
				right.Item().AlignCenter().Text(line).Bold().FontSize(10);
		});
	});

	/// <summary>The table: two header rows, then one row per employee.</summary>
	private static void StTable(IContainer c, ShiftTableModel m) => c.Table(t =>
	{
		var days = m.DaysInMonth;
		var codes = m.WorkCodes.Count;

		t.ColumnsDefinition(cols =>
		{
			cols.RelativeColumn(StName);
			cols.RelativeColumn(StSpec);
			for (var d = 0; d < days; d++) cols.RelativeColumn(StDay);
			for (var k = 0; k < codes; k++) cols.RelativeColumn(StCount);
			cols.RelativeColumn(StDaily);
			cols.RelativeColumn(StSunHol);
		});

		t.Header(h =>
		{
			// Row 1: name / speciality / days (two rows high), code group, hour headers.
			h.Cell().RowSpan(2).Element(StH).Text(ShiftTableTexts.HeadName).Bold().FontSize(9);
			h.Cell().RowSpan(2).Element(StH).Text(ShiftTableTexts.HeadSpeciality).Bold().FontSize(9);
			for (var d = 1; d <= days; d++)
			{
				var shaded = m.IsShaded(d);   // copied before the lambda
				h.Cell().RowSpan(2).Element(x => StH(StShade(x, shaded)))
					.Text(ShiftTableTexts.DayHeader(m.Year, m.Month, d)).Bold().FontSize(7.5f);
			}
			if (codes > 0)
				h.Cell().ColumnSpan((uint)codes).Element(StH).Text(ShiftTableTexts.HeadShifts).Bold().FontSize(8);
			h.Cell().Element(StH).Text(ShiftTableTexts.HeadToComplete).Bold().FontSize(6);
			h.Cell().RowSpan(2).Element(StH).Text(ShiftTableTexts.HeadSundayHoliday).Bold().FontSize(6.5f);

			// Row 2: code numbers, "ΗΜΕΡΗΣΙΑ".
			foreach (var (code, _) in m.WorkCodes)
				h.Cell().Element(StH).Text(code).Bold().FontSize(8.5f);
			h.Cell().Element(StH).Text(ShiftTableTexts.HeadDaily).Bold().FontSize(6.5f);
		});

		foreach (var e in m.Rows)
		{
			t.Cell().Element(StNameCell).Text(ShiftTableTexts.Name(e)).FontSize(8);
			t.Cell().Element(StD).Text(GreekText.Upper(e.Speciality)).FontSize(6.5f);
			for (var d = 1; d <= days; d++)
			{
				var shaded = m.IsShaded(d);   // copied before the lambda
				t.Cell().Element(x => StD(StShade(x, shaded))).Text(e.DayCodes[d - 1] ?? "").Bold().FontSize(8.5f);
			}
			foreach (var n in e.Counts)
				t.Cell().Element(StTot).Text(ShiftTableTexts.Count(n)).Bold().FontSize(8.5f);
			t.Cell().Element(StTot).Text(ShiftTableTexts.Hours(e.DailyToComplete)).Bold().FontSize(8.5f);
			t.Cell().Element(StTot).Text(ShiftTableTexts.Hours(e.SundayHolidayToComplete)).Bold().FontSize(8.5f);
		}
	});

	/// <summary>Legend under name / speciality; the signatures beside it (as on the form).</summary>
	private static void StBottom(IContainer c, ShiftTableModel m) => c.Row(row =>
	{
		// Same proportions as the table, so the legend sits exactly under name + speciality.
		var rest = m.DaysInMonth * StDay + m.WorkCodes.Count * StCount + StDaily + StSunHol;

		row.RelativeItem(StName + StSpec).Table(t =>
		{
			t.ColumnsDefinition(cols => { cols.RelativeColumn(StName); cols.RelativeColumn(StSpec); });
			t.Cell().ColumnSpan(2).Element(StH).Text(ShiftTableTexts.LegendTitle).FontSize(9);
			foreach (var (code, description) in m.WorkCodes.Concat(m.StatusCodes))
			{
				t.Cell().Element(StLegend).Text(code).Bold().FontSize(8.5f);
				t.Cell().Element(StLegend).Text(description).FontSize(7.5f);
			}
		});

		row.RelativeItem(rest).PaddingTop(40).Row(sig =>
		{
			sig.RelativeItem(1);   // gap after the legend
			for (var s = 0; s < ShiftTableTexts.Signatures.Length; s++)
			{
				var (role, name) = ShiftTableTexts.Signatures[s];
				sig.RelativeItem(4).Column(block =>
				{
					block.Item().AlignCenter().Text(role).Bold().FontSize(10);
					block.Item().Height(90);   // space to sign
					foreach (var line in name)
						block.Item().AlignCenter().Text(line).Bold().FontSize(10);
				});
				sig.RelativeItem(1);   // gap between / after the blocks
			}
		});
	});

	// Shift-table cell styles (borders as on the form: medium for headers and totals, thin for the grid).
	private static IContainer StH(IContainer c) => c.Border(1.2f).Padding(2).AlignCenter().AlignMiddle();
	private static IContainer StD(IContainer c) => c.Border(0.5f).MinHeight(13).Padding(1).AlignCenter().AlignMiddle();
	private static IContainer StNameCell(IContainer c) => c.Border(0.5f).MinHeight(13).PaddingHorizontal(4).AlignLeft().AlignMiddle();
	private static IContainer StTot(IContainer c) => c.Border(1.2f).MinHeight(13).Padding(1).AlignCenter().AlignMiddle();
	private static IContainer StLegend(IContainer c) => c.Border(0.5f).MinHeight(11).Padding(1).AlignCenter().AlignMiddle();

	/// <summary>
	/// Weekend / holiday shading of a whole day cell. Applied before the border / padding /
	/// alignment of the cell style, so it fills the cell and not just the area behind the text.
	/// </summary>
	private static IContainer StShade(IContainer c, bool shaded) => shaded ? c.Background(ShiftTableTexts.ShadeHex) : c;
}
