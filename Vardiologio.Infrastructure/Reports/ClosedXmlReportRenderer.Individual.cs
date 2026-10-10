using ClosedXML.Excel;
using Vardiologio.Application.ReportColors;
using Vardiologio.Application.Reports;

namespace Vardiologio.Infrastructure.Reports;

/// <summary>
/// Individual analytical sheet ("ΑΝΑΛΥΤΙΚΟΣ ΗΜΕΡΗΣΙΟΣ ΠΙΝΑΚΑΣ ΠΑΡΟΥΣΙΩΝ ΚΑΙ ΩΡΑΡΙΩΝ") of one
/// employee — one A4 landscape page: title, the day table (one row per day of the month) and
/// the summary blocks under it. Columns and values come from <see cref="IndividualSheetLayout"/>,
/// the same source as the preview and the PDF. Reuses Put/Merge of the Σ.Ω. part.
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
		var lastCol = IndividualSheetLayout.ColumnCount;

		// ---- Title and employee / month line ----
		Merge(ws, 1, 1, 1, lastCol, IndividualSheetLayout.Title, 12, true, center, null);
		Merge(ws, 2, 1, 2, lastCol, IndividualSheetLayout.Subtitle(m), 11, true, center, null);
		ws.Row(1).Height = 20;
		ws.Row(2).Height = 18;

		// ---- Table header: two rows; a header without sub-columns spans both ----
		const int h1 = 4, h2 = 5;
		var col = 1;
		foreach (var header in IndividualSheetLayout.Headers)
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
		ws.Row(h1).Height = 24;
		ws.Row(h2).Height = 14;

		// ---- One row per day ----
		var row = h2 + 1;
		var sunday = XLColor.FromHtml(m.Colors[ReportColorCatalog.IndividualSunday]);   // Παράμετροι → Αναφορών → Χρώματα
		foreach (var day in m.Days)
		{
			var cells = IndividualSheetLayout.Cells(m, day);
			for (var c = 0; c < cells.Length; c++)
			{
				var cell = Put(ws, row, c + 1, cells[c], 8, false, day.IsSunday ? sunday : null);
				if (c == IndividualSheetLayout.NameColumn) cell.Style.Alignment.Horizontal = left;
			}
			ws.Row(row).Height = 13.5;
			row++;
		}
		var lastData = row - 1;

		var table = ws.Range(h1, 1, lastData, lastCol);
		table.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
		table.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
		ws.Range(h1, 1, h2, lastCol).Style.Border.OutsideBorder = XLBorderStyleValues.Medium;

		// ---- Summary blocks, side by side under the table ----
		// Each block: title, a note saying where the numbers come from, headers, values.
		// They start at the first of the equally wide columns (after name / contract), one
		// sheet column per value, so every value cell has the same width.
		const int summaryFirstCol = 5;
		var top = lastData + 2;
		col = summaryFirstCol;
		foreach (var block in IndividualSheetLayout.Summary(m))
		{
			var blockLast = col + block.Cells.Length - 1;

			Merge(ws, top, col, top, blockLast, block.Title, 8, true, center, null);
			Merge(ws, top + 1, col, top + 1, blockLast, block.Note, 7, false, center, null);
			ws.Cell(top + 1, col).Style.Font.Italic = true;

			for (var i = 0; i < block.Cells.Length; i++)
			{
				Merge(ws, top + 2, col + i, top + 2, col + i, block.Cells[i].Header, 7, true, center, null);
				Merge(ws, top + 3, col + i, top + 3, col + i, block.Cells[i].Value, 10, true, center, null);
			}

			var box = ws.Range(top, col, top + 3, blockLast);
			box.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
			box.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;

			col = blockLast + 1;
		}
		ws.Row(top).Height = 26;       // long titles wrap onto two lines
		ws.Row(top + 1).Height = 38;   // the notes wrap onto three or four lines
		ws.Row(top + 2).Height = 62;   // long headers wrap inside one column
		ws.Row(top + 3).Height = 18;
		var lastRow = top + 3;

		// ---- Widths and page: A4 landscape, everything on one page ----
		ws.Column(1).Width = 8;      // date
		ws.Column(2).Width = 5;      // Α/Α
		ws.Column(3).Width = 26;     // name
		ws.Column(4).Width = 10;     // contract
		for (var c = 5; c <= lastCol; c++) ws.Column(c).Width = 8.5;

		ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
		ws.PageSetup.PaperSize = XLPaperSize.A4Paper;
		ws.PageSetup.PagesWide = 1;
		ws.PageSetup.PagesTall = 1;
		ws.PageSetup.CenterHorizontally = true;
		ws.PageSetup.PrintAreas.Add(1, 1, lastRow, lastCol);
		ws.PageSetup.Margins.Left = 0.3; ws.PageSetup.Margins.Right = 0.3;
		ws.PageSetup.Margins.Top = 0.3; ws.PageSetup.Margins.Bottom = 0.3;

		using var ms = new MemoryStream();
		wb.SaveAs(ms);
		return ms.ToArray();
	}
}
