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

	/// <summary>Employees assigned to this work position.</summary>
	public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}