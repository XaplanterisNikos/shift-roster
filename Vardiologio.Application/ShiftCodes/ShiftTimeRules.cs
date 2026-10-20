using System.Globalization;
using Vardiologio.Domain.Enums;

namespace Vardiologio.Application.ShiftCodes;

/// <summary>
/// Pure rules for a shift code's start/end times: 24h parsing, duration (shifts may cross
/// midnight), validation, and the Day/Night suggestion. No EF, no UI — easy to test and reuse.
/// </summary>
public static class ShiftTimeRules
{
	/// <summary>Shortest allowed shift.</summary>
	public static readonly TimeSpan MinDuration = TimeSpan.FromHours(1);

	/// <summary>Longest allowed shift (the longest seeded code is 10h).</summary>
	public static readonly TimeSpan MaxDuration = TimeSpan.FromHours(12);

	/// <summary>Night window start (22:00), per the labour-law definition of night work.</summary>
	public static readonly TimeOnly NightStart = new(22, 0);

	/// <summary>Night window end (06:00 of the next day).</summary>
	public static readonly TimeOnly NightEnd = new(6, 0);

	// Accepted input formats: "7:30" and "07:30". Always 24h, never AM/PM.
	private static readonly string[] InputFormats = { "H:mm", "HH:mm" };

	/// <summary>
	/// Parses a 24h "HH:mm" text. Empty/whitespace is a valid "no time" (status codes).
	/// Returns false only for non-empty text that is not a valid 24h time.
	/// </summary>
	public static bool TryParse(string? text, out TimeOnly? time)
	{
		time = null;
		if (string.IsNullOrWhiteSpace(text)) return true;   // "no time" is allowed

		if (TimeOnly.TryParseExact(text.Trim(), InputFormats, CultureInfo.InvariantCulture,
				DateTimeStyles.None, out var parsed))
		{
			time = parsed;
			return true;
		}
		return false;
	}

	/// <summary>Formats a time for the 24h input ("" when null).</summary>
	public static string Format(TimeOnly? time) =>
		time?.ToString("HH:mm", CultureInfo.InvariantCulture) ?? "";

	/// <summary>
	/// Shift length. If end is not after start, the shift ends on the next day
	/// (22:00 → 06:00 = 8h). Equal times mean a full 24h (rejected by validation).
	/// </summary>
	public static TimeSpan Duration(TimeOnly start, TimeOnly end)
	{
		var d = end.ToTimeSpan() - start.ToTimeSpan();
		return d <= TimeSpan.Zero ? d + TimeSpan.FromDays(1) : d;
	}

	/// <summary>
	/// How much of the shift falls inside 22:00–06:00. Works on a minute timeline
	/// starting at 00:00 of the start day; a shift is at most 24h, so the night windows
	/// [00:00–06:00), [22:00–06:00 next day) and [22:00 next day–…) cover every case.
	/// </summary>
	public static TimeSpan NightOverlap(TimeOnly start, TimeOnly end)
	{
		double s = start.ToTimeSpan().TotalMinutes;
		double e = s + Duration(start, end).TotalMinutes;

		double nightStart = NightStart.ToTimeSpan().TotalMinutes;   // 1320
		double nightEnd = NightEnd.ToTimeSpan().TotalMinutes;       // 360
		const double day = 24 * 60;

		// Night windows on the two-day timeline (start day + next day).
		var windows = new (double From, double To)[]
		{
			(0, nightEnd),                              // 00:00–06:00 start day
			(nightStart, day + nightEnd),               // 22:00–06:00 next day
			(day + nightStart, 2 * day),                // 22:00–24:00 next day
		};

		double minutes = 0;
		foreach (var (from, to) in windows)
			minutes += Math.Max(0, Math.Min(e, to) - Math.Max(s, from));

		return TimeSpan.FromMinutes(minutes);
	}

	/// <summary>Night if at least half of the shift is inside 22:00–06:00, otherwise Day.</summary>
	public static ShiftSegment SuggestSegment(TimeOnly start, TimeOnly end) =>
		NightOverlap(start, end).Ticks * 2 >= Duration(start, end).Ticks
			? ShiftSegment.Night
			: ShiftSegment.Day;

	/// <summary>
	/// Validates a start/end pair. Returns a Greek error message, or null when valid.
	/// Both empty is valid (status codes like Ρ/Α/Κ have no hours).
	/// </summary>
	public static string? Validate(TimeOnly? start, TimeOnly? end)
	{
		if (start is null && end is null) return null;

		if (start is null || end is null)
			return "Συμπλήρωσε και ώρα έναρξης και ώρα λήξης (ή άφησέ τες και τις δύο κενές).";

		var d = Duration(start.Value, end.Value);
		if (d < MinDuration || d > MaxDuration)
			return $"Η βάρδια διαρκεί {FormatHours(d)}. Επιτρέπεται από {MinDuration.TotalHours:0} έως {MaxDuration.TotalHours:0} ώρες " +
			       "(αν τελειώνει την επόμενη μέρα, η λήξη μπαίνει μικρότερη από την έναρξη, π.χ. 22:00 – 06:00).";

		return null;
	}

	/// <summary>"8ω" / "6ω 30λ" / "30λ" for messages and hints.</summary>
	public static string FormatHours(TimeSpan d)
	{
		var hours = (int)d.TotalHours;
		if (d.Minutes == 0) return $"{hours}ω";
		return hours == 0 ? $"{d.Minutes}λ" : $"{hours}ω {d.Minutes}λ";
	}
}
