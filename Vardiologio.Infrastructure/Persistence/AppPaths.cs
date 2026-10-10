namespace Vardiologio.Infrastructure.Persistence;

/// <summary>
/// Single source of truth for where the local SQLite database lives and how to connect to it.
/// The database is stored per-user under %LOCALAPPDATA%\Vardiologio, which is always writable
/// without admin rights and survives app updates.
/// </summary>
public static class AppPaths
{
	/// <summary>
	/// %LOCALAPPDATA%\Vardiologio: per-user, writable, update-safe (e.g. C:\Users\&lt;user&gt;\AppData\Local).
	/// Created if missing.
	/// </summary>
	private static string DataDir
	{
		get
		{
			var dir = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
				"Vardiologio");
			Directory.CreateDirectory(dir);
			return dir;
		}
	}

	/// <summary>
	/// Full path to the SQLite file. Ensures the containing folder exists before returning.
	/// </summary>
	public static string DbPath => Path.Combine(DataDir, "vardiologio.db");

	/// <summary>
	/// Internal folder of database copies (exports, and the safety copy taken before an import).
	/// Not shown to the user. Created if missing.
	/// </summary>
	public static string BackupsDir
	{
		get
		{
			var dir = Path.Combine(DataDir, "Backups");
			Directory.CreateDirectory(dir);
			return dir;
		}
	}

	/// <summary>SQLite connection string pointing at <see cref="DbPath"/>.</summary>
	public static string ConnectionString => $"Data Source={DbPath}";
}