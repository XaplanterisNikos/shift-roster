using System.Diagnostics;
using System.IO;
using MudBlazor;

namespace Vardiologio.App.Components.Shared;

/// <summary>
/// The "file created" message of the report pages, shown as a snackbar with a button that
/// opens the folder of the file(s).
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
		var folder = Path.GetDirectoryName(paths[0]);

		snackbar.Add($"Δημιουργήθηκε: {names}", Severity.Success, options =>
		{
			// Longer than the default 5 seconds: the user may want to press the button.
			options.VisibleStateDuration = 8000;
			if (string.IsNullOrEmpty(folder)) return;

			options.Action = "Άνοιγμα φακέλου";
			options.ActionColor = Color.Inherit;
			options.OnClick = _ =>
			{
				// The folder opens in Windows Explorer; a failure here must not break the page.
				try { Process.Start(new ProcessStartInfo(folder) { UseShellExecute = true }); }
				catch (Exception) { /* the files are already written and opened */ }
				return Task.CompletedTask;
			};
		});
	}
}
