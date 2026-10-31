using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Vardiologio.Application.Reports;
using Vardiologio.Application.ReportTexts;

namespace Vardiologio.Infrastructure.Reports;

/// <summary>
/// Attendance report ("ΠΙΝΑΚΑΣ ΠΑΡΟΥΣΙΩΝ ΠΡΟΣΩΠΙΚΟΥ") on A4 landscape. Each fortnight is one document:
/// emblem + letterhead + titles once at the top, the numbered tables (a new page for each), and the
/// signatures once under the last table. Only the page number ("Σελίδα 1 από 2", counted per
/// fortnight) is on every page.
/// </summary>
public partial class QuestPdfReportRenderer
{
	/// <summary>Width of the letterhead column (mirrored on the right so the titles stay centred).</summary>
	private const float AttSideWidth = 205;

	/// <inheritdoc/>
	public byte[] Render(AttendanceReportModel m)
	{
		var tables = m.Tables();

		var doc = Document.Create(container =>
		{
			var fortnight = 0;
			foreach (var (from, to) in m.Fortnights())
			{
				var section = $"fortnight-{++fortnight}";

				container.Page(page =>
				{
					page.Size(PageSizes.A4.Landscape());
					page.Margin(20);
					page.DefaultTextStyle(x => x.FontFamily("Lato").FontSize(8));

					page.Content().Section(section).Column(col =>
					{
						col.Item().Element(h => AttHead(h, m.Sheet, m.Texts, from, to));

						for (var i = 0; i < tables.Count; i++)
						{
							var index = i;

							Action<IContainer>? closing = index == tables.Count - 1
								? s => AttSignatures(s, m.Sheet, m.Texts)
								: null;

							if (index > 0) col.Item().PageBreak();
							col.Item().PaddingTop(6).Element(c => AttTable(c, index + 1, from, to, tables[index], closing));
						}
					});

					page.Footer().PaddingTop(2).Element(c => PageNumbers(c, section));
				});
			}
		});

		return doc.GeneratePdf();
	}

	/// <summary>Addressee (directorate only), emblem + letterhead on the left, titles + period in the centre.</summary>
	private static void AttHead(IContainer c, AttendanceSheet sheet, ReportTextSet texts, DateOnly from, DateOnly to) => c.Column(col =>
	{
		var formal = AttendanceSheetTexts.IsFormal(sheet);

		var addressee = AttendanceSheetTexts.Addressee(texts);
		if (addressee is not null)
			col.Item().PaddingBottom(4).AlignCenter().Text(addressee).Bold().FontSize(10);

		col.Item().Row(row =>
		{
			row.ConstantItem(AttSideWidth).Column(left =>
			{
				left.Item().Width(46).Height(44).Element(EmblemImage);
				left.Item().Height(3);

				foreach (var (text, bold) in AttendanceSheetTexts.Letterhead(texts))
				{
					var line = left.Item().Text(text).FontSize(7.5f);
					if (bold) line.Bold();
				}
			});

			row.RelativeItem().PaddingTop(47).Column(mid =>
			{
				foreach (var title in AttendanceSheetTexts.Titles(texts))
					mid.Item().AlignCenter().Text(title).Bold().FontSize(9.5f);
				if (formal)
				{
					mid.Item().PaddingTop(12).AlignCenter().Row(period =>
					{
						period.AutoItem().AlignBottom().Text("Από").FontSize(9);
						period.AutoItem().PaddingLeft(14).BorderBottom(0.75f).PaddingHorizontal(10)
							.Text(AttendanceSheetTexts.Date(from)).Bold().FontSize(9);
						period.AutoItem().PaddingLeft(14).AlignBottom().Text("έως").FontSize(9);
						period.AutoItem().PaddingLeft(14).BorderBottom(0.75f).PaddingHorizontal(10)
							.Text(AttendanceSheetTexts.Date(to)).Bold().FontSize(9);
					});
				}
				else
				{
					mid.Item().PaddingTop(2).AlignCenter().Text(AttendanceSheetTexts.Period(from, to)).FontSize(9);
				}
			});

			row.ConstantItem(AttSideWidth);
		});
	});

