using System.Diagnostics;
using System.IO;
using MudBlazor;
using Vardiologio.Application.Printing;

namespace Vardiologio.App.Components.Shared;

/// <summary>
/// The closing messages of the report pages ("file created", "batch printed"), shown as a
/// snackbar with a button that opens the folder of the file(s).
/// </summary>
public static class ReportSnackbar
{
	/// <summary>
	/// Shows which file(s) were written (names only; the folder opens from the button).
	/// </summary>
	/// <param name="snackbar">The snackbar service of the page.</param>
	/// <param name="paths">Full path of every file written by the export.</param>
	public static void ReportSaved(this ISnackbar snackbar, params string[] paths)
	{
		if (paths.Length == 0) return;

		var names = string.Join(", ", paths.Select(Path.GetFileName));
		snackbar.Add($"Δημιουργήθηκε: {names}", Severity.Success,
			options => FolderButton(options, Path.GetDirectoryName(paths[0]), 8000));
	}

	/// <summary>
	/// Says how a batch print went: how many of the ticked employees reached the printer and,
	/// when some failed, their names (the errors are in the log).
	/// </summary>
	/// <param name="snackbar">The snackbar service of the page.</param>
	/// <param name="result">The outcome of the batch.</param>
	/// <param name="total">How many employees were ticked.</param>
	/// <param name="savedPdf">A PDF the batch saved, whose folder the button opens; null when none was saved.</param>
	public static void BatchPrinted(this ISnackbar snackbar, BatchPrintResult result, int total, string? savedPdf)
	{
		var text = $"Στάλθηκαν στον εκτυπωτή {result.Printed} από {total}.";
		if (result.Failed.Count > 0)
			text += $" Δεν τυπώθηκαν: {string.Join(", ", result.Failed)}.";

		snackbar.Add(text, result.Failed.Count == 0 ? Severity.Success : Severity.Warning,
			options => FolderButton(options, savedPdf is null ? null : Path.GetDirectoryName(savedPdf),
				result.Failed.Count == 0 ? 8000 : 20000));
	}

	/// <summary>Sets how long the message stays and its "Άνοιγμα φακέλου" button (none without a folder).</summary>
	/// <param name="options">The options of the message.</param>
	/// <param name="folder">The folder the button opens.</param>
	/// <param name="duration">How long the message stays, in milliseconds.</param>
	private static void FolderButton(SnackbarOptions options, string? folder, int duration)
	{
		options.VisibleStateDuration = duration;
		if (string.IsNullOrEmpty(folder)) return;

		options.Action = "Άνοιγμα φακέλου";
		options.ActionColor = Color.Inherit;
		options.OnClick = _ =>
		{
			try { Process.Start(new ProcessStartInfo(folder) { UseShellExecute = true }); }
			catch (Exception) { }
			return Task.CompletedTask;
		};
	}
}
