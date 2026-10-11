namespace Vardiologio.Infrastructure.Activity;

/// <summary>Greek names of tables and fields, as shown in the activity log.</summary>
internal static class ActivityNames
{
	/// <summary>Name of a table; the table's own name when it has no entry here.</summary>
	public static string Table(string table) => table switch
	{
		"Employees" => "Εργαζόμενοι",
		"ShiftDays" => "Προγράμματα",
		"ShiftCodes" => "Κωδικοί βαρδιών",
		"ShiftCodeHours" => "Ώρες κωδικών βαρδιών",
		"Specialities" => "Ειδικότητες",
		"EmploymentTypes" => "Είδη εργασίας",
		"WorkPositions" => "Θέσεις εργασίας",
		"Holidays" => "Αργίες",
		"HourLimits" => "Όρια ωρών",
		"ReportTexts" => "Κείμενα αναφορών",
		"ReportColors" => "Χρώματα αναφορών",
		"Users" => "Χρήστες",
		"AppSettings" => "Ρυθμίσεις διαχειριστή",
		_ => table
	};

	/// <summary>Name of a field; the property's own name when it has no entry here.</summary>
	public static string Field(string property) => property switch
	{
		"ShiftCodeId" => "Κωδικός βάρδιας",
		"EmployeeId" => "Εργαζόμενος (κλειδί)",
		"Date" => "Ημερομηνία",
		"LastName" => "Επώνυμο",
		"FirstName" => "Όνομα",
		"IsActive" => "Ενεργός",
		"Username" => "Όνομα χρήστη",
		"FullName" => "Ονοματεπώνυμο",
		"Email" => "Email",
		"PasswordSalt" or "PasswordHash" => "Κωδικός πρόσβασης",
		"MaxUsers" => "Όριο χρηστών",
		_ => property
	};
}
