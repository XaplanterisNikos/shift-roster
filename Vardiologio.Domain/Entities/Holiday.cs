namespace Vardiologio.Domain.Entities;

/// <summary>
/// A public holiday (αργία). Days in this list count as <see cref="Enums.DayType.Holiday"/>,
/// even when they fall on a Saturday or Sunday, and each holiday in a month adds to that
/// month's holiday-hours limit. Maintained per year by the user.
/// No other table references it, so it can be hard-deleted (no IsActive flag).
/// </summary>
public class Holiday
{
	/// <summary>Primary key.</summary>
	public int Id { get; set; }

	/// <summary>The holiday date (unique).</summary>
	public DateOnly Date { get; set; }

	/// <summary>Display name, e.g. "Μεγάλη Παρασκευή".</summary>
	public string Name { get; set; } = null!;
}
