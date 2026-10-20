namespace Vardiologio.Application.Logging;

/// <summary>One entry of the error log files.</summary>
/// <param name="Time">When the error was written.</param>
/// <param name="Source">Where it happened, e.g. "Hours report / Excel export".</param>
/// <param name="Message">First line of the exception (type and message).</param>
/// <param name="Details">The full exception text, stack trace included.</param>
public record ErrorLogItem(DateTime Time, string Source, string Message, string Details);

/// <summary>Reads the daily error log files (Documents\Vardiologio\Logs) for the administrator's page.</summary>
public interface IErrorLogReader
{
	/// <summary>The entries of the days in the range, newest first.</summary>
	/// <param name="from">First day included; null = no lower bound.</param>
	/// <param name="to">Last day included; null = no upper bound.</param>
	Task<IReadOnlyList<ErrorLogItem>> ReadAsync(DateTime? from, DateTime? to);
}
