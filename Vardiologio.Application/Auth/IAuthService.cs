namespace Vardiologio.Application.Auth;

/// <summary>An option for the login dropdown — username + display name, no secrets.</summary>
public record LoginOption(string Username, string DisplayName);

/// <summary>A user of this installation, as listed for the administrator.</summary>
public record UserItem(int Id, string Username);

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

	/// <summary>
	/// True when the administrator's limit on the number of users has been reached, so no new
	/// user may be created. Check this before <see cref="CreateUserAsync"/>.
	/// </summary>
	Task<bool> UserLimitReachedAsync();

	/// <summary>The users of this installation, by name (administrator's page).</summary>
	Task<IReadOnlyList<UserItem>> GetUsersAsync();

	/// <summary>Deletes a user for good; the activity log keeps the name. No-op when the user is gone.</summary>
	Task DeleteUserAsync(int id);

	/// <summary>Deletes every user of this installation for good.</summary>
	Task DeleteAllUsersAsync();

	/// <summary>The most users this installation may have; null = no limit.</summary>
	Task<int?> GetMaxUsersAsync();

	/// <summary>Sets the most users this installation may have; null = no limit.</summary>
	Task SetMaxUsersAsync(int? max);
}
