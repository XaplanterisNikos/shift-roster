using System.IO.Compression;
using Vardiologio.Application.Activity;
using Vardiologio.Application.Files;

namespace Vardiologio.Infrastructure.Files;

/// <summary><see cref="IDocumentsArchive"/> over <see cref="AppFolders.Root"/>.</summary>
public class DocumentsArchive : IDocumentsArchive
{
	private readonly IActivityLog _activity;
	public DocumentsArchive(IActivityLog activity) => _activity = activity;

	/// <inheritdoc/>
	public bool CanSaveTo(string targetPath)
	{
		var root = Path.GetFullPath(AppFolders.Root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
		return !Path.GetFullPath(targetPath).StartsWith(root, StringComparison.OrdinalIgnoreCase);
	}

	/// <inheritdoc/>
	public async Task CreateAsync(string targetPath)
	{
		await Task.Run(() =>
		{
			File.Delete(targetPath);   // no error when it does not exist; the save dialog already asked
			ZipFile.CreateFromDirectory(AppFolders.Root, targetPath, CompressionLevel.Optimal, includeBaseDirectory: true);
		});

		await _activity.RecordAsync("Εξαγωγή αρχείων", "Αρχεία εφαρμογής (zip)", Path.GetFileName(targetPath));
	}
}
