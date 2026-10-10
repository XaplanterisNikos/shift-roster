namespace Vardiologio.Application.Auth;

/// <summary>The signed-in user as seen by the app.</summary>
/// <param name="Username">Sign-in name.</param>
/// <param name="DisplayName">Name shown in the app (the username; "Διαχειριστής" for the administrator).</param>
/// <param name="IsAdmin">True for the administrator account.</param>
public record AuthenticatedUser(string Username, string DisplayName, bool IsAdmin);
