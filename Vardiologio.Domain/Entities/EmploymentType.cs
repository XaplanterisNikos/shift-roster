namespace Vardiologio.Domain.Entities;

/// <summary>
/// Lookup entity for employment types, such as permanent, open-ended private-law,
/// or fixed-term private-law employment. Assigned per employee.
/// </summary>
public class EmploymentType
{
	/// <summary>Primary key.</summary>
	public int Id { get; set; }

	/// <summary>Short code displayed in the daily schedule grid.</summary>
	public string Code { get; set; } = null!;

	/// <summary>Full employment type description.</summary>
	public string Name { get; set; } = null!;

	/// <summary>Employees assigned to this employment type.</summary>
	public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}