using Microsoft.EntityFrameworkCore;
using Vardiologio.Infrastructure.Persistence.Seeding;

namespace Vardiologio.Infrastructure.Persistence;

/// <summary>
/// Prepares the local SQLite database at application startup:
/// creates it if missing, applies any pending migrations, and seeds base data.
/// Runs before the DI container is built, so it constructs its own DbContext.
/// </summary>
public static class DbInitializer
{
	/// <summary>
	/// Builds a standalone DbContext, applies pending migrations, and runs the seeder.
	/// </summary>
	public static void Initialize()
	{
		var options = new DbContextOptionsBuilder<AppDbContext>()
			.UseSqlite(AppPaths.ConnectionString)
			.Options;

		using var db = new AppDbContext(options);
		db.Database.Migrate();      // create DB if needed and apply any pending migrations
		DatabaseSeeder.Seed(db);    // insert base data (idempotent: skips if already present)
	}
}