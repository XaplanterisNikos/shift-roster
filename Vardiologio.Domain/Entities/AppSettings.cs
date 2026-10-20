namespace Vardiologio.Domain.Entities;

/// <summary>Settings of this installation set by the administrator (single-row table).</summary>
public class AppSettings
{
	/// <summary>Primary key (always one row).</summary>
	public int Id { get; set; }

	/// <summary>Most users this installation may have; null = no limit.</summary>
	public int? MaxUsers { get; set; }
}
