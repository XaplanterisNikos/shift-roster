namespace Vardiologio.Application.Auth;

/// <summary>
/// The signed-in user as seen by the app. <see cref="Code"/> is a stable identity
/// tag (e.g. "Χ1") that will later label backups by who created them.
/// </summary>
public record AuthenticatedUser(string Username, string DisplayName, string Code);