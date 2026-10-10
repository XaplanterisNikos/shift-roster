using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Vardiologio.Application.DatabaseTransfer;
using Vardiologio.Domain.Entities;
using Vardiologio.Infrastructure.Files;
using Vardiologio.Infrastructure.Persistence;

namespace Vardiologio.Infrastructure.DatabaseTransfer;

/// <summary>
/// SQLite implementation of <see cref="IDatabaseTransferService"/>. Copies are made with
/// SQLite's online backup, which gives a consistent file even while the database is in use.
/// </summary>
public class DatabaseTransferService : IDatabaseTransferService
{
	/// <summary>Copies kept in the internal backup folder; older ones are removed.</summary>
	private const int KeptBackups = 20;

	private readonly IDbContextFactory<AppDbContext> _factory;
	public DatabaseTransferService(IDbContextFactory<AppDbContext> factory) => _factory = factory;

	/// <inheritdoc/>
	public Task<string> ExportAsync() => Task.Run(() =>
	{
		var backup = WriteBackup();

		// Same file name in Documents\Vardiologio\Database, for the user to keep or carry.
		var target = Path.Combine(AppFolders.Database, Path.GetFileName(backup));
		File.Copy(backup, target, overwrite: true);
		return target;
	});

	/// <inheritdoc/>
	public async Task<DatabaseFileCheck> CheckAsync(string path)
	{
		var known = await KnownMigrationsAsync();
		return await Task.Run(() =>
		{
			var temp = CopyToTemp(path);
			try { return Check(temp, known); }
			finally { DeleteQuietly(temp); }
		});
	}

	/// <inheritdoc/>
	public async Task<DatabaseFileCheck> ImportAsync(string path)
	{
		var known = await KnownMigrationsAsync();

		// This installation's users, to put back in place of the file's.
		List<User> users;
		await using (var db = await _factory.CreateDbContextAsync())
			users = await db.Users.AsNoTracking().ToListAsync();

		return await Task.Run(() =>
		{
			// Work on a private copy: the chosen file is never modified.
			var temp = CopyToTemp(path);
			try
			{
				var check = Check(temp, known);
				if (check != DatabaseFileCheck.Ok) return check;

				WriteBackup();   // safety copy of the current data, before anything changes

				ReplaceUsers(temp, users);

				// Overwrite the live database with the prepared copy in one step.
				SqliteConnection.ClearAllPools();   // release idle pooled connections to the live file
				using (var source = Open(temp))
				using (var live = Open(AppPaths.DbPath))
					source.BackupDatabase(live);

				return DatabaseFileCheck.Ok;
			}
			finally
			{
				DeleteQuietly(temp);
			}
		});
	}

	/// <summary>Migration ids of this version of the application, i.e. its schema.</summary>
	private async Task<HashSet<string>> KnownMigrationsAsync()
	{
		await using var db = await _factory.CreateDbContextAsync();
		return db.Database.GetMigrations().ToHashSet();
	}

	/// <summary>
	/// Copies the live database into the internal backup folder as vardiologio_yyyy-MM-dd_HHmmss.db,
	/// trims the folder to the newest <see cref="KeptBackups"/> files and returns the new file's path.
	/// </summary>
	private static string WriteBackup()
	{
		var path = Path.Combine(AppPaths.BackupsDir, $"vardiologio_{DateTime.Now:yyyy-MM-dd_HHmmss}.db");

		using (var live = Open(AppPaths.DbPath))
		using (var copy = Open(path))
		{
			live.BackupDatabase(copy);

			// A single self-contained file: without this the copy would need its -wal / -shm
			// companions, which are easy to leave behind when the file is carried elsewhere.
			using var cmd = copy.CreateCommand();
			cmd.CommandText = "PRAGMA journal_mode=DELETE;";
			cmd.ExecuteNonQuery();
		}

		// The name starts with the date, so ordering by name is ordering by time.
		foreach (var old in Directory.GetFiles(AppPaths.BackupsDir, "vardiologio_*.db")
			         .OrderByDescending(f => Path.GetFileName(f), StringComparer.Ordinal)
			         .Skip(KeptBackups))
			DeleteQuietly(old);

		return path;
	}

	/// <summary>
	/// Decides whether a (private copy of a) file is a database of this application with the
	/// same schema: it must be a SQLite file whose applied migrations are exactly this version's.
	/// </summary>
	private static DatabaseFileCheck Check(string path, HashSet<string> known)
	{
		try
		{
			using var conn = Open(path);
			using var cmd = conn.CreateCommand();
			cmd.CommandText = "SELECT MigrationId FROM __EFMigrationsHistory;";

			var applied = new HashSet<string>();
			using (var reader = cmd.ExecuteReader())
				while (reader.Read()) applied.Add(reader.GetString(0));

			return applied.SetEquals(known) ? DatabaseFileCheck.Ok : DatabaseFileCheck.DifferentSchema;
		}
		catch (SqliteException)
		{
			// Not a SQLite file, or a SQLite file without EF's migrations table.
			return DatabaseFileCheck.NotDatabase;
		}
	}

	/// <summary>Replaces the Users rows of a database file with the given ones (same keys).</summary>
	private static void ReplaceUsers(string path, List<User> users)
	{
		var options = new DbContextOptionsBuilder<AppDbContext>()
			.UseSqlite(ConnectionString(path))
			.Options;

		using var db = new AppDbContext(options);
		db.Users.ExecuteDelete();
		db.Users.AddRange(users);   // Id is set, so each row keeps its key
		db.SaveChanges();
	}

	/// <summary>Copies the chosen file next to the live database, under a temporary name.</summary>
	private static string CopyToTemp(string path)
	{
		var temp = Path.Combine(AppPaths.BackupsDir, $"import_{Guid.NewGuid():N}.tmp");
		File.Copy(path, temp, overwrite: true);
		return temp;
	}

	/// <summary>Opens a SQLite file outside the connection pool, so closing it releases the file.</summary>
	private static SqliteConnection Open(string path)
	{
		var conn = new SqliteConnection(ConnectionString(path));
		conn.Open();
		return conn;
	}

	private static string ConnectionString(string path) =>
		new SqliteConnectionStringBuilder { DataSource = path, Pooling = false }.ToString();

	/// <summary>Deletes a file and its SQLite companions; a leftover file is harmless, so failures are ignored.</summary>
	private static void DeleteQuietly(string path)
	{
		foreach (var file in new[] { path, path + "-wal", path + "-shm", path + "-journal" })
		{
			try { File.Delete(file); }
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
		}
	}
}
