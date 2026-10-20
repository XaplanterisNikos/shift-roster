using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Vardiologio.Application.Auth;
using Vardiologio.Domain.Entities;
using Vardiologio.Infrastructure.Logging;
using Vardiologio.Infrastructure.Persistence;

namespace Vardiologio.Infrastructure.Activity;

/// <summary>
/// Records every database save in the activity log, in one place for all tables: just before
/// EF Core writes, it reads the pending changes and adds one <see cref="ActivityEntry"/> per
/// table (with one <see cref="ActivityChange"/> per changed field) to the same save, so the log
/// and the data are written together or not at all. A failed save is written to the error log.
/// Attached to the contexts of the DbContext factory (see App.xaml.cs); the start-up context
/// (migrations, seeding) has no interceptor, so seeding is not logged.
/// </summary>
/// <remarks>
/// Bulk statements (ExecuteDelete / ExecuteUpdate / raw SQL) do not go through SaveChanges and
/// are therefore not recorded; services that must be logged change tracked entities.
/// </remarks>
public sealed class ActivityInterceptor : SaveChangesInterceptor
{
	/// <summary>Shown as the user while nobody is signed in (e.g. a user created from the login screen).</summary>
	public const string NoUser = "(χωρίς σύνδεση)";

	/// <summary>Longest value kept; longer texts are cut.</summary>
	private const int MaxValueLength = 500;

	private readonly IUserSession _session;
	public ActivityInterceptor(IUserSession session) => _session = session;

	/// <inheritdoc/>
	public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
	{
		Record(eventData.Context);
		return base.SavingChanges(eventData, result);
	}

	/// <inheritdoc/>
	public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
		DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
	{
		Record(eventData.Context);
		return base.SavingChangesAsync(eventData, result, cancellationToken);
	}

	/// <inheritdoc/>
	public override void SaveChangesFailed(DbContextErrorEventData eventData)
	{
		ErrorLogFile.Write("Database save", eventData.Exception);
		base.SaveChangesFailed(eventData);
	}

	/// <inheritdoc/>
	public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
	{
		ErrorLogFile.Write("Database save", eventData.Exception);
		return base.SaveChangesFailedAsync(eventData, cancellationToken);
	}

	/// <summary>Adds the log entries for the context's pending changes to the context.</summary>
	private void Record(DbContext? context)
	{
		if (context is not AppDbContext db) return;

		// The interceptor runs before EF's own change detection.
		db.ChangeTracker.DetectChanges();

		// Snapshot first: the log rows added below must not be logged themselves.
		var pending = db.ChangeTracker.Entries()
			.Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
			.Where(e => e.Entity is not (ActivityEntry or ActivityChange or DatabaseEpoch))
			.ToList();
		if (pending.Count == 0) return;

		var now = DateTime.Now;
		var user = _session.Current?.DisplayName ?? NoUser;
		var roster = RosterNames.For(db, pending);
		DatabaseEpoch? epoch = null;

		foreach (var table in pending.GroupBy(e => e.Metadata.GetTableName() ?? e.Metadata.ClrType.Name))
		{
			var changes = new List<ActivityChange>();
			int added = 0, modified = 0, deleted = 0;

			foreach (var row in table)
			{
				var rowChanges = RowChanges(row, roster);
				if (rowChanges.Count == 0) continue;   // marked modified, but no value differs

				changes.AddRange(rowChanges);
				if (row.State == EntityState.Added) added++;
				else if (row.State == EntityState.Deleted) deleted++;
				else modified++;
			}
			if (changes.Count == 0) continue;

			epoch ??= ActivityEpochs.Current(db);
			db.ActivityEntries.Add(new ActivityEntry
			{
				DatabaseEpoch = epoch,
				Timestamp = now,
				Username = user,
				Action = "Αποθήκευση",
				Subject = ActivityNames.Table(table.Key),
				Details = Details(added, modified, deleted, table.Key == "ShiftDays" ? roster.Summary : null),
				Changes = changes
			});
		}
	}