	/// <summary>
	/// Table caption, then name / speciality / one column per day of the fortnight. The last
	/// table gets the signatures, on the same page as its last employee (see <see cref="RowsWithClosing"/>).
	/// </summary>
	/// <param name="closing">The signatures; null for every table but the last.</param>
	private static void AttTable(IContainer c, int table, DateOnly from, DateOnly to, AttendanceReportRow[] rows,
		Action<IContainer>? closing) => c.Column(col =>
	{
		var days = to.Day - from.Day + 1;

		col.Item().PaddingBottom(2).Text(AttendanceSheetTexts.TableCaption(table)).Bold().FontSize(9);

		col.Item().Table(t =>
		{
			void Columns(TableColumnsDefinitionDescriptor cols)
			{
				cols.ConstantColumn(120);
				cols.ConstantColumn(140);
				for (var i = 0; i < days; i++) cols.RelativeColumn();
			}
			t.ColumnsDefinition(Columns);

			t.Header(h =>
			{
				h.Cell().RowSpan(2).Element(AttH).Text("ΟΝΟΜΑΤΕΠΩΝΥΜΟ").Bold();
				h.Cell().RowSpan(2).Element(AttH).Text("ΕΙΔΙΚΟΤΗΤΑ").Bold();
				for (var i = 0; i < days; i++)
					h.Cell().Element(AttH).Text(from.AddDays(i).Day.ToString()).Bold();

				for (var i = 0; i < days; i++)
					h.Cell().Element(AttHDayName).Text(AttendanceSheetTexts.DayName(from.AddDays(i))).FontSize(5.3f);
			});

			var items = rows.Select(r => new TableItem(false, x =>
			{
				x.Cell().Element(AttDLeft).Text(r.FullName);
				x.Cell().Element(AttDLeft).Text(r.Speciality).FontSize(6.5f);
				for (var i = 0; i < days; i++)
					x.Cell().Element(AttD).Text(r.Mark(from.Day + i)).FontSize(9);
			})).ToList();

			RowsWithClosing(t, (uint)(2 + days), Columns, items, closing);
		});
	});

	/// <summary>Signature blocks: role lines, space to sign, name / title lines.</summary>
	private static void AttSignatures(IContainer c, AttendanceSheet sheet, ReportTextSet texts) => c.PaddingTop(8).Row(row =>
	{
		var signatures = AttendanceSheetTexts.Signatures(texts);
		var roleRows = signatures.Select(s => s.Role.Length).DefaultIfEmpty(0).Max();
		var formal = AttendanceSheetTexts.IsFormal(sheet);
		var size = formal ? 9.5f : 8.5f;
		var gap = formal ? 70f : 34f;

		if (signatures.Length == 1) row.RelativeItem();

		foreach (var (role, name) in signatures)
		{
			row.RelativeItem().Column(col =>
			{
				foreach (var line in role)
					SignatureLine(col, line, size, formal);

				col.Item().Height((roleRows - role.Length) * 11 + gap);

				foreach (var line in name)
					SignatureLine(col, line, size, formal);
			});
		}
	});

	/// <summary>One centred signature line, bold on the formal layout.</summary>
	private static void SignatureLine(ColumnDescriptor col, string text, float size, bool bold)
	{
		var span = col.Item().AlignCenter().Text(text).FontSize(size);
		if (bold) span.Bold();
	}

	private static IContainer AttH(IContainer c) => c.Border(0.75f).Padding(2).AlignCenter().AlignMiddle();
	private static IContainer AttHDayName(IContainer c) => c.Border(0.75f).PaddingVertical(2).AlignCenter().AlignMiddle();
	private static IContainer AttD(IContainer c) => c.Border(0.5f).MinHeight(16).PaddingHorizontal(1).AlignCenter().AlignMiddle();
	private static IContainer AttDLeft(IContainer c) => c.Border(0.5f).MinHeight(16).PaddingHorizontal(3).AlignLeft().AlignMiddle();
}
