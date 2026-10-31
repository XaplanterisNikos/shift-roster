using Microsoft.EntityFrameworkCore;
using Vardiologio.Application.ReportColors;
using Vardiologio.Infrastructure.Persistence;

namespace Vardiologio.Infrastructure.ReportColors;

/// <summary>EF Core implementation of <see cref="IReportColorService"/>.</summary>
public class ReportColorService : IReportColorService
{
	private readonly IDbContextFactory<AppDbContext> _factory;
	public ReportColorService(IDbContextFactory<AppDbContext> factory) => _factory = factory;

	/// <inheritdoc/>
	public async Task<List<ReportColorItem>> GetAsync()
	{
		await using var db = await _factory.CreateDbContextAsync();

		var items = await db.ReportColors
			.AsNoTracking()
			.Select(c => new ReportColorItem { Id = c.Id, Key = c.Key, Hex = c.Hex })
			.ToListAsync();

		var order = ReportColorCatalog.All
			.Select((entry, index) => (entry.Key, index))
			.ToDictionary(x => x.Key, x => x.index);
		return items.OrderBy(i => order.TryGetValue(i.Key, out var index) ? index : int.MaxValue).ToList();
	}

	/// <inheritdoc/>
	public async Task SaveAsync(IReadOnlyList<ReportColorItem> colors)
	{
		await using var db = await _factory.CreateDbContextAsync();

		var rows = await db.ReportColors.ToListAsync();

		var newHexById = colors
			.Where(c => ReportColorCatalog.IsHex(c.Hex))
			.ToDictionary(c => c.Id, c => c.Hex.ToUpperInvariant());
		string NewHex(int id, string current) => newHexById.TryGetValue(id, out var hex) ? hex : current;

		if (rows.All(r => NewHex(r.Id, r.Hex) == r.Hex)) return;

		foreach (var row in rows)
		{
			var hex = NewHex(row.Id, row.Hex);
			row.PreviousHex = row.Hex;
			row.Hex = hex;
		}
		await db.SaveChangesAsync();
	}

	/// <inheritdoc/>
	public async Task<bool> CanUndoAsync()
	{
		await using var db = await _factory.CreateDbContextAsync();
		return await db.ReportColors.AnyAsync(c => c.PreviousHex != null);
	}

	/// <inheritdoc/>
	public async Task UndoLastSaveAsync()
	{
		await using var db = await _factory.CreateDbContextAsync();

		var rows = await db.ReportColors.Where(c => c.PreviousHex != null).ToListAsync();
		foreach (var row in rows)
			(row.Hex, row.PreviousHex) = (row.PreviousHex!, row.Hex);
		await db.SaveChangesAsync();
	}

	/// <summary>
	/// The colours as the renderers read them. Used by the report services, which already
	/// have an open context when they build their model.
	/// </summary>
	internal static async Task<ReportColorSet> LoadSetAsync(AppDbContext db)
	{
		var rows = await db.ReportColors
			.AsNoTracking()
			.Select(c => new { c.Key, c.Hex })
			.ToListAsync();
		return new ReportColorSet(rows.Select(r => (r.Key, r.Hex)));
	}
}
