using System.ComponentModel.DataAnnotations;

namespace Vardiologio.Application.Employees;

/// <summary>Row shown in the employee list (per selected specialty).</summary>
public record EmployeeListItem(int Id, string FullName, bool IsActive);

/// <summary>Full editable detail of one employee (used by the edit/create form).</summary>
public class EmployeeDetail
{
	public int Id { get; set; }

	[Required(ErrorMessage = "Το επώνυμο είναι υποχρεωτικό.")]
	[StringLength(20, MinimumLength = 3, ErrorMessage = "Το επώνυμο πρέπει να έχει 3–20 χαρακτήρες.")]
	[RegularExpression(@"^[\p{L}\p{N}.\-/ ]+$", ErrorMessage = "Το επώνυμο περιέχει μη έγκυρους χαρακτήρες.")]

	public string LastName { get; set; } = "";
	[StringLength(20, ErrorMessage = "Το όνομα δεν πρέπει να ξεπερνά τους 20 χαρακτήρες.")]
	[RegularExpression(@"^[\p{L}\p{N}.\-/ ]*$", ErrorMessage = "Το όνομα περιέχει μη έγκυρους χαρακτήρες.")]
	public string FirstName { get; set; } = "";

	[Range(1, int.MaxValue, ErrorMessage = "Επίλεξε ειδικότητα.")]
	public int SpecialityId { get; set; }

	[Required(ErrorMessage = "Επίλεξε σύμβαση.")]
	public int? EmploymentTypeId { get; set; }

	public int? WorkPositionId { get; set; }

	public int? SignatoryId { get; set; }

	public int? PlacementId { get; set; }

	public bool IsActive { get; set; } = true;
}

/// <summary>
/// Generic (Id, Name) option for dropdowns. IsActive lets the form hide retired values
/// from new picks while still showing the value an existing employee already has.
/// </summary>
public record LookupOption(int Id, string Name, bool IsActive = true);

/// <summary>All lookup lists needed to populate the employee form dropdowns.</summary>
/// <param name="Signatories">Supervisors who sign the individual monthly sheet ("Name — role").</param>
/// <param name="Placements">Where an employee can be placed ("Τοποθέτηση υπαλλήλου").</param>
public record EmployeeLookups(
	IReadOnlyList<LookupOption> Specialities,
	IReadOnlyList<LookupOption> EmploymentTypes,
	IReadOnlyList<LookupOption> WorkPositions,
	IReadOnlyList<LookupOption> Signatories,
	IReadOnlyList<LookupOption> Placements);