using Vardiologio.Application.Auth;

namespace Vardiologio.Infrastructure.Auth;

/// <summary>Authenticates against the hard-coded <see cref="UserStore"/> using <see cref="PasswordHasher"/>.</summary>
public class AuthService : IAuthService
{
	/// <inheritdoc/>
	public AuthenticatedUser? Authenticate(string username, string password)
	{
		var u = UserStore.Users.FirstOrDefault(x =>
			string.Equals(x.Username, username, StringComparison.OrdinalIgnoreCase));

		if (u is null) return null;

		return PasswordHasher.Verify(password, u.Salt, u.Hash)
			? new AuthenticatedUser(u.Username, u.DisplayName, u.Code)
			: null;
	}

	/// <inheritdoc/>
	public IReadOnlyList<LoginOption> GetLoginOptions() =>
	UserStore.Users.Select(u => new LoginOption(u.Username, u.DisplayName)).ToList();
}