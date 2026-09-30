namespace Vardiologio.Application.Reports;

/// <summary>
/// How a day's shift entry is shown as attendance, and who belongs to the directorate report.
/// Shared by the individual report (ΠΑΡΟΥΣΙΕΣ column) and the attendance report, so both
/// always show the same mark.
/// </summary>
public static class PresenceRules
{
	/// <summary>Mark of a worked shift.</summary>
	public const string Worked = "√";

	/// <summary>
	/// Work position (Παράμετροι Εργαζομένων → Θέσεις) that sends an employee to the
	/// separate directorate attendance report instead of the staff one.
	/// </summary>
	public const string DirectorPosition = "ΔΙΕΥΘΥΝΤΗΣ";

	/// <summary>
	/// Fallback when nobody has the director's position (e.g. right after a fresh install,
	/// where seeded employees have no position): the head of the directorate by surname.
	/// </summary>
	public const string DirectorLastName = "ΠΑΠΑΣΤΑΜΑΤΗΣ";

	/// <summary>"√" for a work shift; the code itself for a status (leave, rest, holiday…).</summary>
	public static string Mark(string code, bool isWorkShift) => isWorkShift ? Worked : code;

	/// <summary>
	/// True when the employee belongs to the directorate report: the work position is
	/// <see cref="DirectorPosition"/>, or — only when nobody in the list has that position —
	/// the surname is <see cref="DirectorLastName"/>. Compared without accents / case.
	/// </summary>
	/// <param name="lastName">The employee's surname.</param>
	/// <param name="positionName">The employee's work position name, or null when not set.</param>
	/// <param name="anyoneHasPosition">Whether at least one active employee has the director's position.</param>
	public static bool IsDirector(string lastName, string? positionName, bool anyoneHasPosition) =>
		anyoneHasPosition
			? IsDirectorPosition(positionName)
			: Normalize(lastName) == Normalize(DirectorLastName);

	/// <summary>True when the work position is the director's (accent / case insensitive).</summary>
	public static bool IsDirectorPosition(string? positionName) =>
		positionName is not null && Normalize(positionName) == Normalize(DirectorPosition);

	/// <summary>Upper case without accents or surrounding spaces ("Παπασταμάτης " → "ΠΑΠΑΣΤΑΜΑΤΗΣ").</summary>
	private static string Normalize(string text) => GreekText.Upper(text);
}
