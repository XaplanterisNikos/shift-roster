using ClosedXML.Excel;
using Vardiologio.Application.ReportColors;
using Vardiologio.Application.Reports;

namespace Vardiologio.Infrastructure.Reports;

/// <summary>
/// Individual analytical sheet ("ΑΝΑΛΥΤΙΚΟΣ ΗΜΕΡΗΣΙΟΣ ΠΙΝΑΚΑΣ ΠΑΡΟΥΣΙΩΝ ΚΑΙ ΩΡΑΡΙΩΝ") of one
/// employee in the organisation's official form, on one A4 landscape page: emblem + letterhead
/// and the date top right, the title, the day table, the overtime row under it and the two
/// signatures. Layout and texts come from <see cref="IndividualSheetTexts"/> (shared with the
/// PDF); the cell values are the preview's (<see cref="IndividualSheetLayout"/>).
/// Reuses Put/Merge/AddEmblem of the Σ.Ω. part.
/// </summary>
public partial class ClosedXmlReportRenderer
{
	/// <inheritdoc/>
	public byte[] Render(IndividualReportModel m)
	{
		using var wb = new XLWorkbook();
		var ws = wb.AddWorksheet("Individual");
		var center = XLAlignmentHorizontalValues.Center;
		var left = XLAlignmentHorizontalValues.Left;
		var lastCol = IndividualSheetTexts.ColumnCount;

		// ---- Emblem (rows 1–3) and letterhead under it (columns of date … contract) ----
		for (var i = 1; i <= 3; i++) ws.Row(i).Height = 18;
		AddEmblem(ws, "Emblem", 1, 1, 30, 2, 62, 60);

		const int lh0 = 4;
		var letterhead = IndividualSheetTexts.Letterhead(m.Texts);
		for (var i = 0; i < letterhead.Length; i++)
		{
			Merge(ws, lh0 + i, 1, lh0 + i, 4, letterhead[i], 9, false, left, null);
			ws.Cell(lh0 + i, 1).Style.Alignment.WrapText = false;   // one line, may run over
			ws.Row(lh0 + i).Height = 14;
		}

		// Date label and date at the top right, level with the directorate line (as on the sheet).
		var dateRow = lh0 + IndividualSheetTexts.DateRow;
		var dateLabel = IndividualSheetTexts.DateLabel(m.Texts);
		if (dateLabel.Length > 0)
		{
			Merge(ws, dateRow, IndividualSheetTexts.DateLabelFrom, dateRow, IndividualSheetTexts.DateLabelTo,
				dateLabel, 12, false, XLAlignmentHorizontalValues.Right, null);
			Merge(ws, dateRow, IndividualSheetTexts.DateFrom, dateRow, IndividualSheetTexts.DateTo,
				IndividualSheetTexts.Date(m.IssueDate), 12, true, left, null);
		}

		// ---- Title, under the letterhead (at least four letterhead rows, so it never climbs) ----
		var row = lh0 + Math.Max(letterhead.Length, 4) + 1;
		foreach (var title in IndividualSheetTexts.Titles(m.Texts))
		{
			Merge(ws, row, 1, row, lastCol, title, 13, true, center, null);
			ws.Row(row).Height = 22;
			row++;
		}
		row++;

		// ---- Band over the schedule columns, then the two header rows ----
		var band = row;
		foreach (var (text, from, to) in IndividualSheetTexts.ScheduleBands)
			Merge(ws, band, from, band, to, text, 8, true, center, null);
		ws.Row(band).Height = 14;

		var h1 = band + 1;
		var h2 = band + 2;
		var col = 1;
		foreach (var header in IndividualSheetTexts.Headers)
		{
			if (header.Subs.Length == 0)
			{
				Merge(ws, h1, col, h2, col, header.Text, 8, true, center, null);
			}
			else
			{
				Merge(ws, h1, col, h1, col + header.Span - 1, header.Text, 8, true, center, null);
				for (var i = 0; i < header.Subs.Length; i++)
					Merge(ws, h2, col + i, h2, col + i, header.Subs[i], 7, true, center, null);
			}
			col += header.Span;
		}
		ws.Row(h1).Height = 20;
		ws.Row(h2).Height = 30;   // two-line sub-headers ("ΩΡΑΡΙΟ ΕΡΓΑΣΙΑΣ", "ΣΥΝ. ΩΡ. ΚΥΡΙΑΚΗΣ")

		// ---- One row per day ----
		row = h2 + 1;
		var sunday = XLColor.FromHtml(m.Colors[ReportColorCatalog.IndividualSunday]);   // Παράμετροι → Αναφορών → Χρώματα
		foreach (var day in m.Days)
		{
			var cells = IndividualSheetTexts.Cells(m, day);
			for (var c = 0; c < cells.Length; c++)
			{
				var cell = Put(ws, row, c + 1, cells[c], 9, false, day.IsSunday ? sunday : null);
				if (c == IndividualSheetTexts.NameColumn) cell.Style.Alignment.Horizontal = left;
			}
			ws.Row(row).Height = 14;
			row++;
		}
		var lastDay = row - 1;

		// ---- Overtime row: three rows, the same columns as the days ----
		var s1 = row;
		var s2 = row + 1;
		var s3 = row + 2;
		foreach (var group in IndividualSheetTexts.Summary(m))
		{
			if (group.Caption)
			{
				Merge(ws, s1, group.From, s3, group.To, group.Text, 7, true, center, null);   // narrow column
				continue;
			}

			// A single column: header two rows high, value under it. A group: header, sub-headers, values.
			var single = group.Columns.Length == 1 && group.Columns[0].Header.Length == 0;
			if (single)
				Merge(ws, s1, group.From, s2, group.To, group.Text, 8, true, center, null);
			else
				Merge(ws, s1, group.From, s1, group.To, group.Text, 8, true, center, null);

			foreach (var column in group.Columns)
			{
				if (!single)
					Merge(ws, s2, column.From, s2, column.To, column.Header, 7, true, center, null);
				Merge(ws, s3, column.From, s3, column.To, column.Value, 10, true, center, null);
			}
		}
		ws.Row(s1).Height = 32;
		ws.Row(s2).Height = 30;
		ws.Row(s3).Height = 18;

		// Thin lines inside, a thicker frame round the band, the day table and the overtime rows.
		foreach (var (_, from, to) in IndividualSheetTexts.ScheduleBands)
			ws.Range(band, from, band, to).Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
		var table = ws.Range(h1, 1, lastDay, lastCol);
		table.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
		table.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
		ws.Range(h1, 1, h2, lastCol).Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
		var summary = ws.Range(s1, 1, s3, lastCol);
		summary.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
		summary.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;

		// ---- Signatures: role, space to sign, name, title ----
		var roleRow = s3 + 2;
		var signatures = IndividualSheetTexts.Signatures(m);
		var roleLines = Math.Max(1, signatures.Max(s => s.Role.Length));
		var nameRow = roleRow + roleLines + 4;   // four empty rows to sign in
		for (var k = 0; k < signatures.Length && k < IndividualSheetTexts.SignatureColumns.Length; k++)
		{
			var (from, to) = IndividualSheetTexts.SignatureColumns[k];
			var signature = signatures[k];
			for (var i = 0; i < signature.Role.Length; i++)
				Merge(ws, roleRow + i, from, roleRow + i, to, signature.Role[i], 10, true, center, null);
			Merge(ws, nameRow, from, nameRow, to, signature.Name, 10, true, center, null);
			Merge(ws, nameRow + 1, from, nameRow + 1, to, signature.Title, 10, false, center, null);
		}
		var lastRow = nameRow + 1;

		// ---- Widths (the sheet's proportions) and page: A4 landscape, everything on one page ----
		for (var c = 1; c <= lastCol; c++)
			ws.Column(c).Width = Math.Max(6.5, Math.Round(IndividualSheetTexts.Widths[c - 1] * 0.62, 1));   // 6.5: "ΕΝΑΡΞΗ" fits

		ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
		ws.PageSetup.PaperSize = XLPaperSize.A4Paper;
		ws.PageSetup.PagesWide = 1;
		ws.PageSetup.PagesTall = 1;
		ws.PageSetup.CenterHorizontally = true;
		ws.PageSetup.PrintAreas.Add(1, 1, lastRow, lastCol);
		ws.PageSetup.Margins.Left = 0.3; ws.PageSetup.Margins.Right = 0.3;
		ws.PageSetup.Margins.Top = 0.3; ws.PageSetup.Margins.Bottom = 0.3;
		AddPageNumbers(ws);

		using var ms = new MemoryStream();
		wb.SaveAs(ms);
		return ms.ToArray();
	}
}
