using ClosedXML.Excel;
using Vardiologio.Application.Hours;
using Vardiologio.Application.Reports;
using Vardiologio.Domain.Enums;

namespace Vardiologio.Infrastructure.Reports;

/// <summary>Hours report (declared / paid / excess per category) — reuses Put/Merge of the Σ.Ω. part.</summary>
public partial class ClosedXmlReportRenderer
{
	/// <summary>Sub-columns under each category, in order.</summary>
	private static readonly string[] HoursSubHeaders = { "ΔΗΛ.", "ΠΛΗΡ.", "ΠΕΡ." };

	/// <inheritdoc/>
	public byte[] Render(HoursReportModel m)
	{
		var hdrFill = XLColor.FromHtml("#D9E1F2");
		var totFill = XLColor.FromHtml("#E2EFDA");
		var overFill = XLColor.FromHtml("#FDE2E1");   // excess (unpaid) hours

		var categories = Enum.GetValues<HourCategory>();

		using var wb = new XLWorkbook();
		var ws = wb.AddWorksheet("Ώρες");

		// Columns: Α/Α, name, speciality, then 3 per category (declared / paid / excess).
		const int cAA = 1, cName = 2, cSpec = 3, cCat0 = 4;
		var lastCol = cCat0 + categories.Length * 3 - 1;

		// Title block.
		Merge(ws, 1, cName, 1, lastCol, "ΔΙΕΥΘΥΝΣΗ ΣΤΑΘΜΩΝ ΜΕΤΑΦΟΡΤΩΣΗΣ — ΤΜΗΜΑ ΚΕΝΤΡΙΚΩΝ ΣΜΑ", 10, true, XLAlignmentHorizontalValues.Center, null);
		Merge(ws, 2, cName, 2, lastCol, $"ΜΗΝΙΑΙΑ ΚΑΤΑΣΤΑΣΗ ΩΡΩΝ — ΜΗΝΟΣ {MonthsGen[m.Month - 1]} {m.Year}", 12, true, XLAlignmentHorizontalValues.Center, null);

		// Table headers: row 4 = category (with its limit), row 5 = sub-columns.
		const int rCat = 4, rSub = 5, rData0 = 6;
		Merge(ws, rCat, cAA, rSub, cAA, "Α/Α", 8, true, XLAlignmentHorizontalValues.Center, hdrFill);
		Merge(ws, rCat, cName, rSub, cName, "ΟΝΟΜΑΤΕΠΩΝΥΜΟ", 9, true, XLAlignmentHorizontalValues.Center, hdrFill);
		Merge(ws, rCat, cSpec, rSub, cSpec, "ΕΙΔΙΚΟΤΗΤΑ", 8, true, XLAlignmentHorizontalValues.Center, hdrFill);

		for (var i = 0; i < categories.Length; i++)
		{
			var c0 = cCat0 + i * 3;
			var title = $"{HoursLabels.CategoryHeader(categories[i])} (ΟΡΙΟ {Num(m.Limits[categories[i]])})";
			Merge(ws, rCat, c0, rCat, c0 + 2, title, 8, true, XLAlignmentHorizontalValues.Center, totFill);
			for (var k = 0; k < 3; k++)
				Put(ws, rSub, c0 + k, HoursSubHeaders[k], 7, true, totFill);
		}

		// One row per employee; empty cells for zeros keep the sheet readable.
		var r = rData0;
		foreach (var row in m.Rows)
		{
			Put(ws, r, cAA, row.Index, 8, false, null);
			Put(ws, r, cName, $"{row.LastName} {row.FirstName}".Trim(), 8, false, null)
				.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
			Put(ws, r, cSpec, row.Speciality, 8, false, null)
				.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

			for (var i = 0; i < categories.Length; i++)
			{
				var h = row.Hours[i];
				var c0 = cCat0 + i * 3;
				Put(ws, r, c0, Value(h.Declared), 8, false, null);
				Put(ws, r, c0 + 1, Value(h.Paid), 8, false, null);
				Put(ws, r, c0 + 2, Value(h.Excess), 8, h.Excess > 0, h.Excess > 0 ? overFill : null);
			}
			r++;
		}

		// Totals row.
		Merge(ws, r, cAA, r, cSpec, "ΣΥΝΟΛΑ", 8, true, XLAlignmentHorizontalValues.Right, totFill);
		for (var i = 0; i < categories.Length; i++)
		{
			var c0 = cCat0 + i * 3;
			Put(ws, r, c0, Value(m.Total(categories[i], h => h.Declared)), 8, true, totFill);
			Put(ws, r, c0 + 1, Value(m.Total(categories[i], h => h.Paid)), 8, true, totFill);
			Put(ws, r, c0 + 2, Value(m.Total(categories[i], h => h.Excess)), 8, true, totFill);
		}
		var lastRow = r;

		// Legend under the table.
		Merge(ws, lastRow + 2, cAA, lastRow + 2, lastCol,
			$"ΔΗΛ. = δηλωμένες, ΠΛΗΡ. = πληρώνονται, ΠΕΡ. = περισσεύουν (δεν πληρώνονται). " +
			$"Όριο αργίας = {Num(m.Limits[HourCategory.Holiday])} ({m.HolidaysInMonth} αργίες τον μήνα). " +
			$"Προς συμπλήρωση + Αργίας: κοινό όριο {Num(m.CombinedLimit)}, κόβεται πρώτα το προς συμπλήρωση.",
			7, false, XLAlignmentHorizontalValues.Left, null);

		// Borders / widths / page setup (A4 landscape, one page wide).
		var table = ws.Range(rCat, 1, lastRow, lastCol);
		table.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
		table.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

		ws.Column(cAA).Width = 4;
		ws.Column(cName).Width = 24;
		ws.Column(cSpec).Width = 18;
		for (var c = cCat0; c <= lastCol; c++) ws.Column(c).Width = 6.5;
		ws.Row(rCat).Height = 30;   // category titles wrap onto two lines

		ws.SheetView.FreezeRows(rSub);
		ws.SheetView.FreezeColumns(cSpec);

		ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
		ws.PageSetup.PaperSize = XLPaperSize.A4Paper;
		ws.PageSetup.PagesWide = 1;
		ws.PageSetup.SetRowsToRepeatAtTop(rCat, rSub);
		ws.PageSetup.Margins.Left = 0.3; ws.PageSetup.Margins.Right = 0.3;
		ws.PageSetup.Margins.Top = 0.4; ws.PageSetup.Margins.Bottom = 0.4;

		using var ms = new MemoryStream();
		wb.SaveAs(ms);
		return ms.ToArray();
	}

	/// <summary>Numeric cell value, or empty for zero (stored as a real number, not text).</summary>
	private static XLCellValue Value(decimal hours) => hours == 0 ? "" : (double)hours;

	/// <summary>Hours as text for titles/legend ("6,5", "120"), current culture.</summary>
	private static string Num(decimal hours) => hours.ToString("0.##");
}
