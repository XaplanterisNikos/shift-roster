using Microsoft.EntityFrameworkCore;
using Vardiologio.Application.Reports;
using Vardiologio.Infrastructure.Activity;
using Vardiologio.Infrastructure.Persistence.Seeding;

namespace Vardiologio.Infrastructure.Persistence;

/// <summary>
/// Prepares the local SQLite database at application startup:
/// creates it if missing, applies any pending migrations, and seeds base data.
/// Also loads the report data file (see <see cref="ReportDataFile"/>).
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
		db.Database.Migrate();

		ActivityEpochs.Current(db);
		db.SaveChanges();

		var reportData = ReportDataFile.Load();
		PresenceRules.DirectorLastName = reportData.DirectorLastName ?? "";

		DatabaseSeeder.Seed(db, reportData);
	}
}