using ClosedXML.Excel;
using Vardiologio.Application.ReportColors;
using Vardiologio.Application.Reports;
using Vardiologio.Application.ReportTexts;

namespace Vardiologio.Infrastructure.Reports;

/// <summary>
/// Renders the Σ.Ω. monthly roster to an .xlsx file using ClosedXML: emblem + letterhead, titles
/// and addressee above the table, the legend of shift codes and the signatures below it. The
/// texts are the user's (Παράμετροι → Αναφορών) and are formatted like those of the shift table.
/// Partial: the hours report lives in ClosedXmlReportRenderer.Hours.cs and reuses the helpers here.
/// </summary>
public partial class ClosedXmlReportRenderer : IExcelReportRenderer
{
	private static readonly string[] MonthsGen =
		{ "ΙΑΝΟΥΑΡΙΟΥ","ΦΕΒΡΟΥΑΡΙΟΥ","ΜΑΡΤΙΟΥ","ΑΠΡΙΛΙΟΥ","ΜΑΪΟΥ","ΙΟΥΝΙΟΥ",
		  "ΙΟΥΛΙΟΥ","ΑΥΓΟΥΣΤΟΥ","ΣΕΠΤΕΜΒΡΙΟΥ","ΟΚΤΩΒΡΙΟΥ","ΝΟΕΜΒΡΙΟΥ","ΔΕΚΕΜΒΡΙΟΥ" };
	/// <summary>Weekday abbreviations, Monday first.</summary>
	private static readonly string[] WdGr = { "Δε", "Τρ", "Τε", "Πε", "Πα", "Σα", "Κυ" };

	/// <summary>Height of a Σ.Ω. table / legend row, in points (Arial 8–9 with a little room).</summary>
	private const double SoRowHeight = 12.75;

