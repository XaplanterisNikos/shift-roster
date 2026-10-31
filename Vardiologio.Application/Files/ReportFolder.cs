namespace Vardiologio.Application.Files;

/// <summary>
/// The report a saved file belongs to. Each report has its own folder under
/// Documents\Vardiologio\Reports (Latin names only; see the file-store implementation).
/// </summary>
public enum ReportFolder
{
	/// <summary>Monthly roster ("Συνολική μηνιαία Σ.Ω.").</summary>
	MonthlyTotal,

	/// <summary>Monthly hours report.</summary>
	MonthlyHours,

	/// <summary>Staff attendance report (staff and directorate).</summary>
	StaffAttendance,

	/// <summary>Individual overtime certificate.</summary>
	OvertimeCertificate,

	/// <summary>Official shift table (A3).</summary>
	ShiftTable,

	/// <summary>Individual monthly sheet of one employee.</summary>
	IndividualSheet,

	/// <summary>Collective overtime certificate per contract.</summary>
	ContractCertificate
}
