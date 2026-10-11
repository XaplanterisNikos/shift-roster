namespace Vardiologio.Application.Feedback;

/// <summary>A message from a user to the developer.</summary>
/// <param name="Kind">Kind of message, e.g. "Σφάλμα" or "Προσθήκη".</param>
/// <param name="Menu">The menu the message is about, e.g. "Αναφορές - Ατομική Μηνιαία".</param>
/// <param name="SenderName">Name typed by the sender.</param>
/// <param name="Text">The message itself.</param>
/// <param name="AppVersion">Running version of the application, e.g. "1.20.0".</param>
public record FeedbackMessage(string Kind, string Menu, string SenderName, string Text, string AppVersion);

/// <summary>Outcome of <see cref="IFeedbackSender.SendAsync"/>.</summary>
/// <param name="Sent">True when the message reached the developer.</param>
/// <param name="ArchivePath">
/// Full path of the zip (message + error logs) left on disk for the user to send by hand;
/// null when the message was sent, or when not even the zip could be written.
/// </param>
public record FeedbackResult(bool Sent, string? ArchivePath);

/// <summary>
/// Delivers a user's message to the developer together with the recent error logs, without
/// depending on any mail program of the computer.
/// </summary>
public interface IFeedbackSender
{
	/// <summary>
	/// Packs the message and the recent error logs into a zip and sends it. Never throws: when
	/// sending is not possible (no internet, blocked, not configured) the zip stays in
	/// Documents\Vardiologio\Feedback and its path is returned.
	/// </summary>
	/// <param name="message">The message to deliver.</param>
	Task<FeedbackResult> SendAsync(FeedbackMessage message);
}
