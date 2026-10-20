namespace Vardiologio.Domain.Entities;

/// <summary>
/// One line of the activity log: who did what, and when. A save writes one entry per table it
/// touched (with the field changes in <see cref="Changes"/>); an export or import writes one
/// entry without changes.
/// </summary>
public class ActivityEntry
{
	/// <summary>Primary key.</summary>
	public int Id { get; set; }

	/// <summary>Foreign key to the epoch the action belongs to.</summary>
	public int DatabaseEpochId { get; set; }

	/// <summary>Navigation property for the epoch.</summary>
	public DatabaseEpoch DatabaseEpoch { get; set; } = null!;

	/// <summary>When it happened (local time).</summary>
	public DateTime Timestamp { get; set; }

	/// <summary>
	/// Name of the signed-in user, kept as text (not a key) so the line survives the user's deletion.
	/// </summary>
	public string Username { get; set; } = null!;

	/// <summary>Kind of action, e.g. "Αποθήκευση", "Εξαγωγή αναφοράς".</summary>
	public string Action { get; set; } = null!;

	/// <summary>What it was done to: the table (in Greek) for a save, the kind of file for an export.</summary>
	public string Subject { get; set; } = null!;

	/// <summary>Short description: row counts, employee and month, file name.</summary>
	public string Details { get; set; } = null!;

	/// <summary>Field changes of a save; empty for exports and imports.</summary>
	public ICollection<ActivityChange> Changes { get; set; } = new List<ActivityChange>();
}
