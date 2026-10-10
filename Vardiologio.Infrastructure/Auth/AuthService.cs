using Microsoft.EntityFrameworkCore;
using Vardiologio.Application.Auth;
using Vardiologio.Domain.Entities;
using Vardiologio.Infrastructure.Persistence;

namespace Vardiologio.Infrastructure.Auth;

/// <summary>
/// <see cref="IAuthService"/> over the Users table (<see cref="PasswordHasher"/> for the
/// passwords) plus the administrator account of <see cref="AdminAccountFile"/>.
/// </summary>
/// <remarks>
/// Users has no global IsActive query filter (see AppDbContext), so "active only" is always an
/// explicit Where(x => x.IsActive) here.
/// </remarks>
public class AuthService : IAuthService
{
	private readonly IDbContextFactory<AppDbContext> _factory;

	/// <summary>Administrator account, read once; null when no account file is usable.</summary>
	private readonly AdminAccount? _admin = AdminAccountFile.Load();

	public AuthService(IDbContextFactory<AppDbContext> factory) => _factory = factory;

	/// <inheritdoc/>
	public async Task<IReadOnlyList<LoginOption>> GetLoginOptionsAsync()
	{
		await using var db = await _factory.CreateDbContextAsync();

		var names = await db.Users.Where(u => u.IsActive).Select(u => u.Username).ToListAsync();

		// Sorted in memory: SQLite's own ordering is not case-insensitive for Greek letters.
		var options = names
			.OrderBy(n => n, StringComparer.CurrentCultureIgnoreCase)
			.Select(n => new LoginOption(n, n))
			.ToList();

		if (_admin is not null)
			options.Add(new LoginOption(UserRules.AdminUsername, UserRules.AdminDisplayName));

		return options;
	}

	/// <inheritdoc/>
	public async Task<AuthenticatedUser?> AuthenticateAsync(string username, string password)
	{
		if (IsAdminName(username))
		{
			return _admin is not null && PasswordHasher.Verify(password, _admin.Salt, _admin.Hash)
				? new AuthenticatedUser(UserRules.AdminUsername, UserRules.AdminDisplayName, IsAdmin: true)
				: null;
		}

		await using var db = await _factory.CreateDbContextAsync();

		// The login screen sends the stored name back, so an exact match is enough.
		var user = await db.Users.FirstOrDefaultAsync(u => u.IsActive && u.Username == username);
		if (user is null) return null;

		return PasswordHasher.Verify(password, user.PasswordSalt, user.PasswordHash)
			? new AuthenticatedUser(user.Username, user.Username, IsAdmin: false)
			: null;
	}

	/// <inheritdoc/>
	public async Task<bool> UsernameExistsAsync(string username)
	{
		var name = username.Trim();
		if (IsAdminName(name)) return true;

		await using var db = await _factory.CreateDbContextAsync();

		// Active AND inactive rows: the unique index covers both and names are never reused.
		// Compared in memory, because SQLite compares Greek letters case-sensitively.
		var names = await db.Users.Select(u => u.Username).ToListAsync();
		return names.Any(n => string.Equals(n, name, StringComparison.CurrentCultureIgnoreCase));
	}

	/// <inheritdoc/>
	public async Task CreateUserAsync(string username, string password)
	{
		var (salt, hash) = PasswordHasher.Hash(password);

		await using var db = await _factory.CreateDbContextAsync();

		db.Users.Add(new User
		{
			Username = username.Trim(),
			PasswordSalt = salt,
			PasswordHash = hash
		});
		await db.SaveChangesAsync();
	}

	/// <summary>True for the reserved administrator name, in any letter case.</summary>
	private static bool IsAdminName(string username) =>
		string.Equals(username.Trim(), UserRules.AdminUsername, StringComparison.OrdinalIgnoreCase);
}
