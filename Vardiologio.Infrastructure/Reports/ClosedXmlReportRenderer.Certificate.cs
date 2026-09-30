using ClosedXML.Excel;
using Vardiologio.Application.Reports;

namespace Vardiologio.Infrastructure.Reports;

/// <summary>
/// Individual overtime certificate ("ΠΙΣΤΟΠΟΙΗΣΗ ΥΠΕΡΩΡΙΑΚΗΣ ΚΑΙ ΛΟΙΠΗΣ ΕΡΓΑΣΙΑΣ") — one A4
/// landscape page laid out as the organisation's sheet. Reuses Put/Merge of the Σ.Ω. part.
/// </summary>
public partial class ClosedXmlReportRenderer
{
	// Columns: A/A, surname, first name, speciality, then the six hour columns.
	private const int CertIndex = 1, CertLast = 2, CertFirst = 3, CertSpec = 4, CertValue0 = 5;
	private const int CertLastCol = CertValue0 + 5;

	/// <inheritdoc/>
	public byte[] Render(OvertimeCertificateModel m)
	{
		using var wb = new XLWorkbook();
		var ws = wb.AddWorksheet("ΒΕΒΑΙΩΣΗ");
		var left = XLAlignmentHorizontalValues.Left;
		var center = XLAlignmentHorizontalValues.Center;

		// Emblem: three rows high, top left, above the letterhead.
		for (var r = 1; r <= 3; r++) ws.Row(r).Height = 15;
		using (var emblem = new MemoryStream(AttendanceSheetTexts.Emblem))   // ClosedXML copies the image
			ws.AddPicture(emblem, "Emblem").MoveTo(ws.Cell(1, CertIndex), 6, 2).WithSize(58, 56);

		// Letterhead (left) with the hand-filled labels on the right of some of its lines.
		const int lh0 = 4;
		var lh = AttendanceSheetTexts.Letterhead;
		for (var i = 0; i < lh.Length; i++)
		{
			Merge(ws, lh0 + i, CertIndex, lh0 + i, CertFirst, lh[i], 9, i < AttendanceSheetTexts.LetterheadBoldLines, left, null);
			ws.Row(lh0 + i).Height = 13;
		}
		foreach (var (line, text) in OvertimeCertificateTexts.HandFilled)
			Merge(ws, lh0 + line, CertLastCol - 1, lh0 + line, CertLastCol, text, 10, true, left, null);

		// Titles on the last letterhead line and the one after it (as on the sheet).
		var rTitle = lh0 + lh.Length - 1;
		Merge(ws, rTitle, CertSpec, rTitle, CertLastCol, OvertimeCertificateTexts.Title(m.Year, m.Month), 11, true, center, null);
		Merge(ws, rTitle + 1, CertSpec, rTitle + 1, CertLastCol, OvertimeCertificateTexts.Subtitle, 11, true, center, null);
		ws.Row(rTitle + 1).Height = 15;

		// Introduction: four centred lines.
		var r0 = rTitle + 3;
		var intro = OvertimeCertificateTexts.Intro(m.From, m.To);
		for (var i = 0; i < intro.Length; i++)
		{
			Merge(ws, r0 + i, CertIndex, r0 + i, CertLastCol, intro[i], 10, false, center, null);
			ws.Row(r0 + i).Height = 14;
		}

		// Table: two header rows, one data row.
		var rHead = r0 + intro.Length + 1;
		Merge(ws, rHead, CertIndex, rHead + 1, CertIndex, OvertimeCertificateTexts.HeadIndex, 10, true, center, null);
		Merge(ws, rHead, CertLast, rHead + 1, CertLast, OvertimeCertificateTexts.HeadLastName, 10, true, center, null);
		Merge(ws, rHead, CertFirst, rHead + 1, CertFirst, OvertimeCertificateTexts.HeadFirstName, 10, true, center, null);
		Merge(ws, rHead, CertSpec, rHead + 1, CertSpec, OvertimeCertificateTexts.HeadSpeciality, 8, true, center, null);
		for (var g = 0; g < OvertimeCertificateTexts.Groups.Length; g++)
		{
			var c = CertValue0 + g * 2;
			Merge(ws, rHead, c, rHead, c + 1, OvertimeCertificateTexts.Groups[g], 8, true, center, null);
		}
		ws.Cell(rHead, CertValue0).Style.Font.Underline = XLFontUnderlineValues.Single;   // underlined on the form
		for (var k = 0; k < OvertimeCertificateTexts.SubHeads.Length; k++)
			Merge(ws, rHead + 1, CertValue0 + k, rHead + 1, CertValue0 + k, OvertimeCertificateTexts.SubHeads[k], 7, true, center, null);
		ws.Row(rHead).Height = 30;
		ws.Row(rHead + 1).Height = 30;

		var rData = rHead + 2;
		Put(ws, rData, CertIndex, m.Index, 11, true, null);
		Put(ws, rData, CertLast, GreekText.Upper(m.LastName), 11, true, null);
		Put(ws, rData, CertFirst, GreekText.Upper(m.FirstName), 11, true, null);
		Merge(ws, rData, CertSpec, rData, CertSpec, GreekText.Upper(m.Speciality), 8, true, center, null);
		var values = OvertimeCertificateTexts.Values(m);
		for (var k = 0; k < values.Length; k++)
			Put(ws, rData, CertValue0 + k, values[k], 12, true, null);
		ws.Row(rData).Height = 26;

		var table = ws.Range(rHead, CertIndex, rData, CertLastCol);
		table.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
		table.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

		// Declaration: heading, four numbered items, then the certification paragraph.
		var rDecl = rData + 2;
		Merge(ws, rDecl, CertIndex, rDecl, CertLastCol, OvertimeCertificateTexts.DeclarationTitle, 11, true, center, null);
		ws.Row(rDecl).Height = 16;

		var rItem = rDecl + 2;
		var items = OvertimeCertificateTexts.Declarations;
		for (var i = 0; i < items.Length; i++)
		{
			Put(ws, rItem + i, CertIndex, $"{i + 1}.", 10, true, null)
				.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
			Merge(ws, rItem + i, CertLast, rItem + i, CertLastCol, items[i], 10, true, left, null);
			ws.Cell(rItem + i, CertLast).Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
			ws.Row(rItem + i).Height = items[i].Length > 120 ? 28 : 16;   // long items wrap onto two lines
		}

		var rCert = rItem + items.Length;
		Merge(ws, rCert, CertIndex, rCert, CertLastCol, OvertimeCertificateTexts.Certification, 10, true, left, null);
		ws.Cell(rCert, CertIndex).Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
		ws.Row(rCert).Height = 42;

		// Signature: role, space to sign, name.
		var rSign = rCert + 2;
		Merge(ws, rSign, CertIndex, rSign, CertLastCol, OvertimeCertificateTexts.SignatureRole(m.IsFemale), 11, true, center, null);
		ws.Row(rSign + 1).Height = 22;
		ws.Row(rSign + 2).Height = 22;
		Merge(ws, rSign + 3, CertIndex, rSign + 3, CertLastCol, OvertimeCertificateTexts.SignatureName(m), 11, true, center, null);
		var lastRow = rSign + 3;

		// Widths / page: one A4 landscape page.
		ws.Column(CertIndex).Width = 7;
		ws.Column(CertLast).Width = 22;
		ws.Column(CertFirst).Width = 18;
		ws.Column(CertSpec).Width = 22;
		for (var c = CertValue0; c <= CertLastCol; c++) ws.Column(c).Width = 13;

		ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
		ws.PageSetup.PaperSize = XLPaperSize.A4Paper;
		ws.PageSetup.PagesWide = 1;
		ws.PageSetup.PagesTall = 1;
		ws.PageSetup.CenterHorizontally = true;
		ws.PageSetup.PrintAreas.Add(1, 1, lastRow, CertLastCol);
		ws.PageSetup.Margins.Left = 0.4; ws.PageSetup.Margins.Right = 0.4;
		ws.PageSetup.Margins.Top = 0.4; ws.PageSetup.Margins.Bottom = 0.4;

		using var ms = new MemoryStream();
		wb.SaveAs(ms);
		return ms.ToArray();
	}
}