	/// <inheritdoc/>
	public byte[] Render(ReportModel m)
	{
		var hdrFill = XLColor.FromHtml(m.Colors[ReportColorCatalog.MonthlyTotalHeader]);
		var sunFill = XLColor.FromHtml(m.Colors[ReportColorCatalog.MonthlyTotalSunday]);
		var satFill = XLColor.FromHtml(m.Colors[ReportColorCatalog.MonthlyTotalSaturday]);
		var totFill = XLColor.FromHtml(m.Colors[ReportColorCatalog.MonthlyTotalTotals]);
		var bandFill = XLColor.FromHtml(m.Colors[ReportColorCatalog.MonthlyTotalFixedTermBand]);

		using var wb = new XLWorkbook();
		var ws = wb.AddWorksheet("Σ.Ω.");

		var days = m.DaysInMonth;
		const int cName = 1, cSpec = 2, cDay0 = 3;
		var cDayEnd = cDay0 + days - 1;
		var cCount0 = cDayEnd + 1;
		var codeCount = Math.Max(m.WorkCodes.Count, 1);
		var cCountEnd = cCount0 + codeCount - 1;
		var cHmer = cCountEnd + 1;
		var cKyr = cHmer + 1;
		var lastCol = cKyr;

		var center = XLAlignmentHorizontalValues.Center;

		for (var i = 1; i <= 3; i++) ws.Row(i).Height = 16;
		AddEmblem(ws, "Emblem", 1, cName, 30, 2, 62, 60);

		const int lh0 = 4;
		var letterhead = ShiftTableTexts.Letterhead(m.Texts);
		for (var i = 0; i < letterhead.Length; i++)
		{
			var (plain, bold) = letterhead[i];
			ws.Range(lh0 + i, cName, lh0 + i, cDay0 + 5).Merge();
			var cell = ws.Cell(lh0 + i, cName);
			var rich = cell.GetRichText();
			if (plain.Length > 0) rich.AddText(plain).SetFontName("Arial").SetFontSize(9);
			if (bold.Length > 0) rich.AddText(bold).SetFontName("Arial").SetFontSize(9).SetBold();
			cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
			ws.Row(lh0 + i).Height = 14;
		}

		var titles = ShiftTableTexts.Titles(m.Texts, m.Year, m.Month);
		int[] titleRows = { 2, 4, 5, 6 };
		for (var i = 0; i < titles.Length && i < titleRows.Length; i++)
			Merge(ws, titleRows[i], cDay0 + 8, titleRows[i], cDay0 + days - 5, titles[i].Text, titles[i].Large ? 14 : 11, titles[i].Bold, center, null);
		ws.Row(titleRows[0]).Height = 22;

		var addressee = ShiftTableTexts.Addressee(m.Texts);
		for (var i = 0; i < addressee.Length; i++)
			Merge(ws, 3 + i, cCount0 - 2, 3 + i, lastCol, addressee[i], 10, true, center, null);

		var rGroup = lh0 + Math.Max(letterhead.Length, 3) + 1;
		var rDay = rGroup + 1;
		var rWd = rGroup + 2;
		var rData0 = rGroup + 3;

		Merge(ws, rGroup, cName, rWd, cName, "ΟΝΟΜΑΤΕΠΩΝΥΜΟ", 9, true, XLAlignmentHorizontalValues.Center, hdrFill);
		Merge(ws, rGroup, cSpec, rWd, cSpec, "ΕΙΔΙΚΟΤΗΤΑ", 8, true, XLAlignmentHorizontalValues.Center, hdrFill);

		Merge(ws, rGroup, cCount0, rGroup, cCountEnd, "ΒΑΡΔΙΑ ΕΡΓΑΣΙΑΣ", 9, true, XLAlignmentHorizontalValues.Center, hdrFill);
		Merge(ws, rGroup, cHmer, rGroup, cKyr, "ΕΡΓΑΣΙΑ ΠΡΟΣ ΣΥΜΠΛΗΡΩΣΗ", 8, true, XLAlignmentHorizontalValues.Center, totFill);
		Merge(ws, rDay, cHmer, rWd, cHmer, "ΗΜΕΡΗΣΙΑ", 8, true, XLAlignmentHorizontalValues.Center, totFill);
		Merge(ws, rDay, cKyr, rWd, cKyr, "ΚΥΡΙΑΚΩΝ ΕΞΑΙΡΕΣΙΜΩΝ", 7, true, XLAlignmentHorizontalValues.Center, totFill);
		
		for (var d = 1; d <= days; d++)
		{
			var date = new DateOnly(m.Year, m.Month, d);
			var col = cDay0 + d - 1;
			var wi = ((int)date.DayOfWeek + 6) % 7;
			var fill = date.DayOfWeek == DayOfWeek.Sunday ? sunFill
					 : date.DayOfWeek == DayOfWeek.Saturday ? satFill : hdrFill;

			Put(ws, rDay, col, d, 8, true, fill);
			Put(ws, rWd, col, WdGr[wi], 7, false, fill);
		}

		for (var k = 0; k < codeCount; k++)
		{
			var code = k < m.WorkCodes.Count ? m.WorkCodes[k].Code : "";
			Merge(ws, rDay, cCount0 + k, rWd, cCount0 + k, code, 8, true, XLAlignmentHorizontalValues.Center, hdrFill);
		}

		ws.Row(rGroup).Height = 22;
		ws.Row(rDay).Height = SoRowHeight;
		ws.Row(rWd).Height = SoRowHeight;

		var bandRows = new HashSet<int>();
		var group = ContractKind.Permanent;
		var r = rData0;
		foreach (var row in m.Rows)
		{
			if (row.Group != group)
			{
				group = row.Group;
				Merge(ws, r, cName, r, lastCol, ShiftTableTexts.GroupBand(m.Texts, group), 9, true, center, bandFill);
				ws.Row(r).Height = 15;
				bandRows.Add(r);
				r++;
			}

			var nameCell = Put(ws, r, cName, $"{row.LastName} {row.FirstName}".Trim(), 8, false, null);
			nameCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
			var specCell = Put(ws, r, cSpec, row.Speciality, 8, false, null);
			specCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

			for (var d = 1; d <= days; d++)
			{
				var date = new DateOnly(m.Year, m.Month, d);
				var fill = date.DayOfWeek == DayOfWeek.Sunday ? sunFill
						 : date.DayOfWeek == DayOfWeek.Saturday ? satFill : (XLColor?)null;
				Put(ws, r, cDay0 + d - 1, row.DayCodes[d - 1] ?? "", 8, false, fill);
			}

			for (var k = 0; k < codeCount; k++)
			{
				var count = k < row.ProgramCounts.Length ? row.ProgramCounts[k] : 0;
				Put(ws, r, cCount0 + k, count == 0 ? "" : count, 8, false, null);
			}

			Put(ws, r, cHmer, ShiftTableTexts.Hours(row.DailyToComplete), 8, false, null);
			Put(ws, r, cKyr, ShiftTableTexts.Hours(row.SundayHolidayToComplete), 8, false, null);
			ws.Row(r).Height = SoRowHeight;
			r++;
		}
		var lastRow = Math.Max(r - 1, rData0);

		var table = ws.Range(rGroup, 1, lastRow, lastCol);
		table.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
		table.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

		var lt = lastRow + 2;
		Merge(ws, lt, cName, lt, cSpec, ShiftTableTexts.LegendTitle, 9, true, center, hdrFill);
		ws.Row(lt).Height = SoRowHeight;
		var lr = lt + 1;
		foreach (var (code, description) in m.WorkCodes.Concat(m.StatusCodes))
		{
			Put(ws, lr, cName, code, 9, true, null);
			Put(ws, lr, cSpec, description, 8, false, null);
			ws.Row(lr).Height = SoRowHeight;
			lr++;
		}
		var legend = ws.Range(lt, cName, Math.Max(lr - 1, lt), cSpec);
		legend.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
		legend.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

		var roleRow = lt + 2;
		var nameRow = roleRow + 6;
		(int From, int To)[] signatureColumns =
		{
			(cDay0 + 3, cDay0 + 12),
			(cDay0 + 17, Math.Min(cDay0 + 26, lastCol))
		};
		var signatures = ShiftTableTexts.Signatures(m.Texts);
		for (var k = 0; k < signatures.Length && k < signatureColumns.Length; k++)
		{
			var (role, name) = signatures[k];
			var (from, to) = signatureColumns[k];
			Merge(ws, roleRow, from, roleRow, to, role, 10, true, center, null);
			for (var i = 0; i < name.Length; i++)
				Merge(ws, nameRow + i, from, nameRow + i, to, name[i], 10, true, center, null);
		}

		ws.Column(cName).Width = 22;
		ws.Column(cSpec).Width = 16;
		for (var c = cDay0; c <= cCountEnd; c++) ws.Column(c).Width = 3.4;
		ws.Column(cHmer).Width = 8;
		ws.Column(cKyr).Width = 9;

		ws.SheetView.FreezeRows(rWd);
		ws.SheetView.FreezeColumns(cSpec);

		ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
		var printLastRow = Math.Max(Math.Max(lr - 1, lt), nameRow + 2);
		ws.PageSetup.PaperSize = XLPaperSize.A4Paper;
		ws.PageSetup.PagesWide = 1;
		ws.PageSetup.SetRowsToRepeatAtTop(rGroup, rWd);
		ws.PageSetup.CenterHorizontally = true;
		ws.PageSetup.PrintAreas.Add(1, 1, printLastRow, lastCol);
		ws.PageSetup.Margins.Left = 0.3; ws.PageSetup.Margins.Right = 0.3;
		ws.PageSetup.Margins.Top = 0.4; ws.PageSetup.Margins.Bottom = 0.4;
		AddPageNumbers(ws);

		if (m.Rows.Count > 0)
			KeepLastRowsWithClosing(ws, lastCol, TailFirstRow(lastRow, bandRows), lastRow, printLastRow);

		using var ms = new MemoryStream();
		wb.SaveAs(ms);
		return ms.ToArray();
	}

