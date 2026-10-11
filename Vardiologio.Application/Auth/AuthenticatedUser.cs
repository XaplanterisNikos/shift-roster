namespace Vardiologio.Application.Auth;

/// <summary>The signed-in user as seen by the app.</summary>
/// <param name="Username">Sign-in name.</param>
/// <param name="DisplayName">Name shown in the app (the username; "Διαχειριστής" for the administrator).</param>
/// <param name="IsAdmin">True for the administrator account.</param>
/// <param name="FullName">The person's full name; "" for the administrator and for users without one.</param>
/// <param name="Email">The person's e-mail address; "" for the administrator and for users without one.</param>
public record AuthenticatedUser(string Username, string DisplayName, bool IsAdmin, string FullName = "", string Email = "");
