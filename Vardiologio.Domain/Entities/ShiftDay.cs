namespace Vardiologio.Domain.Entities;

/// <summary>A daily roster entry that assigns a shift code to an employee.</summary>
public class ShiftDay
{
	/// <summary>Primary key.</summary>
	public int Id { get; set; }

	/// <summary>Foreign key to the employee.</summary>
	public int EmployeeId { get; set; }

	/// <summary>Navigation property for the employee assigned to this entry.</summary>
	public Employee Employee { get; set; } = null!;

	/// <summary>
	/// Date of the roster entry. The reporting month is derived from this value.
	/// </summary>
	public DateOnly Date { get; set; }

	/// <summary>Foreign key to the assigned shift or status code.</summary>
	public int ShiftCodeId { get; set; }

	/// <summary>Navigation property for the assigned shift or status code.</summary>
	public ShiftCode ShiftCode { get; set; } = null!;
}