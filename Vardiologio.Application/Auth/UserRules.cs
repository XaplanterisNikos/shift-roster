using System.Text.RegularExpressions;

namespace Vardiologio.Application.Auth;

/// <summary>
/// Rules for usernames, passwords and contact details, shared by the login screen, the
/// user's own details, the message form and the service.
/// </summary>
public static class UserRules
{
	/// <summary>Sign-in name of the administrator; reserved, no user can take it.</summary>
	public const string AdminUsername = "admin";

	/// <summary>Name the administrator is shown with.</summary>
	public const string AdminDisplayName = "Διαχειριστής";

	/// <summary>Longest username (also the column size).</summary>
	public const int MaxUsernameLength = 40;

	/// <summary>Shortest password a new user may choose.</summary>
	public const int MinPasswordLength = 6;

	/// <summary>Longest full name (also the column size).</summary>
	public const int MaxFullNameLength = 80;

	/// <summary>Longest e-mail address (also the column size).</summary>
	public const int MaxEmailLength = 100;

	/// <summary>
	/// Shape of an e-mail address: something, "@", a domain with at least one dot and an ending
	/// of two or more characters; no spaces and a single "@".
	/// </summary>
	private static readonly Regex EmailPattern = new(@"^[^@\s]+@[^@\s.]+(\.[^@\s.]+)*\.[^@\s.]{2,}$");

	/// <summary>
	/// True when the text has the shape of an e-mail address (surrounding spaces ignored).
	/// It catches typing slips only: whether the address exists cannot be known here.
	/// </summary>
	public static bool IsValidEmail(string email) => EmailPattern.IsMatch(email.Trim());
}
