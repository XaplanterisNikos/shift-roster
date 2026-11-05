namespace Vardiologio.Application.Printing;

/// <summary>
/// Sends finished PDF reports straight to a printer, without opening them in a viewer.
/// Used by the batch printing of the individual reports: the printer is chosen once, then every
/// report of the batch goes to it.
/// </summary>
public interface IPdfPrinter
{
	/// <summary>
	/// Shows the Windows print window (printer, copies) once.
	/// </summary>
	/// <returns>The chosen printer, or null when the user closed the window without printing.</returns>
	IPrintTarget? ChoosePrinter();
}

/// <summary>A printer chosen with <see cref="IPdfPrinter.ChoosePrinter"/>, with its settings (copies).</summary>
public interface IPrintTarget
{
	/// <summary>
	/// Prints every page of a PDF as it is, in the orientation of its first page (landscape when
	/// that page is wider than tall, portrait otherwise).
	/// </summary>
	/// <param name="pdf">The PDF file, as it was saved.</param>
	/// <param name="jobName">Name of the job in the printer's queue.</param>
	Task PrintAsync(byte[] pdf, string jobName);
}
