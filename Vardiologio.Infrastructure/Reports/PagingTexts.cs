namespace Vardiologio.Infrastructure.Reports;

/// <summary>
/// The page number printed at the bottom right of every page of every report
/// ("Σελίδα 1 από 2"), the same words in the PDF and in the Excel files.
/// </summary>
internal static class PagingTexts
{
	/// <summary>Word before the page number.</summary>
	public const string Page = "Σελίδα ";

	/// <summary>Word between the page number and the number of pages.</summary>
	public const string Of = " από ";

	/// <summary>
	/// Footer code of Excel for "Σελίδα X από Y": Excel replaces &amp;P with the page number
	/// and &amp;N with the number of pages when it prints.
	/// </summary>
	public const string ExcelFooter = Page + "&P" + Of + "&N";
}