	/// <summary>The field changes of one row; empty when nothing really changed.</summary>
	private static List<ActivityChange> RowChanges(EntityEntry row, RosterNames roster)
	{
		var kind = row.State switch
		{
			EntityState.Added => "Νέα",
			EntityState.Deleted => "Διαγραφή",
			_ => "Αλλαγή"
		};
		var rowKey = row.Entity is ShiftDay day ? roster.RowKey(day) : KeyOf(row);
		var result = new List<ActivityChange>();

		foreach (var p in row.Properties)
		{
			if (p.Metadata.IsPrimaryKey()) continue;   // the key is the row's name, not a change

			string? oldValue = null, newValue = null;
			if (row.State == EntityState.Added)
			{
				newValue = Text(p, p.CurrentValue, roster);
				if (newValue is null) continue;   // nothing to say about an empty field of a new row
			}
			else if (row.State == EntityState.Deleted)
			{
				oldValue = Text(p, p.OriginalValue, roster);
				if (oldValue is null) continue;
			}
			else
			{
				if (!p.IsModified || Equals(p.OriginalValue, p.CurrentValue)) continue;
				oldValue = Text(p, p.OriginalValue, roster);
				newValue = Text(p, p.CurrentValue, roster);
			}

			result.Add(new ActivityChange
			{
				RowKey = rowKey,
				Kind = kind,
				Field = ActivityNames.Field(p.Metadata.Name),
				OldValue = oldValue,
				NewValue = newValue
			});
		}
		return result;
	}

	/// <summary>The row's primary key as text; "(νέα)" while the database has not assigned it yet.</summary>
	private static string KeyOf(EntityEntry row)
	{
		var keys = row.Properties.Where(p => p.Metadata.IsPrimaryKey()).ToList();
		if (keys.Any(p => p.IsTemporary)) return "(νέα)";
		return string.Join(" / ", keys.Select(p => Convert.ToString(p.CurrentValue, CultureInfo.CurrentCulture)));
	}

	/// <summary>A value as text for the log; null for an empty value. Passwords are never written.</summary>
	private static string? Text(PropertyEntry property, object? value, RosterNames roster)
	{
		if (value is null) return null;
		var name = property.Metadata.Name;

		if (name.Contains("Password", StringComparison.Ordinal)) return "•••";

		// A roster day refers to its shift code by key; the log shows the code itself.
		if (name == nameof(ShiftDay.ShiftCodeId) && value is int codeId) return roster.Code(codeId);

		var text = value switch
		{
			bool b => b ? "Ναι" : "Όχι",
			DateOnly d => d.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
			DateTime t => t.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture),
			IFormattable f => f.ToString(null, CultureInfo.CurrentCulture),
			_ => value.ToString() ?? ""
		};
		if (text.Length == 0) return null;
		return text.Length <= MaxValueLength ? text : text[..MaxValueLength] + "…";
	}

	/// <summary>The entry's short description: row counts and, for the roster, employee and month.</summary>
	private static string Details(int added, int modified, int deleted, string? rosterSummary)
	{
		var parts = new List<string>();
		if (rosterSummary is not null) parts.Add(rosterSummary);
		if (added > 0) parts.Add($"νέες: {added}");
		if (modified > 0) parts.Add($"αλλαγές: {modified}");
		if (deleted > 0) parts.Add($"διαγραφές: {deleted}");
		return string.Join(" · ", parts);
	}

	/// <summary>
	/// Readable names for the roster days of one save: employee names and shift codes, read once.
	/// Empty when the save has no roster day.
	/// </summary>
	private sealed class RosterNames
	{
		private Dictionary<int, string> _employees = new();
		private Dictionary<int, string> _codes = new();

		/// <summary>"employee(s) · month(s)" of the roster days; null when there are none.</summary>
		public string? Summary { get; private set; }

		public static RosterNames For(AppDbContext db, List<EntityEntry> pending)
		{
			var names = new RosterNames();
			var days = pending.Select(e => e.Entity).OfType<ShiftDay>().ToList();
			if (days.Count == 0) return names;

			// Inactive employees and retired codes included: the log must name them too.
			var employeeIds = days.Select(d => d.EmployeeId).Distinct().ToList();
			// Projected (not whole entities), so nothing extra is tracked in the saving context.
			names._employees = db.Employees.IgnoreQueryFilters()
				.Where(e => employeeIds.Contains(e.Id))
				.Select(e => new { e.Id, e.LastName, e.FirstName })
				.ToDictionary(e => e.Id, e => $"{e.LastName} {e.FirstName}");
			names._codes = db.ShiftCodes
				.Select(c => new { c.Id, c.Code })
				.ToDictionary(c => c.Id, c => c.Code);

			var months = days.Select(d => d.Date.ToString("MM/yyyy", CultureInfo.InvariantCulture)).Distinct().OrderBy(m => m);
			names.Summary = $"{string.Join(", ", names._employees.Values.OrderBy(n => n))} · {string.Join(", ", months)}";
			return names;
		}

		/// <summary>"employee dd/MM/yyyy".</summary>
		public string RowKey(ShiftDay day) =>
			$"{(_employees.TryGetValue(day.EmployeeId, out var name) ? name : $"#{day.EmployeeId}")} " +
			day.Date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

		/// <summary>The shift code of a key; the key itself when the code is unknown.</summary>
		public string Code(int id) => _codes.TryGetValue(id, out var code) ? code : $"#{id}";
	}
}
