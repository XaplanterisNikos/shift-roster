namespace Vardiologio.Domain.Entities;

/// <summary>
/// One "life" of the data on this installation. The first row is the original database; every
/// database import starts a new one, named after the imported file. The activity log is shown
/// per epoch (one tab each), so actions on older data stay apart from actions on newer data.
/// </summary>
public class DatabaseEpoch
{
	/// <summary>Primary key; the highest one is the current epoch.</summary>
	public int Id { get; set; }

	/// <summary>Name shown on the tab, e.g. "Αρχική βάση" or the imported file's name.</summary>
	public string Name { get; set; } = null!;

	/// <summary>When the epoch started (local time).</summary>
	public DateTime StartedAt { get; set; }
}
