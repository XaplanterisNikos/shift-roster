using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Vardiologio.Application.ReportColors;
using Vardiologio.Application.Reports;

namespace Vardiologio.Infrastructure.Reports;

/// <summary>
/// Renders the Σ.Ω. monthly roster to PDF using QuestPDF: emblem + letterhead, titles and
/// addressee above the table, the legend of shift codes and the signatures below it. The texts
/// are the user's (Παράμετροι → Αναφορών) and are formatted like those of the shift table.
/// Partial: the hours report lives in QuestPdfReportRenderer.Hours.cs and reuses the helpers here.
/// </summary>
public partial class QuestPdfReportRenderer : IPdfReportRenderer
{
	private static readonly string[] MonthsGen =
		{ "ΙΑΝΟΥΑΡΙΟΥ","ΦΕΒΡΟΥΑΡΙΟΥ","ΜΑΡΤΙΟΥ","ΑΠΡΙΛΙΟΥ","ΜΑΪΟΥ","ΙΟΥΝΙΟΥ",
		  "ΙΟΥΛΙΟΥ","ΑΥΓΟΥΣΤΟΥ","ΣΕΠΤΕΜΒΡΙΟΥ","ΟΚΤΩΒΡΙΟΥ","ΝΟΕΜΒΡΙΟΥ","ΔΕΚΕΜΒΡΙΟΥ" };
	private static readonly string[] WdGr = { "Δε", "Τρ", "Τε", "Πε", "Πα", "Σα", "Κυ" };


	/// <inheritdoc/>
	public byte[] Render(ReportModel m)
	{
		var days = m.DaysInMonth;

		var doc = Document.Create(container =>
		{
			container.Page(page =>
			{
				page.Size(PageSizes.A4.Landscape());
				page.Margin(15);
				page.DefaultTextStyle(x => x.FontFamily("Lato").FontSize(6));

				// Normal flow over as many A4 pages as the employees need: the letterhead once at
				// the top, the table (its header rows repeat on every page), then the legend and
				// the signatures once after the last row, kept together.
				page.Content().Column(col =>
				{
					col.Item().Element(c => Head(c, m));
					col.Item().PaddingTop(6).Element(c => Table(c, m, days));
					col.Item().PaddingTop(10).ShowEntire().Element(c => Bottom(c, m));
				});
			});
		});

		return doc.GeneratePdf();
	}

	/// <summary>
	/// Header of the sheet: emblem + letterhead (left), titles (centre), addressee (right).
	/// Same arrangement and position-based formatting as the official shift table.
	/// </summary>
	private static void Head(IContainer c, ReportModel m) => c.Row(row =>
	{
		row.ConstantItem(215).Column(left =>
		{
			left.Item().PaddingLeft(20).Width(44).Height(42).Image(AttendanceSheetTexts.Emblem).FitArea();
			left.Item().Height(3);
			foreach (var (plain, bold) in ShiftTableTexts.Letterhead(m.Texts))
			{
				// Only the bold part is bold (whole line, nothing, or just the e-mail address).
				left.Item().Text(t =>
				{
					if (plain.Length > 0) t.Span(plain).FontSize(7.5f);
					if (bold.Length > 0) t.Span(bold).FontSize(7.5f).Bold();
				});
			}
		});

		row.RelativeItem().PaddingTop(4).Column(mid =>
		{
			foreach (var title in ShiftTableTexts.Titles(m.Texts, m.Year, m.Month))
			{
				var span = mid.Item().PaddingBottom(title.Large ? 6 : 2).AlignCenter().Text(title.Text).FontSize(title.Large ? 13 : 10.5f);
				if (title.Bold) span.Bold();
			}
		});

		row.ConstantItem(215).PaddingTop(20).Column(right =>
		{
			foreach (var line in ShiftTableTexts.Addressee(m.Texts))
				right.Item().AlignCenter().Text(line).Bold().FontSize(9);
		});
	});

	/// <summary>Legend of the shift codes on the left, the signatures beside it (as on the shift table).</summary>
	private static void Bottom(IContainer c, ReportModel m) => c.Row(row =>
	{
		var hdrBg = m.Colors[ReportColorCatalog.MonthlyTotalHeader];

		row.ConstantItem(170).Table(t =>
		{
			t.ColumnsDefinition(cols => { cols.ConstantColumn(60); cols.RelativeColumn(); });
			t.Cell().ColumnSpan(2).Element(x => Filled(x, hdrBg)).Text(ShiftTableTexts.LegendTitle).Bold();
			foreach (var (code, description) in m.WorkCodes.Concat(m.StatusCodes))
			{
				t.Cell().Element(D).Text(code).Bold();
				t.Cell().Element(D).Text(description);
			}
		});

		row.RelativeItem().PaddingTop(30).Row(sig =>
		{
			sig.RelativeItem(1);   // gap after the legend
			foreach (var (role, name) in ShiftTableTexts.Signatures(m.Texts))
			{
				sig.RelativeItem(4).Column(block =>
				{
					block.Item().AlignCenter().Text(role).Bold().FontSize(9);
					block.Item().Height(60);   // space to sign
					foreach (var line in name)
						block.Item().AlignCenter().Text(line).Bold().FontSize(9);
				});
				sig.RelativeItem(1);   // gap between / after the blocks
			}
		});
	});

