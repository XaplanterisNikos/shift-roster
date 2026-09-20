using System.ComponentModel.DataAnnotations;

namespace Vardiologio.Application.Employees;

/// <summary>Row shown in the employee list (per selected specialty).</summary>
public record EmployeeListItem(int Id, string FullName, bool IsActive);

/// <summary>Full editable detail of one employee (used by the edit/create form).</summary>
public class EmployeeDetail
{
	public int Id { get; set; }

	// Required surname, 3–20 chars. Allowed: letters (any language), digits, . - / space.
	[Required(ErrorMessage = "Το επώνυμο είναι υποχρεωτικό.")]
	[StringLength(20, MinimumLength = 3, ErrorMessage = "Το επώνυμο πρέπει να έχει 3–20 χαρακτήρες.")]
	[RegularExpression(@"^[\p{L}\p{N}.\-/ ]+$", ErrorMessage = "Το επώνυμο περιέχει μη έγκυρους χαρακτήρες.")]// 0 = new (not yet persisted)

	public string LastName { get; set; } = "";
	// Optional first name (often abbreviated like "Γ." or blank in the source), max 20.
	[StringLength(20, ErrorMessage = "Το όνομα δεν πρέπει να ξεπερνά τους 20 χαρακτήρες.")]
	[RegularExpression(@"^[\p{L}\p{N}.\-/ ]*$", ErrorMessage = "Το όνομα περιέχει μη έγκυρους χαρακτήρες.")]
	public string FirstName { get; set; } = "";

	// Must pick a real specialty (0 = none selected).
	[Range(1, int.MaxValue, ErrorMessage = "Επίλεξε ειδικότητα.")]
	public int SpecialityId { get; set; }

	// Must pick an employment type (— = null is invalid).
	[Required(ErrorMessage = "Επίλεξε σύμβαση.")]
	public int? EmploymentTypeId { get; set; }

	// Must pick a work position (— = null is invalid).
	[Required(ErrorMessage = "Επίλεξε θέση.")]
	public int? WorkPositionId { get; set; }

	public bool IsActive { get; set; } = true;
}

/// <summary>Generic (Id, Name) option for dropdowns.</summary>
public record LookupOption(int Id, string Name);

/// <summary>All lookup lists needed to populate the employee form dropdowns.</summary>
public record EmployeeLookups(
	IReadOnlyList<LookupOption> Specialities,
	IReadOnlyList<LookupOption> EmploymentTypes,
	IReadOnlyList<LookupOption> WorkPositions);