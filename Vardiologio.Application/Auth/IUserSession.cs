namespace Vardiologio.Application.Auth;

/// <summary>
/// Holds the currently signed-in user for the app's lifetime (single desktop session).
/// Registered as a singleton; UI components subscribe to <see cref="Changed"/> to refresh.
/// </summary>
public interface IUserSession
{
	/// <summary>The signed-in user, or null when nobody is logged in.</summary>
	AuthenticatedUser? Current { get; }

	/// <summary>True when a user is signed in.</summary>
	bool IsAuthenticated { get; }

	/// <summary>Raised whenever the session changes (sign-in / sign-out) so the UI can re-render.</summary>
	event Action? Changed;

	/// <summary>Sets the current user and notifies subscribers.</summary>
	void SignIn(AuthenticatedUser user);

	/// <summary>Clears the current user and notifies subscribers.</summary>
	void SignOut();
}