namespace Vardiologio.Application.Hours;

/// <summary>
/// Suggests the Greek public holidays of a year: the fixed ones plus the movable ones
/// derived from Orthodox Easter. Only a suggestion — the user confirms or edits the list
/// (e.g. when Labour Day is moved because it falls in the Easter period).
/// </summary>
public static class HolidayCalendar
{
	/// <summary>First year the Julian→Gregorian offset of 13 days is valid for.</summary>
	public const int MinYear = 1900;

	/// <summary>Last year the Julian→Gregorian offset of 13 days is valid for.</summary>
	public const int MaxYear = 2099;

	/// <summary>
	/// Orthodox Easter Sunday (Gregorian date). Meeus' Julian algorithm, then +13 days
	/// to convert from the Julian to the Gregorian calendar (valid 1900–2099).
	/// </summary>
	/// <exception cref="ArgumentOutOfRangeException">Year outside 1900–2099.</exception>
	public static DateOnly OrthodoxEaster(int year)
	{
		if (year < MinYear || year > MaxYear)
			throw new ArgumentOutOfRangeException(nameof(year), year, $"Supported years: {MinYear}–{MaxYear}.");

		// Meeus' Julian algorithm: a, b, c are the year's position in the 4-, 7- and 19-year cycles.
		int a = year % 4;
		int b = year % 7;
		int c = year % 19;
		int d = (19 * c + 15) % 30;               // days from 21 March to the Paschal full moon
		int e = (2 * a + 4 * b - d + 34) % 7;     // days from that full moon to the next Sunday
		int month = (d + e + 114) / 31;           // 3 = March, 4 = April (Julian calendar)
		int day = (d + e + 114) % 31 + 1;

		// Julian date -> Gregorian date (+13 days in 1900–2099).
		return new DateOnly(year, month, day).AddDays(13);
	}

	/// <summary>
	/// The suggested holidays of a year, ordered by date. If two holidays land on the same
	/// date (e.g. Labour Day on Easter Sunday) they are merged into one entry with both names,
	/// so the list can be stored with a unique date.
	/// </summary>
	public static IReadOnlyList<(DateOnly Date, string Name)> Suggest(int year)
	{
		var easter = OrthodoxEaster(year);

		var all = new List<(DateOnly Date, string Name)>
		{
			// Fixed holidays.
			(new DateOnly(year, 1, 1), "Πρωτοχρονιά"),
			(new DateOnly(year, 1, 6), "Θεοφάνεια"),
			(new DateOnly(year, 3, 25), "25η Μαρτίου"),
			(new DateOnly(year, 5, 1), "Εργατική Πρωτομαγιά"),
			(new DateOnly(year, 8, 15), "Κοίμηση της Θεοτόκου"),
			(new DateOnly(year, 10, 28), "28η Οκτωβρίου"),
			(new DateOnly(year, 12, 25), "Χριστούγεννα"),
			(new DateOnly(year, 12, 26), "2η ημέρα των Χριστουγέννων"),

			// Movable holidays, relative to Orthodox Easter Sunday.
			(easter.AddDays(-48), "Καθαρά Δευτέρα"),
			(easter.AddDays(-2), "Μεγάλη Παρασκευή"),
			(easter, "Κυριακή του Πάσχα"),
			(easter.AddDays(1), "Δευτέρα του Πάσχα"),
			(easter.AddDays(50), "Αγίου Πνεύματος"),
		};

		// Merge same-date entries (names joined) and order by date.
		return all
			.GroupBy(h => h.Date)
			.OrderBy(g => g.Key)
			.Select(g => (g.Key, string.Join(" / ", g.Select(h => h.Name))))
			.ToList();
	}
}
