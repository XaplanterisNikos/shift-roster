namespace Vardiologio.Domain.Enums;

/// <summary>
/// Official form whose fixed texts (letterhead, titles, signatures…) are editable by the user.
/// The three attendance reports are separate forms: they have different titles and signatures.
/// </summary>
public enum ReportForm
{
	/// <summary>Πίνακας βαρδιών εργασίας προσωπικού (A3).</summary>
	ShiftTable = 1,

	/// <summary>Πίνακας παρουσιών προσωπικού: permanent and open-ended staff (Μόνιμοι, ΙΔΑΧ).</summary>
	AttendanceStaff = 2,

	/// <summary>Πίνακας παρουσιών of the head of the directorate.</summary>
	AttendanceDirectorate = 3,

	/// <summary>Ατομική βεβαίωση (πιστοποίηση υπερωριακής και λοιπής εργασίας).</summary>
	OvertimeCertificate = 4,

	/// <summary>Συνολική μηνιαία Σ.Ω. (monthly roster; same header / footer layout as the shift table).</summary>
	MonthlyTotal = 5,

	/// <summary>
	/// Συγκεντρωτική πιστοποίηση υπερωριακής εργασίας ανά σύμβαση (ΙΔΟΧ / ΙΔΑΧ / Μόνιμοι):
	/// one row per employee, grouped by contract. Both printed forms share these texts.
	/// </summary>
	ContractCertificate = 6,

	/// <summary>Πίνακας παρουσιών προσωπικού Ι.Δ. ορισμένου χρόνου (ΙΔΟΧ).</summary>
	AttendanceFixedTerm = 7,

	/// <summary>
	/// Ατομική μηνιαία ("ΑΝΑΛΥΤΙΚΟΣ ΗΜΕΡΗΣΙΟΣ ΠΙΝΑΚΑΣ ΠΑΡΟΥΣΙΩΝ ΚΑΙ ΩΡΑΡΙΩΝ") as exported in its
	/// official form; the on-screen preview of the page does not use these texts.
	/// Stored by name ("IndividualMonthly"), so adding it needs no schema change.
	/// </summary>
	IndividualMonthly = 8
}
