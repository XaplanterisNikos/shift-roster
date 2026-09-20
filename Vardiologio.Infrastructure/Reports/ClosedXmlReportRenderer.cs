using ClosedXML.Excel;
using Vardiologio.Application.Reports;

namespace Vardiologio.Infrastructure.Reports;

/// <summary>Renders the Σ.Ω. monthly roster to an .xlsx file using ClosedXML.</summary>
public class ClosedXmlReportRenderer : IExcelReportRenderer
{
	private static readonly string[] MonthsGen =
		{ "ΙΑΝΟΥΑΡΙΟΥ","ΦΕΒΡΟΥΑΡΙΟΥ","ΜΑΡΤΙΟΥ","ΑΠΡΙΛΙΟΥ","ΜΑΪΟΥ","ΙΟΥΝΙΟΥ",
		  "ΙΟΥΛΙΟΥ","ΑΥΓΟΥΣΤΟΥ","ΣΕΠΤΕΜΒΡΙΟΥ","ΟΚΤΩΒΡΙΟΥ","ΝΟΕΜΒΡΙΟΥ","ΔΕΚΕΜΒΡΙΟΥ" };
	private static readonly string[] WdGr = { "Δε", "Τρ", "Τε", "Πε", "Πα", "Σα", "Κυ" }; // Δευτέρα-based

	/// <inheritdoc/>
	public byte[] Render(ReportModel m)
	{
		var hdrFill = XLColor.FromHtml("#D9E1F2");
		var sunFill = XLColor.FromHtml("#FCE4D6");
		var satFill = XLColor.FromHtml("#FFF2CC");
		var totFill = XLColor.FromHtml("#E2EFDA");

		using var wb = new XLWorkbook();
		var ws = wb.AddWorksheet("Σ.Ω.");

		var days = m.DaysInMonth;
		const int cAA = 1, cName = 2, cSpec = 3, cDay0 = 4;
		var cDayEnd = cDay0 + days - 1;
		var cCount0 = cDayEnd + 1;
		var cCountEnd = cCount0 + 6;         
		var cHmer = cCountEnd + 1;
		var cKyr = cHmer + 1;
		var lastCol = cKyr;

		// Header block (fixed organisation text)
		Merge(ws, 1, cDay0, 1, cDay0 + 10, "ΔΙΕΥΘΥΝΣΗ ΣΤΑΘΜΩΝ ΜΕΤΑΦΟΡΤΩΣΗΣ", 11, true, XLAlignmentHorizontalValues.Center, null);
		Merge(ws, 2, cName, 2, 8, "ΕΛΛΗΝΙΚΗ ΔΗΜΟΚΡΑΤΙΑ", 9, true, XLAlignmentHorizontalValues.Left, null);
		Merge(ws, 3, cName, 3, 8, "ΠΕΡΙΦΕΡΕΙΑ ΑΤΤΙΚΗΣ", 9, false, XLAlignmentHorizontalValues.Left, null);
		Merge(ws, 4, cName, 4, 10, "ΕΙΔΙΚΟΣ ΔΙΑΒΑΘΜΙΔΙΚΟΣ ΣΥΝΔΕΣΜΟΣ", 9, true, XLAlignmentHorizontalValues.Left, null);
		Merge(ws, 5, cName, 5, 12, "ΕΔΡΑ: Άντερσεν 6 και Μωραΐτη 90, 11525 Αθήνα", 8, false, XLAlignmentHorizontalValues.Left, null);
		Merge(ws, 6, cName, 6, 12, "Πληρ.: Κασσιανή Μακρή   Τηλ.: 210 4015080, 210 4320581   e-mail: makri@edsna.gr", 8, false, XLAlignmentHorizontalValues.Left, null);

		Merge(ws, 3, cDay0 + 11, 3, cDay0 + 24, "ΠΙΝΑΚΑΣ ΒΑΡΔΙΩΝ ΕΡΓΑΣΙΑΣ ΠΡΟΣΩΠΙΚΟΥ", 12, true, XLAlignmentHorizontalValues.Center, null);
		Merge(ws, 4, cDay0 + 11, 4, cDay0 + 24, "ΤΜΗΜΑΤΟΣ ΚΕΝΤΡΙΚΩΝ ΣΜΑ", 10, true, XLAlignmentHorizontalValues.Center, null);
		Merge(ws, 5, cDay0 + 11, 5, cDay0 + 24, $"ΜΗΝΟΣ {MonthsGen[m.Month - 1]} {m.Year}", 11, true, XLAlignmentHorizontalValues.Center, null);
		Merge(ws, 2, cHmer - 6, 2, cKyr, "ΠΡΟΣ: Αναπλ/τρια Προϊστ. Τμ. Λογιστηρίου", 8, false, XLAlignmentHorizontalValues.Right, null);
		Merge(ws, 3, cHmer - 6, 3, cKyr, "κ. Αγιανίδου Δέσποινα", 8, false, XLAlignmentHorizontalValues.Right, null);

		// Table column headers
		const int rGroup = 8, rDay = 9, rWd = 10, rData0 = 11;

		Merge(ws, rGroup, cAA, rWd, cAA, "Α/Α", 8, true, XLAlignmentHorizontalValues.Center, hdrFill);
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

		// Data rows
		var r = rData0;
		foreach (var row in m.Rows)
		{
			Put(ws, r, cAA, row.Index, 8, false, null);
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
			r++;
		}
		var lastRow = Math.Max(r - 1, rData0);

		// Borders / column widths / page setup
		var table = ws.Range(rGroup, 1, lastRow, lastCol);
		table.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
		table.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

		ws.Column(cAA).Width = 4;
		ws.Column(cName).Width = 22;
		ws.Column(cSpec).Width = 16;
		for (var c = cDay0; c <= cCountEnd; c++) ws.Column(c).Width = 3.4;
		ws.Column(cHmer).Width = 8;
		ws.Column(cKyr).Width = 9;

		ws.SheetView.FreezeRows(rWd);
		ws.SheetView.FreezeColumns(cSpec);

		ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
		ws.PageSetup.PaperSize = XLPaperSize.A3Paper;
		ws.PageSetup.PagesWide = 1;
		ws.PageSetup.SetRowsToRepeatAtTop(rGroup, rWd);
		ws.PageSetup.Margins.Left = 0.3; ws.PageSetup.Margins.Right = 0.3;
		ws.PageSetup.Margins.Top = 0.4; ws.PageSetup.Margins.Bottom = 0.4;

		using var ms = new MemoryStream();
		wb.SaveAs(ms);
		return ms.ToArray();
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