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
	private static readonly string[] WdGr = { "Δε", "Τρ", "Τε", "Πε", "Πα", "Σα", "Κυ" }; // Δευτέρα-based

	/// <summary>Height of a Σ.Ω. table / legend row, in points (Arial 8–9 with a little room).</summary>
	private const double SoRowHeight = 12.75;

	/// <inheritdoc/>
	public byte[] Render(ReportModel m)
	{
		// Colours from Παράμετροι → Αναφορών → Χρώματα.
		var hdrFill = XLColor.FromHtml(m.Colors[ReportColorCatalog.MonthlyTotalHeader]);
		var sunFill = XLColor.FromHtml(m.Colors[ReportColorCatalog.MonthlyTotalSunday]);
		var satFill = XLColor.FromHtml(m.Colors[ReportColorCatalog.MonthlyTotalSaturday]);
		var totFill = XLColor.FromHtml(m.Colors[ReportColorCatalog.MonthlyTotalTotals]);
		var bandFill = XLColor.FromHtml(m.Colors[ReportColorCatalog.MonthlyTotalFixedTermBand]);

		using var wb = new XLWorkbook();
		var ws = wb.AddWorksheet("Σ.Ω.");

		var days = m.DaysInMonth;
		// No Α/Α column on the printed Σ.Ω. (as on the paper form): the name is the first column.
		const int cName = 1, cSpec = 2, cDay0 = 3;
		var cDayEnd = cDay0 + days - 1;
		var cCount0 = cDayEnd + 1;
		var cCountEnd = cCount0 + 6;         
		var cHmer = cCountEnd + 1;
		var cKyr = cHmer + 1;
		var lastCol = cKyr;

		var center = XLAlignmentHorizontalValues.Center;

		// ---- Header: emblem + letterhead (left), titles (centre), addressee (right) ----
		// Same arrangement and the same position-based formatting as the official shift table.
		for (var i = 1; i <= 3; i++) ws.Row(i).Height = 16;
		AddEmblem(ws, "Emblem", 1, cName, 30, 2, 62, 60);

		const int lh0 = 4;
		var letterhead = ShiftTableTexts.Letterhead(m.Texts);
		for (var i = 0; i < letterhead.Length; i++)
		{
			var (plain, bold) = letterhead[i];
			// The letterhead is wider than name + speciality here, so it runs over the first day columns.
			ws.Range(lh0 + i, cName, lh0 + i, cDay0 + 5).Merge();
			var cell = ws.Cell(lh0 + i, cName);
			// Rich text so that only the bold part is bold (e.g. just the e-mail address).
			var rich = cell.GetRichText();
			if (plain.Length > 0) rich.AddText(plain).SetFontName("Arial").SetFontSize(9);
			if (bold.Length > 0) rich.AddText(bold).SetFontName("Arial").SetFontSize(9).SetBold();
			cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
			ws.Row(lh0 + i).Height = 14;
		}

		// Titles in the middle of the day columns; the first is the largest.
		var titles = ShiftTableTexts.Titles(m.Texts, m.Year, m.Month);
		int[] titleRows = { 2, 4, 5, 6 };
		for (var i = 0; i < titles.Length && i < titleRows.Length; i++)
			Merge(ws, titleRows[i], cDay0 + 8, titleRows[i], cDay0 + days - 5, titles[i].Text, titles[i].Large ? 14 : 11, titles[i].Bold, center, null);
		ws.Row(titleRows[0]).Height = 22;

		var addressee = ShiftTableTexts.Addressee(m.Texts);
		for (var i = 0; i < addressee.Length; i++)
			Merge(ws, 3 + i, cCount0 - 2, 3 + i, lastCol, addressee[i], 10, true, center, null);

		// Table column headers: under the letterhead; at least three rows down, so a short
		// letterhead cannot pull the table up onto the title rows.
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
		
		// Day columns: number + weekday letter + weekend shading
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

		// Program counts 1..7
		for (var k = 1; k <= 7; k++)
			Merge(ws, rDay, cCount0 + k - 1, rWd, cCount0 + k - 1, k.ToString(), 8, true, XLAlignmentHorizontalValues.Center, hdrFill);

		// Fixed row heights from the table down: Excel then prints exactly these heights, and the
		// page-break estimate under the table (KeepLastRowsWithClosing) can rely on them.
		ws.Row(rGroup).Height = 22;   // "ΕΡΓΑΣΙΑ ΠΡΟΣ ΣΥΜΠΛΗΡΩΣΗ" wraps onto two lines
		ws.Row(rDay).Height = SoRowHeight;
		ws.Row(rWd).Height = SoRowHeight;

		// Data rows: the permanent / open-ended list, then the separator row and the fixed-term list.
		var bandText = m.Texts.Line(ReportTextBlocks.FixedTermBand, 0);
		var bandDone = false;
		var bandRows = new HashSet<int>();
		var r = rData0;
		foreach (var row in m.Rows)
		{
			if (row.IsFixedTerm && !bandDone)
			{
				// One full-width row before the first fixed-term employee (only when there is one).
				Merge(ws, r, cName, r, lastCol, bandText, 9, true, center, bandFill);
				ws.Row(r).Height = 15;
				bandRows.Add(r);
				bandDone = true;
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

			for (var k = 0; k < 7; k++)
				Put(ws, r, cCount0 + k, row.ProgramCounts[k] == 0 ? "" : row.ProgramCounts[k], 8, false, null);

			Put(ws, r, cHmer, "", 8, false, null);   // ΗΜΕΡΗΣΙΑ total — left blank until the rule is defined
			Put(ws, r, cKyr, "", 8, false, null);   // ΚΥΡΙΑΚΩΝ total — left blank until the rule is defined
			ws.Row(r).Height = SoRowHeight;
			r++;
		}
		var lastRow = Math.Max(r - 1, rData0);

		// Borders / column widths / page setup
		var table = ws.Range(rGroup, 1, lastRow, lastCol);
		table.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
		table.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

		// ---- Legend of the shift codes (name / speciality columns), as on the shift table ----
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

		// ---- Signatures, beside the legend: role, space to sign, name / title lines ----
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
		// A4 landscape, one page wide and as many pages tall as the employees need: the
		// letterhead prints once at the top, the table headers repeat on every page, the
		// legend and the signatures come once after the last row.
		var printLastRow = Math.Max(Math.Max(lr - 1, lt), nameRow + 2);
		ws.PageSetup.PaperSize = XLPaperSize.A4Paper;
		ws.PageSetup.PagesWide = 1;   // the height is left free
		ws.PageSetup.SetRowsToRepeatAtTop(rGroup, rWd);
		ws.PageSetup.CenterHorizontally = true;
		ws.PageSetup.PrintAreas.Add(1, 1, printLastRow, lastCol);
		ws.PageSetup.Margins.Left = 0.3; ws.PageSetup.Margins.Right = 0.3;
		ws.PageSetup.Margins.Top = 0.4; ws.PageSetup.Margins.Bottom = 0.4;
		AddPageNumbers(ws);

		// The legend and the signatures never on a page without employees (an estimate: see the method).
		if (m.Rows.Count > 0)
			KeepLastRowsWithClosing(ws, lastCol, TailFirstRow(lastRow, bandRows), lastRow, printLastRow);

		using var ms = new MemoryStream();
		wb.SaveAs(ms);
		return ms.ToArray();
	}

	/// <summary>Writes a merged, styled cell (used for headers/titles).</summary>
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
		if (bytes is null) return;   // no emblem file: the form prints without it

		using var emblem = new MemoryStream(bytes);   // ClosedXML copies the image
		ws.AddPicture(emblem, name).MoveTo(ws.Cell(row, column), offsetX, offsetY).WithSize(width, height);
	}

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