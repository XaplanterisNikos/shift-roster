using Microsoft.EntityFrameworkCore;
using Vardiologio.Application.ReportTexts;
using Vardiologio.Domain.Enums;
using Vardiologio.Infrastructure.Persistence;

namespace Vardiologio.Infrastructure.ReportTexts;

/// <summary>EF Core implementation of <see cref="IReportTextService"/>.</summary>
public class ReportTextService : IReportTextService
{
	private readonly IDbContextFactory<AppDbContext> _factory;
	public ReportTextService(IDbContextFactory<AppDbContext> factory) => _factory = factory;

	/// <inheritdoc/>
	public async Task<List<ReportTextLine>> GetAsync(ReportForm form)
	{
		await using var db = await _factory.CreateDbContextAsync();

		var lines = await db.ReportTexts
			.AsNoTracking()
			.Where(t => t.Form == form)
			.Select(t => new ReportTextLine
			{
				Id = t.Id,
				Section = t.Section,
				Block = t.Block,
				Line = t.Line,
				Text = t.Text
			})
			.ToListAsync();

		// Sorted in memory: the block order is a display rule, not something SQL knows about.
		return lines
			.OrderBy(l => l.Section)
			.ThenBy(l => ReportTextBlocks.Order(l.Block))
			.ThenBy(l => l.Line)
			.ToList();
	}

	/// <inheritdoc/>
	public async Task SaveAsync(ReportForm form, IReadOnlyList<ReportTextLine> lines)
	{
		await using var db = await _factory.CreateDbContextAsync();

		var rows = await db.ReportTexts.Where(t => t.Form == form).ToListAsync();

		// New text per stored line; a line the caller did not send keeps its text.
		var newTextById = lines.ToDictionary(l => l.Id, l => (l.Text ?? "").Trim());
		string NewText(int id, string current) => newTextById.TryGetValue(id, out var text) ? text : current;

		// Nothing changed -> leave the undo state alone (an empty save must not wipe it).
		if (rows.All(r => NewText(r.Id, r.Text) == r.Text)) return;

		// Snapshot the whole form, changed or not, so undo restores it as one consistent state.
		foreach (var row in rows)
		{
			var text = NewText(row.Id, row.Text);
			row.PreviousText = row.Text;
			row.Text = text;
		}
		await db.SaveChangesAsync();
	}

	/// <inheritdoc/>
	public async Task<bool> CanUndoAsync(ReportForm form)
	{
		await using var db = await _factory.CreateDbContextAsync();
		return await db.ReportTexts.AnyAsync(t => t.Form == form && t.PreviousText != null);
	}

	/// <inheritdoc/>
	public async Task UndoLastSaveAsync(ReportForm form)
	{
		await using var db = await _factory.CreateDbContextAsync();

		// Only lines that have a previous state (lines added by a later version have none yet).
		var rows = await db.ReportTexts.Where(t => t.Form == form && t.PreviousText != null).ToListAsync();
		foreach (var row in rows)
			(row.Text, row.PreviousText) = (row.PreviousText!, row.Text);
		await db.SaveChangesAsync();
	}

	/// <summary>
	/// The texts of a form as the renderers read them. Used by the report services, which
	/// already have an open context when they build their model.
	/// </summary>
	internal static async Task<ReportTextSet> LoadSetAsync(AppDbContext db, ReportForm form)
	{
		var rows = await db.ReportTexts
			.AsNoTracking()
			.Where(t => t.Form == form)
			.Select(t => new { t.Block, t.Line, t.Text })
			.ToListAsync();
		return new ReportTextSet(rows.Select(r => (r.Block, r.Line, r.Text)));
	}
}
