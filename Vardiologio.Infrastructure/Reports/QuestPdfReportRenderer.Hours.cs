using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Vardiologio.Application.Hours;
using Vardiologio.Application.ReportColors;
using Vardiologio.Application.Reports;
using Vardiologio.Domain.Enums;

namespace Vardiologio.Infrastructure.Reports;

/// <summary>Hours report (declared / paid / excess per category) — reuses H/T/D of the Σ.Ω. part.</summary>
public partial class QuestPdfReportRenderer
{
	/// <inheritdoc/>
	public byte[] Render(HoursReportModel m)
	{
		var doc = Document.Create(container =>
		{
			container.Page(page =>
			{
				page.Size(PageSizes.A4.Landscape());
				page.Margin(20);
				page.DefaultTextStyle(x => x.FontFamily("Lato").FontSize(7));

				page.Header().Element(h => HoursHead(h, m));
				page.Content().PaddingTop(8).Element(c => HoursTable(c, m));
				// The explanation of the columns on the left, the page number on the right.
				page.Footer().PaddingTop(4).Row(row =>
				{
					row.RelativeItem()
						.Text($"ΔΗΛ. = δηλωμένες, ΠΛΗΡ. = πληρώνονται, ΠΕΡ. = περισσεύουν (δεν πληρώνονται).  " +
						      $"Όριο αργίας = {Num(m.Limits[HourCategory.Holiday])} ({m.HolidaysInMonth} αργίες τον μήνα).  " +
						      $"Προς συμπλήρωση + Αργίας: κοινό όριο {Num(m.CombinedLimit)}, κόβεται πρώτα το προς συμπλήρωση.")
						.FontSize(6);
					row.ConstantItem(80).AlignBottom().Element(c => PageNumbers(c));
				});
			});
		});

		return doc.GeneratePdf();
	}

	/// <summary>Title block of the hours report.</summary>
	private static void HoursHead(IContainer c, HoursReportModel m) => c.Column(col =>
	{
		col.Item().AlignCenter().Text("ΔΙΕΥΘΥΝΣΗ ΣΤΑΘΜΩΝ ΜΕΤΑΦΟΡΤΩΣΗΣ — ΤΜΗΜΑ ΚΕΝΤΡΙΚΩΝ ΣΜΑ").Bold().FontSize(9);
		col.Item().AlignCenter().Text($"ΜΗΝΙΑΙΑ ΚΑΤΑΣΤΑΣΗ ΩΡΩΝ — ΜΗΝΟΣ {MonthsGen[m.Month - 1]} {m.Year}").Bold().FontSize(11);
	});

	/// <summary>Employees × (category × declared/paid/excess) table with a totals row.</summary>
	private static void HoursTable(IContainer c, HoursReportModel m) => c.Table(table =>
	{
		var categories = Enum.GetValues<HourCategory>();

		// Cell styles with the report's colours (Παράμετροι → Αναφορών → Χρώματα).
		var hdrBg = m.Colors[ReportColorCatalog.HoursHeader];
		var catBg = m.Colors[ReportColorCatalog.HoursCategories];
		var overBg = m.Colors[ReportColorCatalog.HoursExcess];   // excess (unpaid) hours
		Func<IContainer, IContainer> H = cell => Filled(cell, hdrBg);   // header cell
		Func<IContainer, IContainer> T = cell => Filled(cell, catBg);   // category header / totals cell

		table.ColumnsDefinition(cols =>
		{
			cols.ConstantColumn(18);                                          // Α/Α
			cols.RelativeColumn(3);                                           // ΟΝΟΜΑΤΕΠΩΝΥΜΟ
			cols.RelativeColumn(2);                                           // ΕΙΔΙΚΟΤΗΤΑ
			for (int k = 0; k < categories.Length * 3; k++) cols.ConstantColumn(30);   // 3 per category
		});

		table.Header(header =>
		{
			// Row 1 — category titles with their limit.
			header.Cell().RowSpan(2).Element(H).Text("Α/Α");
			header.Cell().RowSpan(2).Element(H).AlignLeft().Text("ΟΝΟΜΑΤΕΠΩΝΥΜΟ");
			header.Cell().RowSpan(2).Element(H).AlignLeft().Text("ΕΙΔΙΚΟΤΗΤΑ");
			foreach (var cat in categories)
				header.Cell().ColumnSpan(3).Element(T)
					.Text($"{HoursLabels.CategoryHeader(cat)} (ΟΡΙΟ {Num(m.Limits[cat])})");

			// Row 2 — declared / paid / excess under each category.
			for (int i = 0; i < categories.Length; i++)
			{
				header.Cell().Element(T).Text("ΔΗΛ.");
				header.Cell().Element(T).Text("ΠΛΗΡ.");
				header.Cell().Element(T).Text("ΠΕΡ.");
			}
		});

		foreach (var r in m.Rows)
		{
			table.Cell().Element(D).Text(r.Index.ToString());
			table.Cell().Element(D).AlignLeft().Text($"{r.LastName} {r.FirstName}".Trim());
			table.Cell().Element(D).AlignLeft().Text(r.Speciality);
			foreach (var h in r.Hours)
			{
				table.Cell().Element(D).Text(Blank(h.Declared));
				table.Cell().Element(D).Text(Blank(h.Paid));
				// Excess highlighted: these hours are declared but not paid.
				table.Cell().Element(x => h.Excess > 0 ? D(x.Background(overBg)) : D(x)).Text(Blank(h.Excess));
			}
		}

		// Totals row.
		table.Cell().ColumnSpan(3).Element(T).AlignRight().Text("ΣΥΝΟΛΑ");
		foreach (var cat in categories)
		{
			table.Cell().Element(T).Text(Blank(m.Total(cat, h => h.Declared)));
			table.Cell().Element(T).Text(Blank(m.Total(cat, h => h.Paid)));
			table.Cell().Element(T).Text(Blank(m.Total(cat, h => h.Excess)));
		}
	});

	/// <summary>Hours as text ("6,5", "120"), current culture.</summary>
	private static string Num(decimal hours) => hours.ToString("0.##");

	/// <summary>Like <see cref="Num"/> but empty for zero, to keep the table readable.</summary>
	private static string Blank(decimal hours) => hours == 0 ? "" : Num(hours);
}
