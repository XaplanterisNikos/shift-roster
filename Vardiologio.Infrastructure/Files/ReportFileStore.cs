using Vardiologio.Application.Files;

namespace Vardiologio.Infrastructure.Files;

/// <summary>File-system implementation of <see cref="IReportFileStore"/> (folders from <see cref="AppFolders"/>).</summary>
public class ReportFileStore : IReportFileStore
{
	/// <inheritdoc/>
	public async Task<string> SaveAsync(ReportFolder folder, int year, int month, string fileName, byte[] content)
	{
		var path = Path.Combine(AppFolders.ReportMonth(folder, year, month), fileName);
		await File.WriteAllBytesAsync(path, content);   // replaces an older export of the same month
		return path;
	}
}
