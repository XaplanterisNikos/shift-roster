namespace Vardiologio.Infrastructure.Persistence;

/// <summary>
/// Single source of truth for where the local SQLite database lives and how to connect to it.
/// The database is stored per-user under %LOCALAPPDATA%\Vardiologio, which is always writable
/// without admin rights and survives app updates.
/// </summary>
public static class AppPaths
{
	/// <summary>
	/// Full path to the SQLite file. Ensures the containing folder exists before returning.
	/// </summary>
	public static string DbPath
	{
		get
		{
			// Per-user, writable, update-safe location (e.g. C:\Users\<user>\AppData\Local).
			var dir = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
				"Vardiologio");
			Directory.CreateDirectory(dir);
			return Path.Combine(dir, "vardiologio.db");
		}
	}

	/// <summary>SQLite connection string pointing at <see cref="DbPath"/>.</summary>
	public static string ConnectionString => $"Data Source={DbPath}";
}