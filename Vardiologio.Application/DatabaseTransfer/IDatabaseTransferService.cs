namespace Vardiologio.Application.DatabaseTransfer;

/// <summary>Outcome of checking (or importing) a file chosen as a database copy.</summary>
public enum DatabaseFileCheck
{
	/// <summary>A database of this application with the same schema.</summary>
	Ok,

	/// <summary>Not a database of this application (another kind of file, or a foreign database).</summary>
	NotDatabase,

	/// <summary>A database of this application, but of another version (different schema).</summary>
	DifferentSchema
}

/// <summary>
/// Copies of the whole database: export to a file, and import of such a file over the current
/// data. The users of this installation are never replaced by an import.
/// </summary>
public interface IDatabaseTransferService
{
	/// <summary>
	/// Writes a copy of the database to the application's internal backup folder and to
	/// Documents\Vardiologio\Database. Returns the full path of the copy in Documents.
	/// </summary>
	Task<string> ExportAsync();

	/// <summary>Checks whether the file can be imported. Call before <see cref="ImportAsync"/>.</summary>
	/// <param name="path">Full path of the chosen file.</param>
	Task<DatabaseFileCheck> CheckAsync(string path);

	/// <summary>
	/// Replaces the current data with the file's, keeping this installation's users. A copy of
	/// the current database is first written to the internal backup folder. The file is checked
	/// again; anything but <see cref="DatabaseFileCheck.Ok"/> means nothing was changed.
	/// </summary>
	/// <param name="path">Full path of the chosen file.</param>
	Task<DatabaseFileCheck> ImportAsync(string path);
}
