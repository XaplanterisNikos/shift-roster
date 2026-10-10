namespace Vardiologio.Application.Auth;

/// <summary>Rules for usernames and passwords, shared by the login screen and the service.</summary>
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
}
