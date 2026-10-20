namespace Vardiologio.Infrastructure.Reports;

/// <summary>
/// Estimate of where Excel will break a sheet into printed pages. Excel decides the page breaks
/// itself, when it prints, so the app can only repeat its arithmetic: the scale that makes the
/// print area one page wide, the height of every row, the header rows repeated on every page
/// and the manual page breaks. Plain numbers only (no ClosedXML), so it can be tested on its own;
/// <see cref="ClosedXmlReportRenderer"/> feeds it the sheet.
/// <para>
/// It is an estimate: a different printer, scale or margins chosen in Excel moves the breaks.
/// </para>
/// </summary>
internal static class ExcelPageEstimate
{
	/// <summary>Points per inch (margins are stored in inches).</summary>
	public const double PointsPerInch = 72;

	/// <summary>Long side of an A4 sheet, in points.</summary>
	public const double A4Long = 841.89;

	/// <summary>Short side of an A4 sheet, in points.</summary>
	public const double A4Short = 595.28;

	/// <summary>
	/// Padding that ClosedXML adds to every column width when it saves the file (the width Excel
	/// reads is the width set in the code plus this).
	/// </summary>
	private const double ColumnPadding = 0.710625;

	/// <summary>Width in pixels of one character of the default font (Calibri 11).</summary>
	private const double CharacterPixels = 7;

	/// <summary>Points per screen pixel (96 pixels per inch).</summary>
	private const double PointsPerPixel = 0.75;

	/// <summary>
	/// Width in points of a column whose width was set to <paramref name="width"/> characters,
	/// with Excel's own rounding to whole pixels.
	/// </summary>
	public static double ColumnPoints(double width) =>
		Math.Floor(CharacterPixels * (width + ColumnPadding) + 0.492) * PointsPerPixel;

	/// <summary>
	/// The scale Excel prints at when the sheet must fit one page wide: whole percents, never
	/// more than 100%.
	/// </summary>
	/// <param name="printableWidth">Paper width minus the left and right margins, in points.</param>
	/// <param name="contentWidth">Width of the columns of the print area, in points.</param>
	public static double FitWidthScale(double printableWidth, double contentWidth) =>
		contentWidth <= printableWidth ? 1 : Math.Floor(printableWidth / contentWidth * 100) / 100;

	/// <summary>
	/// The printed page (1, 2, …) of every row from 1 to <paramref name="lastRow"/>; index 0 is unused.
	/// </summary>
	/// <param name="rowHeight">Height of a row in points (0 for a hidden row).</param>
	/// <param name="lastRow">Last row of the print area (the print area starts at row 1).</param>
	/// <param name="capacity">Height available on a page, in points of the sheet (before scaling).</param>
	/// <param name="repeatFirst">First row repeated at the top of every page; 0 when none.</param>
	/// <param name="repeatLast">Last row repeated at the top of every page; 0 when none.</param>
	/// <param name="breaksAfter">Rows after which a manual page break was added.</param>
	public static int[] Pages(Func<int, double> rowHeight, int lastRow, double capacity,
		int repeatFirst, int repeatLast, IEnumerable<int> breaksAfter)
	{
		var breaks = new HashSet<int>(breaksAfter);

		// Height of the header rows that Excel prints again at the top of every next page.
		var repeated = 0.0;
		if (repeatFirst > 0 && repeatLast >= repeatFirst)
			for (var r = repeatFirst; r <= repeatLast; r++) repeated += rowHeight(r);

		var pages = new int[lastRow + 1];
		var page = 1;
		var used = 0.0;
		for (var r = 1; r <= lastRow; r++)
		{
			var height = rowHeight(r);

			// A new page: after a manual break, or when the row no longer fits (a row taller than
			// a whole page is still printed, on a page of its own).
			var manual = breaks.Contains(r - 1);
			if (manual || (used > 0 && used + height > capacity))
			{
				page++;
				used = repeatFirst > 0 && r > repeatLast ? repeated : 0;
			}

			used += height;
			pages[r] = page;
		}
		return pages;
	}
}
