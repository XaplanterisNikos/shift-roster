namespace Vardiologio.Application.Files;

/// <summary>Packs everything the application keeps in Documents\Vardiologio into one zip file.</summary>
public interface IDocumentsArchive
{
	/// <summary>
	/// False when the target is inside Documents\Vardiologio: the archive would then have to
	/// contain itself. Check this before <see cref="CreateAsync"/>.
	/// </summary>
	/// <param name="targetPath">Full path of the zip file to write.</param>
	bool CanSaveTo(string targetPath);

	/// <summary>
	/// Writes the zip (replacing a file with the same name): logs, exported reports and
	/// database copies, under a top folder "Vardiologio".
	/// </summary>
	/// <param name="targetPath">Full path of the zip file to write.</param>
	Task CreateAsync(string targetPath);
}
