namespace Vardiologio.Domain.Enums;

/// <summary>
/// Official form whose fixed texts (letterhead, titles, signatures…) are editable by the user.
/// The three attendance reports are separate forms: they have different titles and signatures.
/// </summary>
public enum ReportForm
{
	/// <summary>Official shift table (A3).</summary>
	ShiftTable = 1,

	/// <summary>Staff attendance: permanent and open-ended staff.</summary>
	AttendanceStaff = 2,

	/// <summary>Attendance of the head of the directorate.</summary>
	AttendanceDirectorate = 3,

	/// <summary>Individual overtime certificate.</summary>
	OvertimeCertificate = 4,

	/// <summary>Monthly roster (same header / footer layout as the shift table).</summary>
	MonthlyTotal = 5,

	/// <summary>
	/// Collective overtime certificate per contract: one row per employee, grouped by contract
	/// (permanent, open-ended, fixed-term) on one printed form.
	/// </summary>
	ContractCertificate = 6,

	/// <summary>Staff attendance: fixed-term staff.</summary>
	AttendanceFixedTerm = 7,

	/// <summary>
	/// Individual monthly sheet as exported in its
	/// official form; the on-screen preview of the page does not use these texts.
	/// Stored by name ("IndividualMonthly"), so adding it needs no schema change.
	/// </summary>
	IndividualMonthly = 8
}
