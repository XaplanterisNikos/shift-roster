using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Vardiologio.Application.Reports;

namespace Vardiologio.Infrastructure.Reports;

/// <summary>Renders the Σ.Ω. monthly roster to PDF using QuestPDF.</summary>
public class QuestPdfReportRenderer : IPdfReportRenderer
{
	private static readonly string[] MonthsGen =
		{ "ΙΑΝΟΥΑΡΙΟΥ","ΦΕΒΡΟΥΑΡΙΟΥ","ΜΑΡΤΙΟΥ","ΑΠΡΙΛΙΟΥ","ΜΑΪΟΥ","ΙΟΥΝΙΟΥ",
		  "ΙΟΥΛΙΟΥ","ΑΥΓΟΥΣΤΟΥ","ΣΕΠΤΕΜΒΡΙΟΥ","ΟΚΤΩΒΡΙΟΥ","ΝΟΕΜΒΡΙΟΥ","ΔΕΚΕΜΒΡΙΟΥ" };
	private static readonly string[] WdGr = { "Δε", "Τρ", "Τε", "Πε", "Πα", "Σα", "Κυ" };

	private const string HdrBg = "#D9E1F2", SunBg = "#FCE4D6", SatBg = "#FFF2CC", TotBg = "#E2EFDA";

	/// <inheritdoc/>
	public byte[] Render(ReportModel m)
	{
		var days = m.DaysInMonth;

		var doc = Document.Create(container =>
		{
			container.Page(page =>
			{
				page.Size(PageSizes.A3.Landscape());
				page.Margin(15);
				page.DefaultTextStyle(x => x.FontFamily("Lato").FontSize(7));

				page.Header().Element(h => Head(h, m));
				page.Content().PaddingTop(6).Element(c => Table(c, m, days));
				page.Footer().PaddingTop(4)
					.Text("ΥΠΟΜΝΗΜΑ:  αριθμός = κωδικός βάρδιας  |  Ρ=ΡΕΠΟ  Α=ΑΣΘΕΝΕΙΑ  ΑΡ=ΑΡΓΙΑ  Κ=ΚΑΝΟΝΙΚΗ ΑΔΕΙΑ  Δ=ΑΝΑΤΡΟΦΗΣ ΤΕΚΝΟΥ  Ε.Α=ΕΙΔΙΚΗ ΑΔΕΙΑ  Κ.Σ=ΚΑΙΡΙΚΩΝ ΣΥΝΘΗΚΩΝ  Σ=ΣΥΝΔΙΚΑΛΙΣΤΙΚΗ  Α.Τ=ΑΔΕΙΑ ΤΟΚΕΤΟΥ")
					.FontSize(6);
			});
		});

		return doc.GeneratePdf();
	}

	/// <summary>Builds the fixed header block (organisation + title + month).</summary>
	private static void Head(IContainer c, ReportModel m) => c.Row(row =>
	{
		row.RelativeItem().Column(col =>
		{
			col.Item().Text("ΕΛΛΗΝΙΚΗ ΔΗΜΟΚΡΑΤΙΑ").Bold().FontSize(9);
			col.Item().Text("ΠΕΡΙΦΕΡΕΙΑ ΑΤΤΙΚΗΣ").FontSize(8);
			col.Item().Text("ΕΙΔΙΚΟΣ ΔΙΑΒΑΘΜΙΔΙΚΟΣ ΣΥΝΔΕΣΜΟΣ").Bold().FontSize(8);
			col.Item().Text("ΕΔΡΑ: Άντερσεν 6 και Μωραΐτη 90, 11525 Αθήνα").FontSize(7);
		});
		row.RelativeItem().Column(col =>
		{
			col.Item().AlignCenter().Text("ΔΙΕΥΘΥΝΣΗ ΣΤΑΘΜΩΝ ΜΕΤΑΦΟΡΤΩΣΗΣ").Bold().FontSize(10);
			col.Item().AlignCenter().Text("ΠΙΝΑΚΑΣ ΒΑΡΔΙΩΝ ΕΡΓΑΣΙΑΣ ΠΡΟΣΩΠΙΚΟΥ").Bold().FontSize(11);
			col.Item().AlignCenter().Text("ΤΜΗΜΑΤΟΣ ΚΕΝΤΡΙΚΩΝ ΣΜΑ").FontSize(9);
			col.Item().AlignCenter().Text($"ΜΗΝΟΣ {MonthsGen[m.Month - 1]} {m.Year}").Bold().FontSize(10);
		});
		row.RelativeItem().Column(col =>
		{
			col.Item().AlignRight().Text("ΠΡΟΣ: Αναπλ/τρια Προϊστ. Τμ. Λογιστηρίου").FontSize(7);
			col.Item().AlignRight().Text("κ. Αγιανίδου Δέσποινα").FontSize(7);
		});
	});

	/// <summary>Builds the roster table (headers + one row per employee).</summary>
	private static void Table(IContainer c, ReportModel m, int days) => c.Table(table =>
	{
		table.ColumnsDefinition(cols =>
		{
			cols.ConstantColumn(16);                                  // Α/Α
			cols.ConstantColumn(95);                                  // ΟΝΟΜΑΤΕΠΩΝΥΜΟ
			cols.ConstantColumn(72);                                  // ΕΙΔΙΚΟΤΗΤΑ
			for (int d = 0; d < days; d++) cols.ConstantColumn(19);  // ημέρες
			for (int k = 0; k < 7; k++) cols.ConstantColumn(19);   // μετρήσεις 1..7
			cols.ConstantColumn(30);                                  // ΗΜΕΡΗΣΙΑ
			cols.ConstantColumn(34);                                  // ΚΥΡΙΑΚΩΝ
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

	// cell styles (borders/background/alignment)
	private static IContainer H(IContainer c) => c.Border(0.5f).Background(HdrBg).Padding(2).AlignCenter().AlignMiddle();// header cell
	private static IContainer T(IContainer c) => c.Border(0.5f).Background(TotBg).Padding(2).AlignCenter().AlignMiddle();// totals-header cell
	private static IContainer D(IContainer c) => c.Border(0.5f).Padding(1).AlignCenter().AlignMiddle();// data cell
	private static IContainer HDay(IContainer c, DateOnly d) =>
		c.Border(0.5f).Background(d.DayOfWeek == DayOfWeek.Sunday ? SunBg : d.DayOfWeek == DayOfWeek.Saturday ? SatBg : HdrBg).Padding(1).AlignCenter().AlignMiddle();// day header with weekend shading
	private static IContainer DDay(IContainer c, DateOnly d) =>
		(d.DayOfWeek == DayOfWeek.Sunday ? c.Background(SunBg) : d.DayOfWeek == DayOfWeek.Saturday ? c.Background(SatBg) : c).Border(0.5f).Padding(1).AlignCenter().AlignMiddle();// day data cell with weekend shading
}