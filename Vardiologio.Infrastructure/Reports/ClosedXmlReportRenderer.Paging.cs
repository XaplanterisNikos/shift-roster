using ClosedXML.Excel;

namespace Vardiologio.Infrastructure.Reports;

/// <summary>
/// Page rules shared by every Excel form:
/// <list type="bullet">
/// <item>"Σελίδα X από Y" at the bottom right of every printed page (<see cref="AddPageNumbers"/>);</item>
/// <item>the closing block of a form (signatures, legend) should not print on a page without
/// employees (<see cref="KeepLastRowsWithClosing"/>). Excel breaks the pages itself, so this one
/// is an estimate; the user can still move a page break in Excel.</item>
/// </list>
/// </summary>
public partial class ClosedXmlReportRenderer
{
	/// <summary>Distance of the page number from the bottom edge of the paper, in inches.</summary>
	private const double FooterMargin = 0.12;

	/// <summary>
	/// Points kept free at the bottom of every page in the estimate. Excel's own arithmetic may
	/// differ a little; when in doubt the last rows go with the signatures, which at worst adds a
	/// page but never leaves the signatures alone.
	/// </summary>
	private const double SafetyPoints = 6;

	/// <summary>
	/// Puts "Σελίδα X από Y" at the bottom right of every printed page. Excel fills in the numbers
	/// when it prints. Call it after the margins are set: the footer goes inside the bottom margin.
	/// </summary>
	private static void AddPageNumbers(IXLWorksheet ws)
	{
		var setup = ws.PageSetup;
		setup.Footer.Right.AddText(PagingTexts.ExcelFooter).SetFontName("Arial").SetFontSize(8);
		setup.Margins.Footer = FooterMargin;    // inside the bottom margin, under the content
		setup.ScaleHFWithDocument = false;      // same size even when the sheet is scaled to fit
	}

	/// <summary>
	/// Adds a manual page break before the last rows of the table when Excel would otherwise
	/// print the closing block (signatures, legend) — or part of it — on a page without
	/// employees. The last rows and the closing block then start a page together. Nothing changes
	/// when the last page already has employees on it.
	/// <para>
	/// Call it last, when every row height, column width, margin, repeated header row and manual
	/// page break of the sheet is set: the estimate reads them all (see <see cref="ExcelPageEstimate"/>).
	/// Only for A4 sheets printed one page wide, as the forms that use it are.
	/// </para>
	/// </summary>
	/// <param name="ws">The worksheet.</param>
	/// <param name="lastColumn">Last column of the print area (it starts at column 1).</param>
	/// <param name="tailFirstRow">First row that must go with the closing block: the last employee,
	/// or the band right above it (see <see cref="TailFirstRow"/>).</param>
	/// <param name="lastTableRow">Last row of the table.</param>
	/// <param name="lastPrintRow">Last row of the print area (the end of the closing block).</param>
	private static void KeepLastRowsWithClosing(IXLWorksheet ws, int lastColumn, int tailFirstRow, int lastTableRow, int lastPrintRow)
	{
		var setup = ws.PageSetup;
		if (setup.PaperSize != XLPaperSize.A4Paper || tailFirstRow <= 1) return;

		// The printable area of the paper, in points.
		var landscape = setup.PageOrientation == XLPageOrientation.Landscape;
		var paperWidth = landscape ? ExcelPageEstimate.A4Long : ExcelPageEstimate.A4Short;
		var paperHeight = landscape ? ExcelPageEstimate.A4Short : ExcelPageEstimate.A4Long;
		var printableWidth = paperWidth - (setup.Margins.Left + setup.Margins.Right) * ExcelPageEstimate.PointsPerInch;
		var printableHeight = paperHeight - (setup.Margins.Top + setup.Margins.Bottom) * ExcelPageEstimate.PointsPerInch;

		// One page wide: Excel shrinks everything by the same scale, row heights included.
		var contentWidth = 0.0;
		for (var c = 1; c <= lastColumn; c++)
			if (!ws.Column(c).IsHidden) contentWidth += ExcelPageEstimate.ColumnPoints(ws.Column(c).Width);
		var scale = setup.PagesWide == 1 ? ExcelPageEstimate.FitWidthScale(printableWidth, contentWidth) : 1;
		var capacity = printableHeight / scale - SafetyPoints;

		var pages = ExcelPageEstimate.Pages(
			r => ws.Row(r).IsHidden ? 0 : ws.Row(r).Height,
			lastPrintRow, capacity,
			setup.FirstRowToRepeatAtTop, setup.LastRowToRepeatAtTop, setup.RowBreaks);

		// The last page has employees on it, or the rows that would move already start a page:
		// nothing to do.
		if (pages[lastPrintRow] == pages[lastTableRow]) return;
		if (pages[tailFirstRow - 1] != pages[tailFirstRow]) return;

		setup.AddHorizontalPageBreak(tailFirstRow - 1);
	}

	/// <summary>
	/// The first row that goes with the closing block: the last row of the table and every band
	/// (full-width title row) right above it, so a band never stays alone at the bottom of a page.
	/// </summary>
	/// <param name="lastTableRow">Last row of the table.</param>
	/// <param name="bandRows">Rows of the table that are bands.</param>
	private static int TailFirstRow(int lastTableRow, ISet<int> bandRows)
	{
		var first = lastTableRow;
		while (bandRows.Contains(first - 1)) first--;
		return first;
	}
}
