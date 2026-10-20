namespace Vardiologio.Domain.Entities;

/// <summary>
/// Lookup entity for where an employee is placed ("Τοποθέτηση υπαλλήλου"), such as
/// "ΔΙΕΥΘΥΝΣΗ ΣΜΑ" or "ΤΜΗΜΑ Κ. ΣΜΑ". Assigned per employee (<see cref="Employee.PlacementId"/>);
/// for now it is only kept on the employee and is not read by any report.
/// </summary>
public class Placement
{
	/// <summary>Primary key.</summary>
	public int Id { get; set; }

	/// <summary>Placement name as shown in the employee form.</summary>
	public string Name { get; set; } = null!;

	/// <summary>
	/// Soft-delete flag. Inactive values are hidden from new picks in the employee form
	/// but stay on employees that already have them. No global query filter (like the
	/// other employee lookups, see AppDbContext).
	/// </summary>
	public bool IsActive { get; set; } = true;
}
