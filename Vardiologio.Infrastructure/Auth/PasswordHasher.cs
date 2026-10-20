using System.Security.Cryptography;
using System.Text;

namespace Vardiologio.Infrastructure.Auth;

/// <summary>
/// Password hashing/verification using PBKDF2 (SHA-256, 100k iterations, per-password salt).
/// Note: for a hard-coded desktop login the hash is mainly to avoid storing plain text —
/// a determined attacker can still extract it from the binary.
/// </summary>
public static class PasswordHasher
{
	private const int Iterations = 100_000;// PBKDF2 work factor
	private const int SaltSize = 16;// bytes of random salt
	private const int HashSize = 32;// bytes of derived key

	/// <summary>
	/// Hashes a password with a fresh random salt. Returns both as Base64.
	/// Use this to generate the Salt/Hash you paste into the user store when changing a password.
	/// </summary>
	public static (string Salt, string Hash) Hash(string password)
	{
		var salt = RandomNumberGenerator.GetBytes(SaltSize);
		var hash = Rfc2898DeriveBytes.Pbkdf2(
			Encoding.UTF8.GetBytes(password), salt, Iterations, HashAlgorithmName.SHA256, HashSize);
		return (Convert.ToBase64String(salt), Convert.ToBase64String(hash));
	}

	/// <summary>
	/// Recomputes the hash from the given salt and compares it in constant time
	/// (FixedTimeEquals) to avoid timing attacks.
	/// </summary>
	public static bool Verify(string password, string saltB64, string hashB64)
	{
		var salt = Convert.FromBase64String(saltB64);
		var expected = Convert.FromBase64String(hashB64);
		var actual = Rfc2898DeriveBytes.Pbkdf2(
			Encoding.UTF8.GetBytes(password), salt, Iterations, HashAlgorithmName.SHA256, HashSize);
		return CryptographicOperations.FixedTimeEquals(actual, expected);
	}
}