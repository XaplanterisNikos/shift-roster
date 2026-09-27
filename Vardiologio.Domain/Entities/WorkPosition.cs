namespace Vardiologio.Domain.Entities;

/// <summary>
/// Lookup entity for work positions, such as Driver, Gate, or Office.
/// Assigned per employee and independent of the employee's specialty.
/// </summary>
public class WorkPosition
{
	/// <summary>Primary key.</summary>
	public int Id { get; set; }

	/// <summary>Work position name as displayed in the roster.</summary>
	public string Name { get; set; } = null!;

	/// <summary>
	/// Soft-delete flag. Inactive values are hidden from new picks in the employee form
	/// but stay on employees that already have them and in all reports. No global query
	/// filter (see AppDbContext): this is the required/optional principal of Employee.
	/// </summary>
	public bool IsActive { get; set; } = true;

	/// <summary>Employees assigned to this work position.</summary>
	public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}