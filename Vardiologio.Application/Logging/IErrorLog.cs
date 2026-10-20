namespace Vardiologio.Application.Logging;

/// <summary>
/// Records errors — and only errors — in a daily log file under Documents\Vardiologio\Logs.
/// Logging never throws: a failure to write the log is swallowed so it cannot hide the original error.
/// </summary>
public interface IErrorLog
{
	/// <summary>Appends one error entry: time, where it happened, and the full exception.</summary>
	/// <param name="source">Where it happened, e.g. "Hours report / Excel export".</param>
	/// <param name="exception">The exception, including its stack trace.</param>
	void Error(string source, Exception exception);
}
