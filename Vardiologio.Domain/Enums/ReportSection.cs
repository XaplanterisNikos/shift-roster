namespace Vardiologio.Domain.Enums;

/// <summary>Where a report text is printed relative to the table of the form.</summary>
public enum ReportSection
{
	/// <summary>Above the table (letterhead, titles, addressee, introduction).</summary>
	Header = 1,

	/// <summary>Below the table (declaration, signatures).</summary>
	Footer = 2
}
