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

		// Sorted in memory by the catalogue order (a display rule); unknown keys go last.
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

		// New colour per stored row; a missing or malformed value keeps the stored colour.
		var newHexById = colors
			.Where(c => ReportColorCatalog.IsHex(c.Hex))
			.ToDictionary(c => c.Id, c => c.Hex.ToUpperInvariant());
		string NewHex(int id, string current) => newHexById.TryGetValue(id, out var hex) ? hex : current;

		// Nothing changed -> leave the undo state alone (an empty save must not wipe it).
		if (rows.All(r => NewHex(r.Id, r.Hex) == r.Hex)) return;

		// Snapshot every colour, changed or not, so undo restores one consistent state.
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

		// Only colours that have a previous state (colours added by a later version have none yet).
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
