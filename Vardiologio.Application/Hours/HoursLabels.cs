using Vardiologio.Domain.Enums;

namespace Vardiologio.Application.Hours;

/// <summary>Greek display labels for the hours enums, shared by the settings screens and reports.</summary>
public static class HoursLabels
{
	/// <summary>The single kinds of day, in display order (Weekday, Saturday, Sunday, Holiday).</summary>
	public static readonly DayType[] DayTypes =
		{ DayType.Weekday, DayType.Saturday, DayType.Sunday, DayType.Holiday };

	/// <summary>Label of a single kind of day.</summary>
	public static string Day(DayType day) => day switch
	{
		DayType.Weekday => "Καθημερινή",
		DayType.Saturday => "Σάββατο",
		DayType.Sunday => "Κυριακή",
		DayType.Holiday => "Αργία",
		_ => day.ToString()
	};

	/// <summary>Label of a pay category.</summary>
	public static string Category(HourCategory category) => category switch
	{
		HourCategory.ToComplete => "Προς συμπλήρωση",
		HourCategory.Simple => "Απλή",
		HourCategory.Night => "Νυχτερινή",
		HourCategory.Sunday => "Κυριακής",
		HourCategory.Holiday => "Αργίας",
		_ => category.ToString()
	};

	/// <summary>
	/// Upper-case label of a pay category for report headers, without accents
	/// (Greek capitals are written unaccented, e.g. "ΠΡΟΣ ΣΥΜΠΛΗΡΩΣΗ").
	/// </summary>
	public static string CategoryHeader(HourCategory category) => category switch
	{
		HourCategory.ToComplete => "ΠΡΟΣ ΣΥΜΠΛΗΡΩΣΗ",
		HourCategory.Simple => "ΑΠΛΗ",
		HourCategory.Night => "ΝΥΧΤΕΡΙΝΗ",
		HourCategory.Sunday => "ΚΥΡΙΑΚΗΣ",
		HourCategory.Holiday => "ΑΡΓΙΑΣ",
		_ => category.ToString().ToUpperInvariant()
	};
}
