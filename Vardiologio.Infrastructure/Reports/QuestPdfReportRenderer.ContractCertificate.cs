using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Vardiologio.Application.ReportColors;
using Vardiologio.Application.Reports;

namespace Vardiologio.Infrastructure.Reports;

/// <summary>
/// Collective overtime certificate per contract ("ΙΔΟΧ / ΙΔΑΧ / Μόνιμοι") — A4 landscape, as many
/// pages as the employees need. Same top as the individual certificate (reuses CertHead /
/// CertIntro); one table per contract under a coloured band, each contract after the first on
/// a new page, the table header repeated on every page, and the signatures once at the end.
/// </summary>
public partial class QuestPdfReportRenderer
{
	/// <summary>Number of columns of the table: A/A, surname, first name, speciality, six hour columns.</summary>
	private const int ContractColumns = 10;

	/// <inheritdoc/>
	public byte[] Render(ContractCertificateModel m)
	{
		var doc = Document.Create(container =>
		{
			container.Page(page =>
			{
				page.Size(PageSizes.A4.Landscape());
				page.Margin(30);
				page.DefaultTextStyle(x => x.FontFamily("Lato").FontSize(9));

				page.Content().Column(col =>
				{
					col.Item().Element(c => CertHead(c, m.Texts, m.Year, m.Month));
					col.Item().PaddingTop(10).Element(c => CertIntro(c, m.Texts, m.From, m.To));

					for (var g = 0; g < m.Groups.Count; g++)
					{
						// Copied out of the loop variable before the lambda below captures them.
						var group = m.Groups[g];
						var isFirst = g == 0;

						// The signatures once, under the last group, on the same page as its last
						// employee (see ContractTable).
						Action<IContainer>? closing = g == m.Groups.Count - 1
							? s => s.PaddingTop(14).Element(x => AttSignatures(x, AttendanceSheet.Staff, m.Texts))
							: null;

						// Every contract after the first starts on a new page.
						if (!isFirst) col.Item().PageBreak();
						col.Item().PaddingTop(isFirst ? 10 : 0).Element(c => ContractTable(c, m, group, isFirst, closing));
					}

					// A form without groups still gets its signatures.
					if (m.Groups.Count == 0)
						col.Item().PaddingTop(14).ShowEntire().Element(c => AttSignatures(c, AttendanceSheet.Staff, m.Texts));
				});

				page.Footer().PaddingTop(4).Element(c => PageNumbers(c));
			});
		});

		return doc.GeneratePdf();
	}

	/// <summary>
	/// The table of one contract: header (repeated on every page it runs onto), the department
	/// band (first group only), the contract band, then one row per employee. The last group
	/// gets the signatures, which print on the same page as its last employee
	/// (see <see cref="RowsWithClosing"/>).
	/// </summary>
	/// <param name="closing">The signatures; null for every group but the last.</param>
	private static void ContractTable(IContainer c, ContractCertificateModel m, ContractCertificateGroup group,
		bool isFirst, Action<IContainer>? closing) => c.Table(t =>
	{
		var bandColor = m.Colors[ReportColorCatalog.ContractCertificateBand];

		// A local function, because the last row is drawn in a table of its own with the same columns.
		void Columns(TableColumnsDefinitionDescriptor cols)
		{
			cols.ConstantColumn(26);                           // A/A
			cols.RelativeColumn(1.7f);                         // Επίθετο
			cols.RelativeColumn(1.3f);                         // Όνομα
			cols.RelativeColumn(1.5f);                         // Ειδικότητα
			for (var k = 0; k < 6; k++) cols.RelativeColumn(); // hour columns
		}
		t.ColumnsDefinition(Columns);

		t.Header(h =>
		{
			// Row 1 — single headers (two rows high) + group headers (two columns wide).
			h.Cell().RowSpan(2).Element(CertH).Text(OvertimeCertificateTexts.HeadIndex).Bold().FontSize(8);
			h.Cell().RowSpan(2).Element(CertH).Text(OvertimeCertificateTexts.HeadLastName).Bold().FontSize(9);
			h.Cell().RowSpan(2).Element(CertH).Text(OvertimeCertificateTexts.HeadFirstName).Bold().FontSize(9);
			h.Cell().RowSpan(2).Element(CertH).Text(ContractCertificateTexts.HeadSpeciality).Bold().FontSize(9);
			for (var g = 0; g < OvertimeCertificateTexts.Groups.Length; g++)
			{
				var text = h.Cell().ColumnSpan(2).Element(CertH).Text(OvertimeCertificateTexts.Groups[g]).Bold().FontSize(7);
				if (g == 0) text.Underline();   // underlined on the form
			}

			// Row 2 — sub-column headers.
			foreach (var sub in OvertimeCertificateTexts.SubHeads)
				h.Cell().Element(CertH).Text(sub).Bold().FontSize(6.5f);
		});

		var items = new List<TableItem>();

		// Bands across the whole table; a band whose text was left empty is not printed.
		var department = isFirst ? ContractCertificateTexts.Department(m.Texts) : "";
		if (department.Length > 0)
			items.Add(new TableItem(true, x =>
				x.Cell().ColumnSpan(ContractColumns).Element(b => ContractBandCell(b, bandColor)).Text(department).Bold().FontSize(8)));

		var title = ContractCertificateTexts.GroupTitle(m.Texts, group.Kind);
		if (title.Length > 0)
			items.Add(new TableItem(true, x =>
				x.Cell().ColumnSpan(ContractColumns).Element(b => ContractBandCell(b, bandColor)).Text(title).Bold().FontSize(8)));

		// One row per employee.
		foreach (var row in group.Rows)
		{
			items.Add(new TableItem(false, x =>
			{
				x.Cell().Element(ContractD).Text(row.Index.ToString()).FontSize(7);
				x.Cell().Element(ContractD).Text(GreekText.Upper(row.LastName)).Bold().FontSize(8.5f);
				x.Cell().Element(ContractD).Text(GreekText.Upper(row.FirstName)).Bold().FontSize(8.5f);
				x.Cell().Element(ContractD).Text(GreekText.Upper(row.Speciality)).Bold().FontSize(6.5f);
				foreach (var v in ContractCertificateTexts.Values(row))
					x.Cell().Element(ContractD).Text(v).Bold().FontSize(9.5f);
			}));
		}

		RowsWithClosing(t, ContractColumns, Columns, items, closing);
	});

	// Contract certificate cell styles.
	private static IContainer ContractD(IContainer c) => c.Border(0.75f).MinHeight(17).Padding(1).AlignCenter().AlignMiddle();   // data cell
	private static IContainer ContractBandCell(IContainer c, string color) =>
		c.Border(0.75f).Background(color).PaddingVertical(1.5f).AlignCenter().AlignMiddle();                                   // coloured band
}