	/// <summary>
	/// Puts the emblem on the sheet, its top-left corner at an offset inside a cell.
	/// Does nothing when there is no emblem file (see <see cref="AttendanceSheetTexts.Emblem"/>).
	/// </summary>
	/// <param name="ws">The worksheet.</param>
	/// <param name="name">Picture name, unique in the worksheet.</param>
	/// <param name="row">Row of the anchor cell.</param>
	/// <param name="column">Column of the anchor cell.</param>
	/// <param name="offsetX">Horizontal offset inside the anchor cell, in pixels.</param>
	/// <param name="offsetY">Vertical offset inside the anchor cell, in pixels.</param>
	/// <param name="width">Picture width in pixels.</param>
	/// <param name="height">Picture height in pixels.</param>
	private static void AddEmblem(IXLWorksheet ws, string name, int row, int column,
		int offsetX, int offsetY, int width, int height)
	{
		var bytes = AttendanceSheetTexts.Emblem;
		if (bytes is null) return;

		using var emblem = new MemoryStream(bytes);
		ws.AddPicture(emblem, name).MoveTo(ws.Cell(row, column), offsetX, offsetY).WithSize(width, height);
	}

	/// <summary>Writes a merged, styled cell (used for headers/titles).</summary>
	private static void Merge(IXLWorksheet ws, int r1, int c1, int r2, int c2,
		string text, double size, bool bold, XLAlignmentHorizontalValues h, XLColor? fill)
	{
		var rng = ws.Range(r1, c1, r2, c2);
		rng.Merge();
		var cell = ws.Cell(r1, c1);
		cell.Value = text;
		cell.Style.Font.FontName = "Arial";
		cell.Style.Font.FontSize = size;
		cell.Style.Font.Bold = bold;
		cell.Style.Alignment.Horizontal = h;
		cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
		cell.Style.Alignment.WrapText = true;
		if (fill is not null) rng.Style.Fill.BackgroundColor = fill;
	}

	/// <summary>Writes a single styled cell and returns it for further tweaks.</summary>
	private static IXLCell Put(IXLWorksheet ws, int r, int c, XLCellValue v,
		double size, bool bold, XLColor? fill)
	{
		var cell = ws.Cell(r, c);
		cell.Value = v;
		cell.Style.Font.FontName = "Arial";
		cell.Style.Font.FontSize = size;
		cell.Style.Font.Bold = bold;
		cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
		cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
		if (fill is not null) cell.Style.Fill.BackgroundColor = fill;
		return cell;
	}
}