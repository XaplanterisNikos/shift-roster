namespace Vardiologio.Domain.Entities;

/// <summary>
/// A supervisor who signs the individual monthly sheet ("ΑΝΑΛΥΤΙΚΟΣ ΗΜΕΡΗΣΙΟΣ ΠΙΝΑΚΑΣ
/// ΠΑΡΟΥΣΙΩΝ ΚΑΙ ΩΡΑΡΙΩΝ") on the right, e.g. "Ο Επόπτης Λειτουργίας Σ.Μ.Α." — each employee
/// is signed for by one of them (<see cref="Employee.SignatoryId"/>).
/// </summary>
public class Signatory
{
	/// <summary>Primary key.</summary>
	public int Id { get; set; }

	/// <summary>
	/// Role printed above the signing space; one or more lines separated by '\n'
	/// (e.g. "Ο Αν. Προϊστάμενος\nΔιεύθυνσης\nΣταθμών Μεταφόρτωσης").
	/// </summary>
	public string Role { get; set; } = null!;

	/// <summary>Full name printed below the signing space (e.g. "Ευθύμιος Καλογήρου").</summary>
	public string FullName { get; set; } = null!;

	/// <summary>Speciality / grade printed under the name (e.g. "ΔΕ Μηχανοτεχνιτών"); may be empty.</summary>
	public string Title { get; set; } = "";

	/// <summary>
	/// Soft-delete flag. Inactive signatories are hidden from new picks but stay on the
	/// employees that already have them. No global query filter (like the lookups).
	/// </summary>
	public bool IsActive { get; set; } = true;
}
