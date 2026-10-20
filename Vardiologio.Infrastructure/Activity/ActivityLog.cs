using Microsoft.EntityFrameworkCore;
using Vardiologio.Application.Activity;
using Vardiologio.Application.Auth;
using Vardiologio.Domain.Entities;
using Vardiologio.Infrastructure.Logging;
using Vardiologio.Infrastructure.Persistence;

namespace Vardiologio.Infrastructure.Activity;

/// <summary>EF Core implementation of <see cref="IActivityLog"/>.</summary>
public class ActivityLog : IActivityLog
{
	private readonly IDbContextFactory<AppDbContext> _factory;
	private readonly IUserSession _session;

	public ActivityLog(IDbContextFactory<AppDbContext> factory, IUserSession session)
	{
		_factory = factory;
		_session = session;
	}

	/// <inheritdoc/>
	public async Task RecordAsync(string action, string subject, string details)
	{
		try
		{
			await using var db = await _factory.CreateDbContextAsync();

			db.ActivityEntries.Add(new ActivityEntry
			{
				DatabaseEpoch = ActivityEpochs.Current(db),
				Timestamp = DateTime.Now,
				Username = _session.Current?.DisplayName ?? ActivityInterceptor.NoUser,
				Action = action,
				Subject = subject,
				Details = details
			});
			await db.SaveChangesAsync();
		}
		catch (Exception ex)
		{
			// The action itself succeeded; only its log line is missing.
			ErrorLogFile.Write("Activity log", ex);
		}
	}

	/// <inheritdoc/>
	public async Task<IReadOnlyList<ActivityEpochItem>> GetEpochsAsync()
	{
		await using var db = await _factory.CreateDbContextAsync();

		return await db.DatabaseEpochs
			.OrderBy(e => e.Id)
			.Select(e => new ActivityEpochItem(e.Id, e.Name, e.StartedAt))
			.ToListAsync();
	}

	/// <inheritdoc/>
	public async Task<IReadOnlyList<string>> GetUsernamesAsync(int epochId)
	{
		await using var db = await _factory.CreateDbContextAsync();

		var names = await db.ActivityEntries
			.Where(e => e.DatabaseEpochId == epochId)
			.Select(e => e.Username)
			.Distinct()
			.ToListAsync();

		// Sorted in memory: SQLite's own ordering is not case-insensitive for Greek letters.
		return names.OrderBy(n => n, StringComparer.CurrentCultureIgnoreCase).ToList();
	}

	/// <inheritdoc/>
	public async Task<IReadOnlyList<ActivityEntryItem>> GetEntriesAsync(
		int epochId, string? username, DateTime? from, DateTime? to)
	{
		await using var db = await _factory.CreateDbContextAsync();

		var query = db.ActivityEntries.Where(e => e.DatabaseEpochId == epochId);
		if (username is not null) query = query.Where(e => e.Username == username);
		if (from is not null)
		{
			var first = from.Value.Date;
			query = query.Where(e => e.Timestamp >= first);
		}
		if (to is not null)
		{
			var afterLast = to.Value.Date.AddDays(1);   // the whole last day is included
			query = query.Where(e => e.Timestamp < afterLast);
		}

		return await query
			.OrderByDescending(e => e.Id)
			.Take(IActivityLog.MaxEntries)
			.Select(e => new ActivityEntryItem(
				e.Id, e.Timestamp, e.Username, e.Action, e.Subject, e.Details, e.Changes.Count))
			.ToListAsync();
	}

	/// <inheritdoc/>
	public async Task<IReadOnlyList<ActivityChangeItem>> GetChangesAsync(int entryId)
	{
		await using var db = await _factory.CreateDbContextAsync();

		return await db.ActivityChanges
			.Where(c => c.ActivityEntryId == entryId)
			.OrderBy(c => c.Id)
			.Select(c => new ActivityChangeItem(c.RowKey, c.Kind, c.Field, c.OldValue, c.NewValue))
			.ToListAsync();
	}
}
