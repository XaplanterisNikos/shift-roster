namespace Vardiologio.Application.Activity;

/// <summary>A tab of the activity log: one epoch of the data (see DatabaseEpoch).</summary>
public record ActivityEpochItem(int Id, string Name, DateTime StartedAt);

/// <summary>One line of the activity log, as listed.</summary>
/// <param name="ChangeCount">Number of field changes behind the line (0 for exports and imports).</param>
public record ActivityEntryItem(
	int Id, DateTime Timestamp, string Username, string Action, string Subject, string Details, int ChangeCount);

/// <summary>One field change of a line.</summary>
public record ActivityChangeItem(string RowKey, string Kind, string Field, string? OldValue, string? NewValue);

/// <summary>
/// The activity log: who saved, exported or imported what, and when. Saves are recorded
/// automatically for every table (see ActivityInterceptor); other actions call
/// <see cref="RecordAsync"/>. Read by the administrator's pages.
/// </summary>
public interface IActivityLog
{
	/// <summary>Most lines one query returns (the newest ones).</summary>
	const int MaxEntries = 2000;

	/// <summary>
	/// Adds a line for an action that is not a database save (export, import, archive).
	/// Never throws: a failing log must not fail the action it describes.
	/// </summary>
	/// <param name="action">Kind of action, e.g. "Εξαγωγή αναφοράς".</param>
	/// <param name="subject">What it was done to.</param>
	/// <param name="details">Short description, e.g. the file name.</param>
	Task RecordAsync(string action, string subject, string details);

	/// <summary>The epochs, oldest first (tab order).</summary>
	Task<IReadOnlyList<ActivityEpochItem>> GetEpochsAsync();

	/// <summary>The user names that appear in an epoch, sorted.</summary>
	Task<IReadOnlyList<string>> GetUsernamesAsync(int epochId);

	/// <summary>The lines of an epoch, newest first, at most <see cref="MaxEntries"/>.</summary>
	/// <param name="epochId">The epoch (tab).</param>
	/// <param name="username">Only this user's lines; null = everybody.</param>
	/// <param name="from">First day included; null = no lower bound.</param>
	/// <param name="to">Last day included; null = no upper bound.</param>
	Task<IReadOnlyList<ActivityEntryItem>> GetEntriesAsync(int epochId, string? username, DateTime? from, DateTime? to);

	/// <summary>The field changes of one line, in the order they were written.</summary>
	Task<IReadOnlyList<ActivityChangeItem>> GetChangesAsync(int entryId);
}
