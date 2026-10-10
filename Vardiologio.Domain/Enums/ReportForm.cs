namespace Vardiologio.Domain.Enums;

/// <summary>
/// Official form whose fixed texts (letterhead, titles, signatures…) are editable by the user.
/// The two attendance reports are separate forms: they have different titles and signatures.
/// </summary>
public enum ReportForm
{
	/// <summary>Πίνακας βαρδιών εργασίας προσωπικού (A3).</summary>
	ShiftTable = 1,

	/// <summary>Πίνακας παρουσιών προσωπικού (every employee except the directorate).</summary>
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
	ContractCertificate = 6
}
