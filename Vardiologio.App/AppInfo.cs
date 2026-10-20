namespace Vardiologio.App;

/// <summary>
/// Facts about the application shown in more than one place (window title, login screen,
/// "about" card): its version and its creator. One place to change them.
/// </summary>
public static class AppInfo
{
	/// <summary>Name of the application in the window title.</summary>
	public const string Name = "Shift-Roster";

	/// <summary>
	/// Release number, e.g. "0.7.0" — from &lt;Version&gt; in Vardiologio.App.csproj (major.minor.patch).
	/// </summary>
	public static readonly string Version =
		typeof(AppInfo).Assembly.GetName().Version?.ToString(3) ?? "";

	/// <summary>Window title: name and running version ("Shift-Roster v0.7.0").</summary>
	public static string WindowTitle => Version.Length > 0 ? $"{Name} v{Version}" : Name;

	/// <summary>Creator of the application.</summary>
	public const string AuthorName = "Ξαπλαντέρης Νίκος";

	/// <summary>Creator's e-mail address.</summary>
	public const string AuthorEmail = "xaplanterisnikos@gmail.com";

	/// <summary>Creator's LinkedIn profile.</summary>
	public const string LinkedInUrl = "https://www.linkedin.com/in/nick-xaplanteris/";

	/// <summary>Creator's GitHub profile.</summary>
	public const string GitHubUrl = "https://github.com/XaplanterisNikos";
}
