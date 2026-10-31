using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Vardiologio.Application.ReportColors;
using Vardiologio.Application.Reports;

namespace Vardiologio.Infrastructure.Reports;

/// <summary>
/// Collective overtime certificate per contract ("ΙΔΟΧ / ΙΔΑΧ / Μόνιμοι") — A4 landscape, as many
/// pages as the employees need. Same top as the individual certificate (reuses CertHead /
/// CertIntro); one table with the three contracts one under the other, each under a coloured
/// band, the table header repeated on every page, and the signatures once at the end, always on
/// the same page as the last employee.
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

					col.Item().PaddingTop(10).Element(c => ContractTable(c, m));
				});

				page.Footer().PaddingTop(4).Element(c => PageNumbers(c));
			});
		});

		return doc.GeneratePdf();
	}

	/// <summary>
	/// The table of the form: header (repeated on every page it runs onto), the department band,
	/// then for each contract its band and one row per employee, one contract under the other.
	/// The signatures close the table and print on the same page as the last employee
	/// (see <see cref="RowsWithClosing"/>).
	/// </summary>
	private static void ContractTable(IContainer c, ContractCertificateModel m) => c.Table(t =>
	{
		var bandColor = m.Colors[ReportColorCatalog.ContractCertificateBand];

		void Columns(TableColumnsDefinitionDescriptor cols)
		{
			cols.ConstantColumn(26);
			cols.RelativeColumn(1.7f);
			cols.RelativeColumn(1.3f);
			cols.RelativeColumn(1.5f);
			for (var k = 0; k < 6; k++) cols.RelativeColumn();
		}
		t.ColumnsDefinition(Columns);

		t.Header(h =>
		{
			h.Cell().RowSpan(2).Element(CertH).Text(OvertimeCertificateTexts.HeadIndex).Bold().FontSize(8);
			h.Cell().RowSpan(2).Element(CertH).Text(OvertimeCertificateTexts.HeadLastName).Bold().FontSize(9);
			h.Cell().RowSpan(2).Element(CertH).Text(OvertimeCertificateTexts.HeadFirstName).Bold().FontSize(9);
			h.Cell().RowSpan(2).Element(CertH).Text(ContractCertificateTexts.HeadSpeciality).Bold().FontSize(9);
			for (var g = 0; g < OvertimeCertificateTexts.Groups.Length; g++)
			{
				var text = h.Cell().ColumnSpan(2).Element(CertH).Text(OvertimeCertificateTexts.Groups[g]).Bold().FontSize(7);
				if (g == 0) text.Underline();
			}

			foreach (var sub in OvertimeCertificateTexts.SubHeads)
				h.Cell().Element(CertH).Text(sub).Bold().FontSize(6.5f);
		});

		var items = new List<TableItem>();

		void AddBand(string text)
		{
			if (text.Length > 0)
				items.Add(new TableItem(true, x =>
					x.Cell().ColumnSpan(ContractColumns).Element(b => ContractBandCell(b, bandColor)).Text(text).Bold().FontSize(8)));
		}

		AddBand(ContractCertificateTexts.Department(m.Texts));

		foreach (var group in m.Groups)
		{
			AddBand(ContractCertificateTexts.GroupTitle(m.Texts, group.Kind));

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
		}

		RowsWithClosing(t, ContractColumns, Columns, items,
			s => s.PaddingTop(14).Element(x => AttSignatures(x, AttendanceSheet.Staff, m.Texts)));
	});

	private static IContainer ContractD(IContainer c) => c.Border(0.75f).MinHeight(17).Padding(1).AlignCenter().AlignMiddle();
	private static IContainer ContractBandCell(IContainer c, string color) =>
		c.Border(0.75f).Background(color).PaddingVertical(1.5f).AlignCenter().AlignMiddle();
}
