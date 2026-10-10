namespace Vardiologio.Domain.Entities;

/// <summary>One field of one row changed by a save: old and new value as text.</summary>
public class ActivityChange
{
	/// <summary>Primary key.</summary>
	public int Id { get; set; }

	/// <summary>Foreign key to the log entry (the save) this change belongs to.</summary>
	public int ActivityEntryId { get; set; }

	/// <summary>Which row: its key, or a readable description (employee and date for a roster day).</summary>
	public string RowKey { get; set; } = null!;

	/// <summary>What happened to the row: "Νέα", "Αλλαγή" or "Διαγραφή".</summary>
	public string Kind { get; set; } = null!;

	/// <summary>Name of the field.</summary>
	public string Field { get; set; } = null!;

	/// <summary>Value before the save; null for a new row or an empty value.</summary>
	public string? OldValue { get; set; }

	/// <summary>Value after the save; null for a deleted row or an empty value.</summary>
	public string? NewValue { get; set; }
}
