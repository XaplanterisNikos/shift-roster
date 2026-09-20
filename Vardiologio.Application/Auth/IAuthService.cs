namespace Vardiologio.Application.Auth;

/// <summary>An option for the login dropdown — username + display name, no secrets.</summary>
public record LoginOption(string Username, string DisplayName);

/// <summary>Authentication against the fixed, hard-coded user list.</summary>
public interface IAuthService
{
	/// <summary>Users to show in the login selector (no password data exposed).</summary>
	IReadOnlyList<LoginOption> GetLoginOptions();

	/// <summary>Returns the user if the password verifies against the stored hash; otherwise null.</summary>
	AuthenticatedUser? Authenticate(string username, string password);
}