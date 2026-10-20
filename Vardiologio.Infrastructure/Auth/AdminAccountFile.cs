using System.Text.Json;

namespace Vardiologio.Infrastructure.Auth;

/// <summary>Salt and hash of the administrator's password, Base64 (see <see cref="PasswordHasher"/>).</summary>
internal sealed record AdminAccount(string Salt, string Hash);

/// <summary>
/// Reads the administrator's password hash from a JSON file next to the executable, so it is
/// not part of the source code. Prefers the real file "admin.json" (git-ignored: present only on
/// the maintainer's machine and inside the installer); when it is absent (e.g. a fresh clone
/// from GitHub) it falls back to the committed "admin.demo.json", whose password is public.
/// The file holds only salt and hash, never the password. Create it with tools/New-AdminFile.ps1.
/// </summary>
internal static class AdminAccountFile
{
	/// <summary>Real account: git-ignored.</summary>
	public const string RealFileName = "admin.json";

	/// <summary>Committed fallback with a public demo password.</summary>
	public const string DemoFileName = "admin.demo.json";

	/// <summary>
	/// Reads the real file, or the demo file when the real one is absent. Returns null when no
	/// usable file exists; the app then simply has no administrator account.
	/// </summary>
	public static AdminAccount? Load()
	{
		// JSON files are copied next to the executable (see the copy rules in Vardiologio.App.csproj).
		var dir = AppContext.BaseDirectory;
		var realPath = Path.Combine(dir, RealFileName);
		var path = File.Exists(realPath) ? realPath : Path.Combine(dir, DemoFileName);
		if (!File.Exists(path)) return null;

		try
		{
			// Case-insensitive so camelCase JSON maps to the PascalCase record.
			var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
			var account = JsonSerializer.Deserialize<AdminAccount>(File.ReadAllText(path), options);

			// Both values must be valid Base64, otherwise verification would throw at sign-in.
			if (account is null || string.IsNullOrWhiteSpace(account.Salt) || string.IsNullOrWhiteSpace(account.Hash))
				return null;
			Convert.FromBase64String(account.Salt);
			Convert.FromBase64String(account.Hash);
			return account;
		}
		catch (Exception ex) when (ex is JsonException or FormatException or IOException)
		{
			return null;   // unreadable or malformed file = no administrator account
		}
	}
}
