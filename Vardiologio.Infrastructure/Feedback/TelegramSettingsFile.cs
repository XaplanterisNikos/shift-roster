using System.Text.Json;

namespace Vardiologio.Infrastructure.Feedback;

/// <summary>Where the messages to the developer go: a Telegram bot and the chat it writes to.</summary>
/// <param name="BotToken">Secret token of the bot, as given by @BotFather.</param>
/// <param name="ChatId">Numeric id of the developer's chat with the bot.</param>
internal sealed record TelegramSettings(string BotToken, string ChatId);

/// <summary>
/// Reads the Telegram settings from "telegram.json" next to the executable, so the bot token is
/// not part of the source code. The file is git-ignored: present only on the maintainer's machine
/// and inside the installer. The committed "telegram.demo.json" only shows its shape and is never
/// read; without the real file messages are not sent and are kept as a zip instead.
/// </summary>
internal static class TelegramSettingsFile
{
	/// <summary>Real settings: git-ignored.</summary>
	public const string FileName = "telegram.json";

	/// <summary>Reads the file. Returns null when it is absent, unreadable or incomplete.</summary>
	public static TelegramSettings? Load()
	{
		// JSON files are copied next to the executable (see the copy rules in Vardiologio.App.csproj).
		var path = Path.Combine(AppContext.BaseDirectory, FileName);
		if (!File.Exists(path)) return null;

		try
		{
			// Case-insensitive so camelCase JSON maps to the PascalCase record.
			var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
			var settings = JsonSerializer.Deserialize<TelegramSettings>(File.ReadAllText(path), options);

			if (settings is null || string.IsNullOrWhiteSpace(settings.BotToken) || string.IsNullOrWhiteSpace(settings.ChatId))
				return null;
			return settings with { BotToken = settings.BotToken.Trim(), ChatId = settings.ChatId.Trim() };
		}
		catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
		{
			return null;   // unreadable or malformed file = sending is not configured
		}
	}
}
