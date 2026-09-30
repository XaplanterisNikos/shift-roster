using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Vardiologio.Application.Reports;

namespace Vardiologio.Infrastructure.Reports;

/// <summary>
/// Individual overtime certificate ("ΠΙΣΤΟΠΟΙΗΣΗ ΥΠΕΡΩΡΙΑΚΗΣ ΚΑΙ ΛΟΙΠΗΣ ΕΡΓΑΣΙΑΣ") — one A4
/// landscape page laid out as the organisation's sheet.
/// </summary>
public partial class QuestPdfReportRenderer
{
	/// <inheritdoc/>
	public byte[] Render(OvertimeCertificateModel m)
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
					col.Item().Element(c => CertHead(c, m));
					col.Item().PaddingTop(10).Element(c => CertIntro(c, m));
					col.Item().PaddingTop(10).Element(c => CertTable(c, m));
					col.Item().PaddingTop(10).Element(CertDeclaration);
					col.Item().PaddingTop(12).Element(c => CertSignature(c, m));
				});
			});
		});

		return doc.GeneratePdf();
	}

	/// <summary>Emblem + letterhead (left), titles (centre), hand-filled labels (right).</summary>
	private static void CertHead(IContainer c, OvertimeCertificateModel m) => c.Row(row =>
	{
		// Left: emblem above the letterhead.
		row.ConstantItem(230).Column(left =>
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

		// Centre: the two title lines, level with the bottom of the letterhead (as on the sheet).
		row.RelativeItem().AlignBottom().Column(mid =>
		{
			mid.Item().AlignCenter().Text(OvertimeCertificateTexts.Title(m.Year, m.Month)).Bold().FontSize(10.5f);
			mid.Item().AlignCenter().Text(OvertimeCertificateTexts.Subtitle).Bold().FontSize(10.5f);
		});

		// Right: labels filled in by hand. One line per letterhead line, in the same font size,
		// so each label sits level with its letterhead line; the other lines stay empty.
		row.ConstantItem(150).PaddingTop(47).Column(right =>
		{
			var lh = AttendanceSheetTexts.Letterhead;
			for (var i = 0; i < lh.Length; i++)
			{
				var label = OvertimeCertificateTexts.HandFilled.FirstOrDefault(h => h.Line == i).Text;
				right.Item().Text(label ?? " ").Bold().FontSize(7.5f);
			}
		});
	});

	/// <summary>Four centred introduction lines.</summary>
	private static void CertIntro(IContainer c, OvertimeCertificateModel m) => c.Column(col =>
	{
		foreach (var line in OvertimeCertificateTexts.Intro(m.From, m.To))
			col.Item().AlignCenter().Text(line).FontSize(9.5f);
	});

	/// <summary>One-row table: A/A, surname, first name, speciality, six hour columns.</summary>
	private static void CertTable(IContainer c, OvertimeCertificateModel m) => c.Table(t =>
	{
		t.ColumnsDefinition(cols =>
		{
			cols.ConstantColumn(34);                           // A/A
			cols.RelativeColumn(1.6f);                         // Επίθετο
			cols.RelativeColumn(1.3f);                         // Όνομα
			cols.RelativeColumn(1.6f);                         // κλάδος/ειδικότητα
			for (var k = 0; k < 6; k++) cols.RelativeColumn(); // hour columns
		});

		// Row 1 — single headers (two rows high) + group headers (two columns wide).
		t.Cell().RowSpan(2).Element(CertH).Text(OvertimeCertificateTexts.HeadIndex).Bold().FontSize(9);
		t.Cell().RowSpan(2).Element(CertH).Text(OvertimeCertificateTexts.HeadLastName).Bold().FontSize(9);
		t.Cell().RowSpan(2).Element(CertH).Text(OvertimeCertificateTexts.HeadFirstName).Bold().FontSize(9);
		t.Cell().RowSpan(2).Element(CertH).Text(OvertimeCertificateTexts.HeadSpeciality).Bold().FontSize(7);
		for (var g = 0; g < OvertimeCertificateTexts.Groups.Length; g++)
		{
			var text = t.Cell().ColumnSpan(2).Element(CertH).Text(OvertimeCertificateTexts.Groups[g]).Bold().FontSize(7);
			if (g == 0) text.Underline();   // underlined on the form
		}

		// Row 2 — sub-column headers.
		foreach (var sub in OvertimeCertificateTexts.SubHeads)
			t.Cell().Element(CertH).Text(sub).Bold().FontSize(6.5f);

		// Data row.
		t.Cell().Element(CertD).Text(m.Index.ToString()).Bold().FontSize(10);
		t.Cell().Element(CertD).Text(GreekText.Upper(m.LastName)).Bold().FontSize(10);
		t.Cell().Element(CertD).Text(GreekText.Upper(m.FirstName)).Bold().FontSize(10);
		t.Cell().Element(CertD).Text(GreekText.Upper(m.Speciality)).Bold().FontSize(7);
		foreach (var v in OvertimeCertificateTexts.Values(m))
			t.Cell().Element(CertD).Text(v).Bold().FontSize(11);
	});

	/// <summary>"Υπεύθυνη Δήλωση", the four numbered items and the certification paragraph.</summary>
	private static void CertDeclaration(IContainer c) => c.Column(col =>
	{
		col.Item().AlignCenter().Text(OvertimeCertificateTexts.DeclarationTitle).FontSize(10.5f);
		col.Item().Height(8);

		var items = OvertimeCertificateTexts.Declarations;
		for (var i = 0; i < items.Length; i++)
		{
			var number = $"{i + 1}.";   // copied out of the loop variable before the lambda
			var text = items[i];
			col.Item().PaddingBottom(4).PaddingLeft(20).Row(row =>
			{
				row.ConstantItem(18).Text(number).Bold();
				row.RelativeItem().Text(text).Bold();
			});
		}

		col.Item().PaddingTop(2).Text(OvertimeCertificateTexts.Certification).Bold();
	});

	/// <summary>"Ο/Η Βεβαιών Υπάλληλος", space to sign, name.</summary>
	private static void CertSignature(IContainer c, OvertimeCertificateModel m) => c.Column(col =>
	{
		col.Item().AlignCenter().Text(OvertimeCertificateTexts.SignatureRole(m.IsFemale)).FontSize(10);
		col.Item().Height(34);
		col.Item().AlignCenter().Text(OvertimeCertificateTexts.SignatureName(m)).Bold().FontSize(10);
	});

	// Certificate cell styles.
	private static IContainer CertH(IContainer c) => c.Border(0.75f).Padding(2).AlignCenter().AlignMiddle();                 // header cell
	private static IContainer CertD(IContainer c) => c.Border(0.75f).MinHeight(24).Padding(2).AlignCenter().AlignMiddle();   // data cell
}