	/// <summary>Builds the roster table (headers + one row per employee).</summary>
	private static void Table(IContainer c, ReportModel m, int days) => c.Table(table =>
	{
		// Cell styles with the report's colours (Παράμετροι → Αναφορών → Χρώματα).
		var hdrBg = m.Colors[ReportColorCatalog.MonthlyTotalHeader];
		var satBg = m.Colors[ReportColorCatalog.MonthlyTotalSaturday];
		var sunBg = m.Colors[ReportColorCatalog.MonthlyTotalSunday];
		var totBg = m.Colors[ReportColorCatalog.MonthlyTotalTotals];
		Func<IContainer, IContainer> H = cell => Filled(cell, hdrBg);   // header cell
		Func<IContainer, IContainer> T = cell => Filled(cell, totBg);   // totals-header cell
		IContainer HDay(IContainer cell, DateOnly day) =>         // day header with weekend shading
			Filled(cell, day.DayOfWeek == DayOfWeek.Sunday ? sunBg : day.DayOfWeek == DayOfWeek.Saturday ? satBg : hdrBg, 1);
		IContainer DDay(IContainer cell, DateOnly day) =>         // day data cell with weekend shading
			(day.DayOfWeek == DayOfWeek.Sunday ? cell.Background(sunBg) : day.DayOfWeek == DayOfWeek.Saturday ? cell.Background(satBg) : cell)
				.Border(0.5f).Padding(1).AlignCenter().AlignMiddle();

		// Relative widths, so the 40-odd columns always fill the width of the A4 page.
		table.ColumnsDefinition(cols =>
		{
			cols.RelativeColumn(0.9f);                                 // Α/Α
			cols.RelativeColumn(5.2f);                                 // ΟΝΟΜΑΤΕΠΩΝΥΜΟ
			cols.RelativeColumn(3.9f);                                 // ΕΙΔΙΚΟΤΗΤΑ
			for (int d = 0; d < days; d++) cols.RelativeColumn(1);     // ημέρες
			for (int k = 0; k < 7; k++) cols.RelativeColumn(1);        // μετρήσεις 1..7
			cols.RelativeColumn(1.7f);                                 // ΗΜΕΡΗΣΙΑ
			cols.RelativeColumn(1.9f);                                 // ΚΥΡΙΑΚΩΝ
		});

		table.Header(header =>
		{
			// Row 1 — group headers
			header.Cell().RowSpan(3).Element(H).Text("Α/Α");
			header.Cell().RowSpan(3).Element(H).AlignLeft().Text("ΟΝΟΜΑΤΕΠΩΝΥΜΟ");
			header.Cell().RowSpan(3).Element(H).AlignLeft().Text("ΕΙΔΙΚΟΤΗΤΑ");
			header.Cell().ColumnSpan((uint)days).Element(H).Text("");                       // spans above the day columns
			header.Cell().ColumnSpan(7).Element(H).Text("ΒΑΡΔΙΑ ΕΡΓΑΣΙΑΣ");
			header.Cell().ColumnSpan(2).Element(T).Text("ΕΡΓΑΣΙΑ ΠΡΟΣ ΣΥΜΠΛΗΡΩΣΗ");

			// Row 2 — day numbers + counts 1..7 + (rowspan) totals
			for (int d = 1; d <= days; d++)
				header.Cell().Element(x => HDay(x, new DateOnly(m.Year, m.Month, d))).Text(d.ToString());
			for (int k = 1; k <= 7; k++) header.Cell().Element(H).Text(k.ToString());
			header.Cell().RowSpan(2).Element(T).Text("ΗΜΕΡΗΣΙΑ");
			header.Cell().RowSpan(2).Element(T).Text("ΚΥΡΙΑΚΩΝ ΕΞΑΙΡ.");

			// Row 3 — weekday letter under each day
			for (int d = 1; d <= days; d++)
			{
				var date = new DateOnly(m.Year, m.Month, d);
				header.Cell().Element(x => HDay(x, date)).Text(WdGr[((int)date.DayOfWeek + 6) % 7]);
			}
			for (int k = 1; k <= 7; k++) header.Cell().Element(H).Text("");
		});

		foreach (var r in m.Rows)
		{
			table.Cell().Element(D).Text(r.Index.ToString());
			table.Cell().Element(D).AlignLeft().Text($"{r.LastName} {r.FirstName}".Trim());
			table.Cell().Element(D).AlignLeft().Text(r.Speciality);
			for (int d = 1; d <= days; d++)
			{
				var date = new DateOnly(m.Year, m.Month, d);
				table.Cell().Element(x => DDay(x, date)).Text(r.DayCodes[d - 1] ?? "");
			}
			for (int k = 0; k < 7; k++)
				table.Cell().Element(D).Text(r.ProgramCounts[k] == 0 ? "" : r.ProgramCounts[k].ToString());
			table.Cell().Element(D).Text("");  // ΗΜΕΡΗΣΙΑ total — left blank until the rule is defined
			table.Cell().Element(D).Text("");   // ΚΥΡΙΑΚΩΝ total — left blank until the rule is defined
		}
	});

	// Cell styles (borders / background / alignment). The fills take their colour as an
	// argument, because the colours are the user's (Παράμετροι → Αναφορών → Χρώματα).

	/// <summary>A bordered, centred cell filled with a colour (headers, totals, legend title).</summary>
	private static IContainer Filled(IContainer c, string background, float padding = 2) =>
		c.Border(0.5f).Background(background).Padding(padding).AlignCenter().AlignMiddle();

	/// <summary>A plain data cell.</summary>
	private static IContainer D(IContainer c) => c.Border(0.5f).Padding(1).AlignCenter().AlignMiddle();
}
