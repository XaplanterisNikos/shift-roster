namespace Vardiologio.Application.Printing;

/// <summary>Outcome of a batch: how many reports reached the printer and who failed.</summary>
/// <param name="Printed">Reports sent to the printer.</param>
/// <param name="Failed">Names of the items whose report could not be made or printed, in batch order.</param>
public sealed record BatchPrintResult(int Printed, IReadOnlyList<string> Failed);

/// <summary>
/// The one batch-printing run shared by the individual report pages: one item after the other,
/// the page makes the report (preview on screen, PDF saved) and the PDF goes to the chosen
/// printer, then the next item starts. A failed item is reported and the batch goes on.
/// </summary>
public static class BatchPrint
{
	/// <summary>Runs the batch in the order of <paramref name="items"/>.</summary>
	/// <typeparam name="T">The item of the batch (an employee of the list).</typeparam>
	/// <param name="items">The chosen items, in the order they print.</param>
	/// <param name="nameOf">Name of an item: the printer job name and the name in the failed list.</param>
	/// <param name="progress">Called before each item with its number (from 1) and the total.</param>
	/// <param name="makePdf">Makes the report of an item and returns its PDF.</param>
	/// <param name="target">The printer chosen for the batch.</param>
	/// <param name="failed">Called when an item fails, with the error, before the next item starts.</param>
	public static async Task<BatchPrintResult> RunAsync<T>(
		IReadOnlyList<T> items,
		Func<T, string> nameOf,
		Func<int, int, Task> progress,
		Func<T, Task<byte[]>> makePdf,
		IPrintTarget target,
		Action<T, Exception> failed)
	{
		var printed = 0;
		var failedNames = new List<string>();

		for (var i = 0; i < items.Count; i++)
		{
			var item = items[i];
			await progress(i + 1, items.Count);
			try
			{
				var pdf = await makePdf(item);
				await target.PrintAsync(pdf, nameOf(item));
				printed++;
			}
			catch (Exception ex)
			{
				failed(item, ex);
				failedNames.Add(nameOf(item));
			}
		}

		return new BatchPrintResult(printed, failedNames);
	}
}
