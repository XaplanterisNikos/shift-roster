using Vardiologio.Infrastructure.Files;

namespace Vardiologio.Infrastructure.Logging;

/// <summary>
/// Writes error entries to Documents\Vardiologio\Logs\yyyy-MM-dd.log (one file per day).
/// Static so it also works before the DI container exists (startup errors, global handlers);
/// <see cref="FileErrorLog"/> exposes it to the UI through <c>IErrorLog</c>.
/// </summary>
public static class ErrorLogFile
{
	/// <summary>Serialises writes from different threads to the same file.</summary>
	private static readonly object Gate = new();

	/// <summary>
	/// Appends one entry: time, source and the full exception (type, message, stack trace, inner
	/// exceptions). Never throws — a failing log must not hide the error being logged.
	/// </summary>
	/// <param name="source">Where it happened, e.g. "Startup" or "Hours report / Excel export".</param>
	/// <param name="exception">The exception, or null when the runtime gave none.</param>
	/// <returns>The log file path, or null when writing failed.</returns>
	public static string? Write(string source, Exception? exception)
	{
		try
		{
			var now = DateTime.Now;
			var path = Path.Combine(AppFolders.Logs, $"{now:yyyy-MM-dd}.log");
			var entry =
				$"{now:yyyy-MM-dd HH:mm:ss} | {source}{Environment.NewLine}" +
				$"{exception?.ToString() ?? "(no exception details)"}{Environment.NewLine}" +
				$"{new string('-', 80)}{Environment.NewLine}";

			lock (Gate) File.AppendAllText(path, entry);
			return path;
		}
		catch
		{
			return null;   // last resort: nowhere left to report it
		}
	}
}
