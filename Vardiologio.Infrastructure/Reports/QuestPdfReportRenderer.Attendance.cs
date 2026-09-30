using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Vardiologio.Application.Reports;

namespace Vardiologio.Infrastructure.Reports;

/// <summary>
/// Attendance report ("ΠΙΝΑΚΑΣ ΠΑΡΟΥΣΙΩΝ ΠΡΟΣΩΠΙΚΟΥ") — one A4 landscape page per fortnight × table:
/// emblem + letterhead + titles in the header, the table in the content, the signatures in the
/// footer (so they always sit at the bottom of every page).
/// </summary>
public partial class QuestPdfReportRenderer
{
	/// <summary>Width of the letterhead column (mirrored on the right so the titles stay centred).</summary>
	private const float AttSideWidth = 205;

	/// <inheritdoc/>
	public byte[] Render(AttendanceReportModel m)
	{
		var doc = Document.Create(container =>
		{
			foreach (var p in m.Pages())
			{
				container.Page(page =>
				{
					page.Size(PageSizes.A4.Landscape());
					page.Margin(20);
					page.DefaultTextStyle(x => x.FontFamily("Lato").FontSize(8));

					page.Header().Element(h => AttHead(h, m.Sheet, p.From, p.To));
					page.Content().PaddingTop(6).Element(c => AttTable(c, p.Table, p.From, p.To, p.Rows));
					page.Footer().Element(f => AttSignatures(f, m.Sheet));
				});
			}
		});

		return doc.GeneratePdf();
	}

	/// <summary>Addressee (directorate only), emblem + letterhead on the left, titles + period in the centre.</summary>
	private static void AttHead(IContainer c, AttendanceSheet sheet, DateOnly from, DateOnly to) => c.Column(col =>
	{
		var formal = AttendanceSheetTexts.IsFormal(sheet);

		// Directorate only: addressee above everything, bold and centred.
		var addressee = AttendanceSheetTexts.Addressee(sheet);
		if (addressee is not null)
			col.Item().PaddingBottom(4).AlignCenter().Text(addressee).Bold().FontSize(10);

		col.Item().Row(row =>
		{
			// Emblem above the letterhead, as on the original sheet.
			row.ConstantItem(AttSideWidth).Column(left =>
			{
				left.Item().Width(46).Height(44).Image(AttendanceSheetTexts.Emblem).FitArea();
				left.Item().Height(3);

				var lh = AttendanceSheetTexts.Letterhead;
				for (var i = 0; i < lh.Length; i++)
				{
					var line = left.Item().Text(lh[i]).FontSize(7.5f);
					if (i < AttendanceSheetTexts.LetterheadBoldLines) line.Bold();
				}
			});

			// Titles start level with the letterhead (under the emblem's height).
			row.RelativeItem().PaddingTop(47).Column(mid =>
			{
				foreach (var title in AttendanceSheetTexts.Titles(sheet))
					mid.Item().AlignCenter().Text(title).Bold().FontSize(9.5f);
				if (formal)
				{
					// "Από [date] έως [date]" with the dates bold and underlined, as on the directorate sheet.
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

			// Empty mirror of the letterhead column.
			row.ConstantItem(AttSideWidth);
		});
	});

	/// <summary>Table caption, then name / speciality / one column per day of the fortnight.</summary>
	private static void AttTable(IContainer c, int table, DateOnly from, DateOnly to, AttendanceReportRow[] rows) => c.Column(col =>
	{
		var days = to.Day - from.Day + 1;

		col.Item().PaddingBottom(2).Text(AttendanceSheetTexts.TableCaption(table)).Bold().FontSize(9);

		col.Item().Table(t =>
		{
			t.ColumnsDefinition(cols =>
			{
				cols.ConstantColumn(120);                              // ΟΝΟΜΑΤΕΠΩΝΥΜΟ
				cols.ConstantColumn(140);                              // ΕΙΔΙΚΟΤΗΤΑ (longest specialities)
				for (var i = 0; i < days; i++) cols.RelativeColumn();  // days of the fortnight
			});

			t.Header(h =>
			{
				// Row 1 — name / speciality (two rows high) + day numbers.
				h.Cell().RowSpan(2).Element(AttH).Text("ΟΝΟΜΑΤΕΠΩΝΥΜΟ").Bold();
				h.Cell().RowSpan(2).Element(AttH).Text("ΕΙΔΙΚΟΤΗΤΑ").Bold();
				for (var i = 0; i < days; i++)
					h.Cell().Element(AttH).Text(from.AddDays(i).Day.ToString()).Bold();

				// Row 2 — weekday name under each day number.
				for (var i = 0; i < days; i++)
					h.Cell().Element(AttHDayName).Text(AttendanceSheetTexts.DayName(from.AddDays(i))).FontSize(5.3f);
			});

			foreach (var r in rows)
			{
				t.Cell().Element(AttDLeft).Text(r.FullName);
				t.Cell().Element(AttDLeft).Text(r.Speciality).FontSize(6.5f);
				for (var i = 0; i < days; i++)
					t.Cell().Element(AttD).Text(r.Mark(from.Day + i)).FontSize(9);
			}
		});
	});

	/// <summary>Signature blocks: role lines, space to sign, name / title lines.</summary>
	private static void AttSignatures(IContainer c, AttendanceSheet sheet) => c.PaddingTop(8).Row(row =>
	{
		var signatures = AttendanceSheetTexts.Signatures(sheet);
		var roleRows = signatures.Max(s => s.Role.Length);
		var formal = AttendanceSheetTexts.IsFormal(sheet);
		var size = formal ? 9.5f : 8.5f;   // formal: bold, slightly larger, taller signing space
		var gap = formal ? 70f : 34f;

		// A single signature goes to the right half, as on the original sheet.
		if (signatures.Length == 1) row.RelativeItem();

		foreach (var (role, name) in signatures)
		{
			row.RelativeItem().Column(col =>
			{
				foreach (var line in role)
					SignatureLine(col, line, size, formal);

				// Keep the name lines of every block level, whatever its number of role lines.
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

	// Attendance cell styles (no shading: the original sheet is plain black and white).
	private static IContainer AttH(IContainer c) => c.Border(0.75f).Padding(2).AlignCenter().AlignMiddle();                        // header cell
	private static IContainer AttHDayName(IContainer c) => c.Border(0.75f).PaddingVertical(2).AlignCenter().AlignMiddle();              // weekday name: no side padding, "ΠΑΡΑΣΚΕΥΗ" needs the full width
	private static IContainer AttD(IContainer c) => c.Border(0.5f).MinHeight(16).PaddingHorizontal(1).AlignCenter().AlignMiddle(); // day mark
	private static IContainer AttDLeft(IContainer c) => c.Border(0.5f).MinHeight(16).PaddingHorizontal(3).AlignLeft().AlignMiddle(); // name / speciality
}
