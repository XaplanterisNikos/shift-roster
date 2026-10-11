using System.Globalization;
using System.IO.Compression;
using System.Text;
using Vardiologio.Application.Feedback;
using Vardiologio.Application.Logging;
using Vardiologio.Infrastructure.Files;

namespace Vardiologio.Infrastructure.Feedback;

/// <summary>
/// <see cref="IFeedbackSender"/> over the Telegram Bot API: one HTTPS call (sendDocument) posts
/// the zip to the developer's chat with the bot. The user needs no Telegram and no mail program.
/// Settings come from <see cref="TelegramSettingsFile"/>.
/// </summary>
public class TelegramFeedbackSender : IFeedbackSender
{
	/// <summary>Error logs of this many days (today included) go into the zip.</summary>
	private const int LogDays = 7;

	/// <summary>Longest caption Telegram accepts under a document.</summary>
	private const int MaxCaptionLength = 1024;

	/// <summary>
	/// One client for the life of the app (the recommended use of HttpClient). The timeout covers
	/// the whole upload, so a blocked network fails in reasonable time instead of hanging.
	/// </summary>
	private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(30) };

	private readonly IErrorLog _errors;
	private readonly TelegramSettings? _settings = TelegramSettingsFile.Load();

	public TelegramFeedbackSender(IErrorLog errors) => _errors = errors;

	/// <inheritdoc/>
	public async Task<FeedbackResult> SendAsync(FeedbackMessage message)
	{
		string zipPath;
		try
		{
			zipPath = await Task.Run(() => CreateArchive(message));
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			_errors.Error("Message to the developer / zip", ex);
			return new FeedbackResult(false, null);
		}

		// No settings file in this installation: the zip is all there is.
		var settings = _settings;
		if (settings is null) return new FeedbackResult(false, zipPath);

		try
		{
			using var form = new MultipartFormDataContent();
			form.Add(new StringContent(settings.ChatId), "chat_id");
			form.Add(new StringContent(Caption(message)), "caption");

			await using (var file = File.OpenRead(zipPath))
			{
				form.Add(new StreamContent(file), "document", Path.GetFileName(zipPath));

				using var response = await Http.PostAsync(
					$"https://api.telegram.org/bot{settings.BotToken}/sendDocument", form);

				if (!response.IsSuccessStatusCode)
				{
					// Telegram explains the refusal in the response text (e.g. "chat not found").
					var reason = await response.Content.ReadAsStringAsync();
					_errors.Error("Message to the developer / send",
						new InvalidOperationException($"Telegram answered {(int)response.StatusCode}: {reason}"));
					return new FeedbackResult(false, zipPath);
				}
			}

			// Delivered: the copy on disk is no longer needed. A failed delete is harmless.
			try { File.Delete(zipPath); } catch (IOException) { } catch (UnauthorizedAccessException) { }
			return new FeedbackResult(true, null);
		}
		catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException)
		{
			// No internet, blocked address or timeout. Only type and message are logged, with the
			// token masked: the request address holds it and the logs travel in later messages.
			_errors.Error("Message to the developer / send",
				new InvalidOperationException($"{ex.GetType().Name}: {ex.Message.Replace(settings.BotToken, "***")}"));
			return new FeedbackResult(false, zipPath);
		}
	}

	/// <summary>
	/// Writes Documents\Vardiologio\Feedback\yyyy-MM-dd_HHmmss.zip: "message.txt" and, under
	/// "Logs", the daily error logs of the last <see cref="LogDays"/> days.
	/// </summary>
	/// <returns>Full path of the zip.</returns>
	private static string CreateArchive(FeedbackMessage message)
	{
		var now = DateTime.Now;
		var zipPath = Path.Combine(AppFolders.Feedback, $"{now:yyyy-MM-dd_HHmmss}.zip");
		File.Delete(zipPath);   // same second twice: replace (no error when it does not exist)

		using var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create);

		var entry = zip.CreateEntry("message.txt", CompressionLevel.Optimal);
		// UTF-8 with BOM, so Notepad shows the Greek text correctly.
		using (var writer = new StreamWriter(entry.Open(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true)))
			writer.Write(FullText(message, now));

		// Log files are named yyyy-MM-dd.log (see ErrorLogFile); anything else in the folder is skipped.
		var firstDay = now.Date.AddDays(-(LogDays - 1));
		foreach (var log in Directory.EnumerateFiles(AppFolders.Logs, "*.log"))
		{
			var named = DateTime.TryParseExact(Path.GetFileNameWithoutExtension(log), "yyyy-MM-dd",
				CultureInfo.InvariantCulture, DateTimeStyles.None, out var day);
			if (!named || day < firstDay) continue;

			// Read with sharing: today's log may be written to at this very moment.
			using var source = new FileStream(log, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			using var target = zip.CreateEntry($"Logs/{Path.GetFileName(log)}", CompressionLevel.Optimal).Open();
			source.CopyTo(target);
		}

		return zipPath;
	}

	/// <summary>Content of message.txt: who, about what, when, from which computer, and the text.</summary>
	private static string FullText(FeedbackMessage m, DateTime now) =>
		$"{m.Kind} - {m.Menu}{Environment.NewLine}" +
		$"Από: {m.SenderName}{Environment.NewLine}" +
		$"Ημερομηνία: {now:dd/MM/yyyy HH:mm}{Environment.NewLine}" +
		$"Έκδοση: {m.AppVersion}{Environment.NewLine}" +
		$"Υπολογιστής: {Environment.MachineName}{Environment.NewLine}" +
		$"{Environment.NewLine}{m.Text}{Environment.NewLine}";

	/// <summary>
	/// Text shown under the file in the chat: subject, sender, version and the message, cut to
	/// Telegram's limit (the whole text is always in message.txt).
	/// </summary>
	private static string Caption(FeedbackMessage m)
	{
		var caption = $"{m.Kind} - {m.Menu}\n{m.SenderName} · v{m.AppVersion}\n\n{m.Text}";
		return caption.Length <= MaxCaptionLength ? caption : caption[..(MaxCaptionLength - 1)] + "…";
	}
}
