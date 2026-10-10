namespace Vardiologio.Application.Auth;

/// <summary>An option for the login dropdown — username + display name, no secrets.</summary>
public record LoginOption(string Username, string DisplayName);

/// <summary>
/// Sign-in and user creation. The users belong to this installation (database); the
/// administrator is the same in every installation and comes from a data file.
/// </summary>
public interface IAuthService
{
	/// <summary>
	/// Accounts to show in the login selector (no password data exposed): the active users by
	/// name, then the administrator when an administrator account is configured.
	/// </summary>
	Task<IReadOnlyList<LoginOption>> GetLoginOptionsAsync();

	/// <summary>Returns the account if the password verifies against the stored hash; otherwise null.</summary>
	Task<AuthenticatedUser?> AuthenticateAsync(string username, string password);

	/// <summary>
	/// True when the name is taken: by an active or inactive user (names are never reused) or by
	/// the administrator. Case-insensitive. Check this before <see cref="CreateUserAsync"/>.
	/// </summary>
	Task<bool> UsernameExistsAsync(string username);

	/// <summary>Creates an active user with the given password (stored as salt + hash only).</summary>
	/// <param name="username">Sign-in name; trimmed before it is stored.</param>
	/// <param name="password">The password as typed.</param>
	Task CreateUserAsync(string username, string password);
}
