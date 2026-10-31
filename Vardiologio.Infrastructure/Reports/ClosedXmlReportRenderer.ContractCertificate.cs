using ClosedXML.Excel;
using Vardiologio.Application.ReportColors;
using Vardiologio.Application.Reports;

namespace Vardiologio.Infrastructure.Reports;

/// <summary>
/// Collective overtime certificate per contract ("ΙΔΟΧ / ΙΔΑΧ / Μόνιμοι") — A4 landscape, as many
/// pages as the employees need. Same top and same columns as the individual certificate
/// (reuses its column constants and <see cref="CertificateHead"/>); the table has one row per
/// employee under a coloured band per contract — permanent, open-ended, fixed-term, one under the
/// other — and the table header is repeated at the top of every printed page.
/// </summary>
public partial class ClosedXmlReportRenderer
{
	/// <inheritdoc/>
	public byte[] Render(ContractCertificateModel m)
	{
		using var wb = new XLWorkbook();
		var ws = wb.AddWorksheet(ContractCertificateTexts.SheetName);
		var center = XLAlignmentHorizontalValues.Center;
		var bandFill = XLColor.FromHtml(m.Colors[ReportColorCatalog.ContractCertificateBand]);

		var rHead = CertificateHead(ws, m.Texts, m.Year, m.Month, m.From, m.To);

		Merge(ws, rHead, CertIndex, rHead + 1, CertIndex, OvertimeCertificateTexts.HeadIndex, 9, true, center, null);
		Merge(ws, rHead, CertLast, rHead + 1, CertLast, OvertimeCertificateTexts.HeadLastName, 10, true, center, null);
		Merge(ws, rHead, CertFirst, rHead + 1, CertFirst, OvertimeCertificateTexts.HeadFirstName, 10, true, center, null);
		Merge(ws, rHead, CertSpec, rHead + 1, CertSpec, ContractCertificateTexts.HeadSpeciality, 10, true, center, null);
		for (var g = 0; g < OvertimeCertificateTexts.Groups.Length; g++)
		{
			var c = CertValue0 + g * 2;
			Merge(ws, rHead, c, rHead, c + 1, OvertimeCertificateTexts.Groups[g], 8, true, center, null);
		}
		ws.Cell(rHead, CertValue0).Style.Font.Underline = XLFontUnderlineValues.Single;
		for (var k = 0; k < OvertimeCertificateTexts.SubHeads.Length; k++)
			Merge(ws, rHead + 1, CertValue0 + k, rHead + 1, CertValue0 + k, OvertimeCertificateTexts.SubHeads[k], 7, true, center, null);
		ws.Row(rHead).Height = 30;
		ws.Row(rHead + 1).Height = 30;

		var r = rHead + 2;
		var bandRows = new HashSet<int>();

		var department = ContractCertificateTexts.Department(m.Texts);
		if (department.Length > 0)
		{
			bandRows.Add(r);
			r = ContractBand(ws, r, department, bandFill);
		}

		foreach (var group in m.Groups)
		{
			var title = ContractCertificateTexts.GroupTitle(m.Texts, group.Kind);
			if (title.Length > 0)
			{
				bandRows.Add(r);
				r = ContractBand(ws, r, title, bandFill);
			}

			foreach (var row in group.Rows)
			{
				var speciality = GreekText.Upper(row.Speciality);

				Put(ws, r, CertIndex, row.Index, 8, false, null);
				Put(ws, r, CertLast, GreekText.Upper(row.LastName), 10, true, null);
				Put(ws, r, CertFirst, GreekText.Upper(row.FirstName), 10, true, null);
				Merge(ws, r, CertSpec, r, CertSpec, speciality, 8, true, center, null);

				var values = ContractCertificateTexts.Values(row);
				for (var k = 0; k < values.Length; k++)
					Put(ws, r, CertValue0 + k, values[k], 11, true, null);

				ws.Row(r).Height = speciality.Length > 44 ? 34 : speciality.Length > 22 ? 25 : 18;
				r++;
			}
		}
		var lastTableRow = r - 1;

		var table = ws.Range(rHead, CertIndex, lastTableRow, CertLastCol);
		table.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
		table.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

		ws.Row(r).Height = 10;
		var lastRow = ContractSignatures(ws, r + 1, m);

		ws.Column(CertIndex).Width = 7;
		ws.Column(CertLast).Width = 22;
		ws.Column(CertFirst).Width = 18;
		ws.Column(CertSpec).Width = 22;
		for (var c = CertValue0; c <= CertLastCol; c++) ws.Column(c).Width = 13;

		ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
		ws.PageSetup.PaperSize = XLPaperSize.A4Paper;
		ws.PageSetup.PagesWide = 1;
		ws.PageSetup.SetRowsToRepeatAtTop(rHead, rHead + 1);
		ws.PageSetup.CenterHorizontally = true;
		ws.PageSetup.PrintAreas.Add(1, 1, lastRow, CertLastCol);
		ws.PageSetup.Margins.Left = 0.4; ws.PageSetup.Margins.Right = 0.4;
		ws.PageSetup.Margins.Top = 0.4; ws.PageSetup.Margins.Bottom = 0.4;
		AddPageNumbers(ws);

		if (m.Groups.Count > 0)
			KeepLastRowsWithClosing(ws, CertLastCol, TailFirstRow(lastTableRow, bandRows), lastTableRow, lastRow);

		using var ms = new MemoryStream();
		wb.SaveAs(ms);
		return ms.ToArray();
	}

	/// <summary>
	/// Writes one coloured band across the whole table ("ΤΜΗΜΑ: …" or a contract title) and
	/// returns the next free row.
	/// </summary>
	private static int ContractBand(IXLWorksheet ws, int row, string text, XLColor fill)
	{
		Merge(ws, row, CertIndex, row, CertLastCol, text, 9, true, XLAlignmentHorizontalValues.Center, fill);
		ws.Row(row).Height = 15;
		return row + 1;
	}

	/// <summary>
	/// Writes the signature blocks starting at <paramref name="top"/> — role lines, blank rows to
	/// sign, then name / title lines — and returns the last row used. With two blocks the first
	/// sits on the left and the second on the right; a single block sits on the right.
	/// </summary>
	private static int ContractSignatures(IXLWorksheet ws, int top, ContractCertificateModel m)
	{
		const int gapRows = 4;
		var center = XLAlignmentHorizontalValues.Center;

		var signatures = m.Texts.Signatures();
		var roleRows = signatures.Select(s => s.Role.Length).DefaultIfEmpty(0).Max();
		var nameRows = signatures.Select(s => s.Name.Length).DefaultIfEmpty(0).Max();

		for (var k = 0; k < signatures.Length; k++)
		{
			var (c1, c2) = signatures.Length > 1 && k == 0
				? (CertLast, CertFirst)
				: (CertLastCol - 3, CertLastCol);

			for (var i = 0; i < signatures[k].Role.Length; i++)
				Merge(ws, top + i, c1, top + i, c2, signatures[k].Role[i], 10, true, center, null);
			for (var i = 0; i < signatures[k].Name.Length; i++)
				Merge(ws, top + roleRows + gapRows + i, c1, top + roleRows + gapRows + i, c2,
					signatures[k].Name[i], 10, true, center, null);
		}

		var rows = roleRows + gapRows + nameRows;
		for (var i = 0; i < rows; i++) ws.Row(top + i).Height = 14;
		return top + Math.Max(rows, 1) - 1;
	}
}
