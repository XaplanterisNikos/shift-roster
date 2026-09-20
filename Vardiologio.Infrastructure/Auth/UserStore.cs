namespace Vardiologio.Infrastructure.Auth;

/// <summary>One hard-coded user: identity + Base64 PBKDF2 salt/hash of the password.</summary>
internal record StoredUser(string Username, string DisplayName, string Code, string Salt, string Hash);

/// <summary>
/// The fixed set of application users. To change a password, generate a new
/// Salt/Hash with PasswordHasher.Hash("...") and paste the two Base64 values here.
/// </summary>
internal static class UserStore
{
	public static readonly StoredUser[] Users =
	{
		new("user1", "Χρήστης 1", "Χ1",
			"A3TpFEP8foe1J38G1QE3uQ==",
			"rvayCLqAgND94joZg9IR+tmufug+xGfIAVdOk/cTaBY="),   // password: 1111 (demo — change before real use)

        new("user2", "Χρήστης 2", "Χ2",
			"+4bzyhryHeCltk19Ci03iQ==",
			"VG+5GIvHleLIbGl1swuimEL9jOBC7orljrKFjjnQY1U="),   // password: 2222 (demo — change before real use
    };
}