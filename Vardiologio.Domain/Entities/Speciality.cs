namespace Vardiologio.Domain.Entities;

/// <summary>Represents an employee specialty.</summary>
public class Speciality
{
	/// <summary>Primary key.</summary>
	public int Id { get; set; }

	/// <summary>Specialty name, for example "Driver".</summary>
	public string Name { get; set; } = null!;

	/// <summary>Employees assigned to this specialty.</summary>
	public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}