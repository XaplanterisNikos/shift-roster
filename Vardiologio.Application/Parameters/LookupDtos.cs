using System.ComponentModel.DataAnnotations;

namespace Vardiologio.Application.Parameters;

/// <summary>Which employee lookup table a parameters call targets.</summary>
public enum LookupKind
{
	/// <summary>Speciality.</summary>
	Speciality = 1,

	/// <summary>Contract (EmploymentType); the only kind that also has a short code.</summary>
	EmploymentType = 2,

	/// <summary>Work position.</summary>
	WorkPosition = 3,

	/// <summary>Placement of the employee.</summary>
	Placement = 4
}

/// <summary>
/// Editable state of one lookup row, shared by all kinds.
/// Code is used only for <see cref="LookupKind.EmploymentType"/> and is null for the others;
/// its "required" rule depends on the kind, so the UI checks it before saving.
/// </summary>
public class LookupItem
{
	/// <summary>Primary key; 0 = new (not yet persisted).</summary>
	public int Id { get; set; }

	/// <summary>Short code shown in the roster grid (EmploymentType only). Mirrors HasMaxLength(10).</summary>
	[StringLength(10, ErrorMessage = "Ο κωδικός μπορεί να έχει έως 10 χαρακτήρες.")]
	public string? Code { get; set; }

	/// <summary>
	/// Display name. 60 = the smallest DB limit of the tables
	/// (EmploymentType/WorkPosition/Placement are 60, Speciality is 100), so one rule fits all.
	/// </summary>
	[Required(ErrorMessage = "Το όνομα είναι υποχρεωτικό.")]
	[StringLength(60, ErrorMessage = "Το όνομα μπορεί να έχει έως 60 χαρακτήρες.")]
	public string Name { get; set; } = "";

	/// <summary>False = soft-deleted (hidden from new picks, kept everywhere else).</summary>
	public bool IsActive { get; set; } = true;
}
