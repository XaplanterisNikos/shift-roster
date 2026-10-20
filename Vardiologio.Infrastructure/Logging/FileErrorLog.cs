using Vardiologio.Application.Logging;

namespace Vardiologio.Infrastructure.Logging;

/// <summary><see cref="IErrorLog"/> for the UI; writes through <see cref="ErrorLogFile"/>.</summary>
public class FileErrorLog : IErrorLog
{
	/// <inheritdoc/>
	public void Error(string source, Exception exception) => ErrorLogFile.Write(source, exception);
}
