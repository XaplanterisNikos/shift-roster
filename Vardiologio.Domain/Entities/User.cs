namespace Vardiologio.Domain.Entities;

/// <summary>
/// A person who signs in to this installation. Created from the login screen; the
/// administrator is not a row here (see AdminAccountFile in Infrastructure).
/// </summary>
public class User
{
	/// <summary>Primary key.</summary>
	public int Id { get; set; }

	/// <summary>Sign-in name, also shown in the app. Unique, never reused once created.</summary>
	public string Username { get; set; } = null!;

	/// <summary>Random PBKDF2 salt of the password, Base64.</summary>
	public string PasswordSalt { get; set; } = null!;

	/// <summary>PBKDF2 hash of the password, Base64. The password itself is never stored.</summary>
	public string PasswordHash { get; set; } = null!;

	/// <summary>
	/// Soft-delete flag. An inactive user cannot sign in and is not listed on the login
	/// screen, but the row stays so the name remains known. Defaults to <c>true</c>.
	/// </summary>
	public bool IsActive { get; set; } = true;
}
