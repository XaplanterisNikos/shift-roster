namespace Vardiologio.Domain.Entities;

/// <summary>A staff member included in the shift roster.</summary>
public class Employee
{
	/// <summary>Primary key.</summary>
	public int Id { get; set; }

	/// <summary>Last name.</summary>
	public string LastName { get; set; } = null!;

	/// <summary>First name, abbreviated in the source data.</summary>
	public string FirstName { get; set; } = null!;

	/// <summary>Foreign key to the employee's specialty.</summary>
	public int SpecialityId { get; set; }

	/// <summary>Navigation property for the employee's specialty.</summary>
	public Speciality Speciality { get; set; } = null!;

	/// <summary>
	/// Indicates whether the employee is active. Inactive employees are excluded
	/// from lists and reports but retained in the database for historical records.
	/// Defaults to <c>true</c>.
	/// </summary>
	public bool IsActive { get; set; } = true;

	/// <summary>
	/// Foreign key to the employment type. Nullable until the relevant data is available.
	/// </summary>
	public int? EmploymentTypeId { get; set; }

	/// <summary>Navigation property for the employee's employment type.</summary>
	public EmploymentType? EmploymentType { get; set; }

	/// <summary>
	/// Foreign key to the work position. Nullable until the relevant data is available.
	/// </summary>
	public int? WorkPositionId { get; set; }

	/// <summary>Navigation property for the employee's work position.</summary>
	public WorkPosition? WorkPosition { get; set; }

	/// <summary>
	/// The supervisor who signs this employee's individual monthly sheet (a <see cref="Signatory"/>
	/// id); null until one is chosen. A plain column on purpose, without a database foreign key:
	/// adding it is a simple ALTER TABLE that never rebuilds the Employees table, and signatories
	/// are only ever soft-deleted, so the id always resolves.
	/// </summary>
	public int? SignatoryId { get; set; }

	/// <summary>
	/// Where the employee is placed ("Τοποθέτηση υπαλλήλου", a <see cref="Placement"/> id); null
	/// until one is chosen. Optional, and a plain column on purpose like <see cref="SignatoryId"/>:
	/// no navigation and no database foreign key, so adding it is a simple ALTER TABLE that never
	/// rebuilds the Employees table. Placements are only ever soft-deleted, so the id always resolves.
	/// </summary>
	public int? PlacementId { get; set; }

	/// <summary>All daily shift entries associated with this employee.</summary>
	public ICollection<ShiftDay> ShiftDays { get; set; } = new List<ShiftDay>();
}