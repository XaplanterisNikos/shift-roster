using ClosedXML.Excel;
using Vardiologio.Application.Reports;

namespace Vardiologio.Infrastructure.Reports;

/// <summary>
/// Official shift table ("ΠΙΝΑΚΑΣ ΒΑΡΔΙΩΝ ΕΡΓΑΣΙΑΣ ΠΡΟΣΩΠΙΚΟΥ") — one A3 landscape page, laid out
/// and coloured as the "Σ.Ω." sheet: letterhead + titles + addressee, the table (days shaded on
/// weekends / holidays, counts per work code, paid "προς συμπλήρωση" hours), the legend and the
/// signatures. Printed "1 page wide × 1 page tall", so Excel scales it to always fit on the A3.
/// </summary>
public partial class ClosedXmlReportRenderer
{
	// Fonts of the original sheet.
	private const string StTitleFont = "Segoe UI", StTextFont = "Candara", StCodeFont = "Calibri";

	/// <inheritdoc/>
	public byte[] Render(ShiftTableModel m)
	{
		using var wb = new XLWorkbook();
		var ws = wb.AddWorksheet("Shift table");
		var shade = XLColor.FromHtml(ShiftTableTexts.ShadeHex);
		var center = XLAlignmentHorizontalValues.Center;

		// Columns: narrow margin, name, speciality, one per day, one per work code, the two hour columns.
		const int cName = 2, cSpec = 3, cDay0 = 4;
		var days = m.DaysInMonth;
		var cCount0 = cDay0 + days;
		var codes = m.WorkCodes.Count;
		var cDaily = cCount0 + codes;
		var cSunHol = cDaily + 1;
		var lastCol = cSunHol;

		// ---- Header: emblem + letterhead (left), titles (centre), addressee (right) ----
		for (var r = 1; r <= 3; r++) ws.Row(r).Height = 16;
		using (var emblem = new MemoryStream(AttendanceSheetTexts.Emblem))   // ClosedXML copies the image
			ws.AddPicture(emblem, "Emblem").MoveTo(ws.Cell(1, cName), 30, 2).WithSize(62, 60);

		const int lh0 = 4;
		for (var i = 0; i < ShiftTableTexts.Letterhead.Length; i++)
		{
			var (text, bold) = ShiftTableTexts.Letterhead[i];
			ws.Range(lh0 + i, cName, lh0 + i, cSpec).Merge();
			var cell = ws.Cell(lh0 + i, cName);
			// Rich text so that only the bold part is bold (e.g. just the e-mail address).
			var plain = bold.Length == 0 ? text : text[..text.IndexOf(bold, StringComparison.Ordinal)];
			var rich = cell.GetRichText();
			if (plain.Length > 0) rich.AddText(plain).SetFontName(StTextFont).SetFontSize(11);
			if (bold.Length > 0) rich.AddText(bold).SetFontName(StTextFont).SetFontSize(11).SetBold();
			cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
			ws.Row(lh0 + i).Height = 18;   // same height for every letterhead row (titles share rows 4–6)
		}

		var tFrom = cDay0 + 6;
		var tTo = cDay0 + days - 8;
		var titles = ShiftTableTexts.Titles(m.Year, m.Month);
		int[] titleRows = { 2, 4, 5, 6 };
		for (var i = 0; i < titles.Length; i++)
			StMerge(ws, titleRows[i], tFrom, titleRows[i], tTo, titles[i].Text, StTitleFont, i == 0 ? 20 : 13, titles[i].Bold, center);
		ws.Row(titleRows[0]).Height = 28;   // first title is the largest; the others fit the 18pt letterhead rows
		for (var i = 0; i < ShiftTableTexts.Addressee.Length; i++)
			StMerge(ws, 3 + i, cCount0 - 6, 3 + i, lastCol, ShiftTableTexts.Addressee[i], StTitleFont, 13, true, center);

		// ---- Table header (two rows) ----
		var h1 = lh0 + ShiftTableTexts.Letterhead.Length + 1;
		var h2 = h1 + 1;
		StMerge(ws, h1, cName, h2, cName, ShiftTableTexts.HeadName, StTitleFont, 13, true, center);
		StMerge(ws, h1, cSpec, h2, cSpec, ShiftTableTexts.HeadSpeciality, StTextFont, 13, true, center);
		for (var d = 1; d <= days; d++)
		{
			StMerge(ws, h1, cDay0 + d - 1, h2, cDay0 + d - 1, ShiftTableTexts.DayHeader(m.Year, m.Month, d), StTitleFont, 11, true, center);
			if (m.IsShaded(d)) ws.Range(h1, cDay0 + d - 1, h2, cDay0 + d - 1).Style.Fill.BackgroundColor = shade;
		}
		if (codes > 0)
			StMerge(ws, h1, cCount0, h1, cDaily - 1, ShiftTableTexts.HeadShifts, StTextFont, 12, true, center);
		for (var k = 0; k < codes; k++)
			StMerge(ws, h2, cCount0 + k, h2, cCount0 + k, m.WorkCodes[k].Code, StCodeFont, 13, true, center);
		StMerge(ws, h1, cDaily, h1, cDaily, ShiftTableTexts.HeadToComplete, StTextFont, 9, true, center);
		StMerge(ws, h2, cDaily, h2, cDaily, ShiftTableTexts.HeadDaily, StTextFont, 10, true, center);
		StMerge(ws, h1, cSunHol, h2, cSunHol, ShiftTableTexts.HeadSundayHoliday, StTextFont, 10, true, center);
		ws.Row(h1).Height = 36;
		ws.Row(h2).Height = 28;

		var header = ws.Range(h1, cName, h2, lastCol);
		header.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
		header.Style.Border.InsideBorder = XLBorderStyleValues.Medium;

		// ---- One row per employee ----
		var r0 = h2 + 1;
		var row = r0;
		foreach (var e in m.Rows)
		{
			StCell(ws, row, cName, ShiftTableTexts.Name(e), StTextFont, 12, false, XLAlignmentHorizontalValues.Left);
			ws.Cell(row, cName).Style.Alignment.Indent = 1;
			StMerge(ws, row, cSpec, row, cSpec, GreekText.Upper(e.Speciality), StTextFont, 10, false, center);
			for (var d = 1; d <= days; d++)
			{
				var cell = StCell(ws, row, cDay0 + d - 1, e.DayCodes[d - 1] ?? "", StCodeFont, 14, true, center);
				if (m.IsShaded(d)) cell.Style.Fill.BackgroundColor = shade;
			}
			for (var k = 0; k < codes; k++)
				StCell(ws, row, cCount0 + k, ShiftTableTexts.Count(e.Counts[k]), StCodeFont, 13, true, center);
			StCell(ws, row, cDaily, ShiftTableTexts.Hours(e.DailyToComplete), StCodeFont, 13, true, center);
			StCell(ws, row, cSunHol, ShiftTableTexts.Hours(e.SundayHolidayToComplete), StCodeFont, 13, true, center);
			ws.Row(row).Height = 21;
			row++;
		}
		var lastData = Math.Max(row - 1, r0);

		// Borders: thin grid for names / days, medium boxes for the count and hour columns (as on the form).
		var body = ws.Range(r0, cName, lastData, lastCol);
		body.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
		body.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
		var totals = ws.Range(r0, cCount0, lastData, lastCol);
		totals.Style.Border.InsideBorder = XLBorderStyleValues.Medium;
		totals.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;

		// ---- Legend (name / speciality columns) ----
		var lt = lastData + 2;
		StMerge(ws, lt, cName, lt, cSpec, ShiftTableTexts.LegendTitle, StTextFont, 13, false, center);
		ws.Row(lt).Height = 22;
		var lr = lt + 1;
		foreach (var (code, description) in m.WorkCodes.Concat(m.StatusCodes))
		{
			StCell(ws, lr, cName, code, StCodeFont, 13, true, center);
			StCell(ws, lr, cSpec, description, StCodeFont, 11, false, center);
			ws.Row(lr).Height = 17;
			lr++;
		}
		var legend = ws.Range(lt, cName, Math.Max(lr - 1, lt), cSpec);
		legend.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
		legend.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

		// ---- Signatures, beside the legend: role, space to sign, name / title ----
		var roleRow = lt + 4;
		var nameRow = Math.Max(roleRow + 8, lr - 4);
		(int From, int To)[] blocks =
		{
			(cDay0 + 5, cDay0 + 13),                                   // Ο συντάξας
			(cDay0 + 18, Math.Max(cDay0 + 26, cCount0 + codes / 2))    // Ο Αν. Προϊστάμενος
		};
		for (var s = 0; s < ShiftTableTexts.Signatures.Length && s < blocks.Length; s++)
		{
			var (role, name) = ShiftTableTexts.Signatures[s];
			var (from, to) = blocks[s];
			StMerge(ws, roleRow, from, roleRow, to, role, StTextFont, 14, true, center);
			for (var i = 0; i < name.Length; i++)
				StMerge(ws, nameRow + i, from, nameRow + i, to, name[i], StTextFont, 14, true, center);
		}
		var lastRow = Math.Max(lr - 1, nameRow + 1);

		// ---- Widths and page: A3 landscape, everything on one page ----
		ws.Column(1).Width = 1.5;
		ws.Column(cName).Width = 34;
		ws.Column(cSpec).Width = 28;
		for (var c = cDay0; c < cCount0; c++) ws.Column(c).Width = 6.5;
		for (var c = cCount0; c < cDaily; c++) ws.Column(c).Width = 5.5;
		ws.Column(cDaily).Width = 12;
		ws.Column(cSunHol).Width = 13;

		ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
		ws.PageSetup.PaperSize = XLPaperSize.A3Paper;
		ws.PageSetup.PagesWide = 1;
		ws.PageSetup.PagesTall = 1;
		ws.PageSetup.CenterHorizontally = true;
		ws.PageSetup.PrintAreas.Add(1, 1, lastRow, lastCol);
		ws.PageSetup.Margins.Left = 0.25; ws.PageSetup.Margins.Right = 0.25;
		ws.PageSetup.Margins.Top = 0.4; ws.PageSetup.Margins.Bottom = 0.4;

		using var ms = new MemoryStream();
		wb.SaveAs(ms);
		return ms.ToArray();
	}

	/// <summary>Writes one cell with the form's font (the shared Put helper always uses Arial).</summary>
	private static IXLCell StCell(IXLWorksheet ws, int r, int c, string text,
		string font, double size, bool bold, XLAlignmentHorizontalValues h)
	{
		var cell = ws.Cell(r, c);
		cell.Value = text;
		cell.Style.Font.FontName = font;
		cell.Style.Font.FontSize = size;
		cell.Style.Font.Bold = bold;
		cell.Style.Alignment.Horizontal = h;
		cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
		return cell;
	}

	/// <summary>Merges a range (a single cell is left as is) and writes wrapped text with the form's font.</summary>
	private static void StMerge(IXLWorksheet ws, int r1, int c1, int r2, int c2, string text,
		string font, double size, bool bold, XLAlignmentHorizontalValues h)
	{
		ws.Range(r1, c1, r2, c2).Merge();
		StCell(ws, r1, c1, text, font, size, bold, h).Style.Alignment.WrapText = true;
	}
}
