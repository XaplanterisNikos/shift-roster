using System.ComponentModel.DataAnnotations;

namespace Vardiologio.Application.Parameters;

/// <summary>
/// Editable state of one supervisor who signs the individual monthly sheet on the right
/// (Παράμετροι Εργαζομένων → Υπογράφοντες). The string lengths mirror AppDbContext.
/// </summary>
public class SignatoryItem
{
	/// <summary>Primary key; 0 = new (not yet persisted).</summary>
	public int Id { get; set; }

	/// <summary>Role above the signing space, one line per row of the box (e.g. "Ο Επόπτης Λειτουργίας Σ.Μ.Α.").</summary>
	[Required(ErrorMessage = "Η ιδιότητα είναι υποχρεωτική.")]
	[StringLength(200, ErrorMessage = "Η ιδιότητα μπορεί να έχει έως 200 χαρακτήρες.")]
	public string Role { get; set; } = "";

	/// <summary>Full name under the signing space (e.g. "Ευθύμιος Καλογήρου").</summary>
	[Required(ErrorMessage = "Το ονοματεπώνυμο είναι υποχρεωτικό.")]
	[StringLength(100, ErrorMessage = "Το ονοματεπώνυμο μπορεί να έχει έως 100 χαρακτήρες.")]
	public string FullName { get; set; } = "";

	/// <summary>Speciality / grade under the name (e.g. "ΔΕ Μηχανοτεχνιτών"); may be empty.</summary>
	[StringLength(150, ErrorMessage = "Η ειδικότητα μπορεί να έχει έως 150 χαρακτήρες.")]
	public string? Title { get; set; } = "";

	/// <summary>False = soft-deleted (hidden from new picks, kept on employees that have it).</summary>
	public bool IsActive { get; set; } = true;

	/// <summary>The role split into its printed lines (blank lines dropped).</summary>
	public string[] RoleLines => SplitLines(Role);

	/// <summary>
	/// Label in lists and dropdowns: "Ευθύμιος Καλογήρου — Ο Επόπτης Λειτουργίας Σ.Μ.Α."
	/// (name and first role line), so two entries of the same person can be told apart.
	/// </summary>
	public static string Label(string fullName, string role)
	{
		var first = SplitLines(role).FirstOrDefault() ?? "";
		return first.Length == 0 ? fullName : $"{fullName} — {first}";
	}

	/// <summary>Lines of a multi-line text, trimmed, blank ones dropped ("\r\n" or "\n").</summary>
	public static string[] SplitLines(string? text) =>
		(text ?? "").Replace("\r\n", "\n")
			.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
}

/// <summary>An active employee as offered in the "who does this supervisor sign for" picker.</summary>
/// <param name="Id">Employee id.</param>
/// <param name="FullName">"Επώνυμο Όνομα".</param>
/// <param name="SignatoryId">The supervisor the employee has now (null = none).</param>
public record SignatoryEmployee(int Id, string FullName, int? SignatoryId);
