namespace Vardiologio.Domain.Entities;

/// <summary>
/// Represents additional hours credited to a specific shift code.
/// Has a one-to-one relationship with <see cref="ShiftCode"/> through a shared primary key.
/// Only shift codes with additional hours have a corresponding record.
/// </summary>
public class ShiftExtraHours
{
	/// <summary>
	/// Primary key and foreign key for the related <see cref="ShiftCode"/>.
	/// </summary>
	public int ShiftCodeId { get; set; }

	/// <summary>Navigation property for the related shift code.</summary>
	public ShiftCode ShiftCode { get; set; } = null!;

	/// <summary>Number of additional hours credited for this shift code.</summary>
	public decimal Hours { get; set; }
}