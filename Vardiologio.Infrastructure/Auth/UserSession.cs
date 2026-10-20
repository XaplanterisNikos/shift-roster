using Vardiologio.Application.Auth;

namespace Vardiologio.Infrastructure.Auth;

/// <summary>In-memory single-session implementation of <see cref="IUserSession"/> (registered as a singleton).</summary>
public class UserSession : IUserSession
{
	/// <inheritdoc/>
	public AuthenticatedUser? Current { get; private set; }

	/// <inheritdoc/>
	public bool IsAuthenticated => Current is not null;

	/// <inheritdoc/>
	public event Action? Changed;

	/// <inheritdoc/>
	public void SignIn(AuthenticatedUser user) { Current = user; Changed?.Invoke(); }

	/// <inheritdoc/>
	public void SignOut() { Current = null; Changed?.Invoke(); }
}