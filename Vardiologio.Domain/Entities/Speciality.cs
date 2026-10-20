namespace Vardiologio.Domain.Entities;

/// <summary>Represents an employee specialty.</summary>
public class Speciality
{
	/// <summary>Primary key.</summary>
	public int Id { get; set; }

	/// <summary>Specialty name, for example "Driver".</summary>
	public string Name { get; set; } = null!;

	/// <summary>
	/// Soft-delete flag. Inactive values are hidden from new picks in the employee form
	/// but stay on employees that already have them and in all reports. No global query
	/// filter (see AppDbContext): this is the required/optional principal of Employee.
	/// </summary>
	public bool IsActive { get; set; } = true;

	/// <summary>Employees assigned to this specialty.</summary>
	public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}