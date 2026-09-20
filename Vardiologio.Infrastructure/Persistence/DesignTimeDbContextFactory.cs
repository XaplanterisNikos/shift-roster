using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Vardiologio.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used ONLY by the EF Core tools (e.g. "dotnet ef migrations add",
/// "dotnet ef database update"). Those tools need to build an AppDbContext without
/// running the WPF app (where the context is normally configured via DI at startup),
/// so EF discovers and calls this factory instead.
/// Not used at runtime.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
	/// <summary>
	/// Builds an AppDbContext pointed at the same local SQLite database, so migrations
	/// are generated/applied against the real schema.
	/// </summary>
	/// <param name="args">Arguments passed by the EF tooling (unused here).</param>
	public AppDbContext CreateDbContext(string[] args)
	{
		var options = new DbContextOptionsBuilder<AppDbContext>()
			.UseSqlite(AppPaths.ConnectionString)
			.Options;

		return new AppDbContext(options);
	}
}