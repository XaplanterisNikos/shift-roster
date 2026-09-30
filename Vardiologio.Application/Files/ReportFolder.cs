namespace Vardiologio.Application.Files;

/// <summary>
/// The report a saved file belongs to. Each report has its own folder under
/// Documents\Vardiologio\Reports (Latin names only; see the file-store implementation).
/// </summary>
public enum ReportFolder
{
	/// <summary>Σ.Ω. — Συνολική μηνιαία.</summary>
	MonthlyTotal,

	/// <summary>Μηνιαία αναφορά ωρών.</summary>
	MonthlyHours,

	/// <summary>Αναφορά παρουσιών προσωπικού (staff and directorate).</summary>
	StaffAttendance,

	/// <summary>Βεβαίωση ατομική (overtime certificate).</summary>
	OvertimeCertificate,

	/// <summary>Πίνακας βαρδιών εργασίας (official shift table, A3).</summary>
	ShiftTable
}
