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

	/// <summary>
	/// Soft-delete flag. Inactive values are hidden from new picks in the employee form
	/// but stay on employees that already have them and in all reports. No global query
	/// filter (see AppDbContext): this is the required/optional principal of Employee.
	/// </summary>
	public bool IsActive { get; set; } = true;

	/// <summary>Employees assigned to this employment type.</summary>
	public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}