using System.Text.Json;
using System.Text.Json.Serialization;
using Vardiologio.Domain.Enums;

namespace Vardiologio.Infrastructure.Persistence.Seeding;

/// <summary>
/// One block of lines of a form, as read from the report data file. The number of lines is
/// fixed: an empty string is a spare line the user can fill in later (an empty line is not printed).
/// </summary>
/// <param name="Form">Form the block belongs to.</param>
/// <param name="Section">Above or below the table.</param>
/// <param name="Block">Block key (see ReportTextBlocks).</param>
/// <param name="Lines">The default text of each line, in order.</param>
public sealed record ReportTextBlockSeed(ReportForm Form, ReportSection Section, string Block, string[] Lines);

/// <summary>Content of the report data file.</summary>
/// <param name="DirectorLastName">
/// Surname of the head of the directorate, used only while no employee has the director's
/// work position (see PresenceRules); null or empty = no fallback.
/// </param>
/// <param name="Texts">Default texts of the official forms, one entry per block.</param>
public sealed record ReportDataSeed(string? DirectorLastName, ReportTextBlockSeed[]? Texts);

/// <summary>
/// Reads the organisation's own report data — the default texts of the official forms
/// (service details, phones, e-mails, decisions, signatories) and the director's surname —
/// from a JSON file next to the executable, so none of it lives in the source code.
/// Prefers the real file "report-data.json"; when it is not present (e.g. a fresh clone from
/// GitHub, where the real data is git-ignored) it falls back to the committed
/// "report-data.demo.json", which has the same blocks with empty lines.
/// The texts only fill the ReportTexts table the first time; after that the user edits them in
/// Παράμετροι → Αναφορών.
/// </summary>
internal static class ReportDataFile
{
	/// <summary>Real data: git-ignored, present only on the maintainer's machine and in the installer.</summary>
	public const string RealFileName = "report-data.json";

	/// <summary>Committed fallback: every block of every form, all lines empty.</summary>
	public const string DemoFileName = "report-data.demo.json";

	/// <summary>Reads the real file, or the demo file when the real one is absent.</summary>
	public static ReportDataSeed Load()
	{
		// JSON files are copied next to the executable (see the copy rules in Vardiologio.App.csproj).
		var dir = AppContext.BaseDirectory;
		var realPath = Path.Combine(dir, RealFileName);
		var demoPath = Path.Combine(dir, DemoFileName);
		var path = File.Exists(realPath) ? realPath : demoPath;

		if (!File.Exists(path))
			throw new FileNotFoundException(
				$"No report data file found ({RealFileName} or {DemoFileName}) next to the app.");

		// Case-insensitive so camelCase JSON maps to the PascalCase records; enums are written by name.
		var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
		options.Converters.Add(new JsonStringEnumConverter());

		return JsonSerializer.Deserialize<ReportDataSeed>(File.ReadAllText(path), options)
			?? new ReportDataSeed(null, null);
	}
}
