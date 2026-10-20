using System.Globalization;
using Vardiologio.Application.Logging;
using Vardiologio.Infrastructure.Files;

namespace Vardiologio.Infrastructure.Logging;

/// <summary>
/// <see cref="IErrorLogReader"/> over the files written by <see cref="ErrorLogFile"/>: one file
/// per day (yyyy-MM-dd.log); an entry is a "time | source" line, the exception text and a line
/// of 80 dashes.
/// </summary>
public class ErrorLogReader : IErrorLogReader
{
	/// <summary>The line that closes every entry (same as in <see cref="ErrorLogFile"/>).</summary>
	private static readonly string Separator = new('-', 80);

	/// <inheritdoc/>
	public Task<IReadOnlyList<ErrorLogItem>> ReadAsync(DateTime? from, DateTime? to) => Task.Run(() =>
	{
		var items = new List<ErrorLogItem>();

		foreach (var file in Directory.GetFiles(AppFolders.Logs, "*.log"))
		{
			// The file name is the day; files with another name are not ours.
			if (!DateTime.TryParseExact(Path.GetFileNameWithoutExtension(file), "yyyy-MM-dd",
				    CultureInfo.InvariantCulture, DateTimeStyles.None, out var day)) continue;
			if (from is not null && day < from.Value.Date) continue;
			if (to is not null && day > to.Value.Date) continue;

			items.AddRange(Parse(ReadShared(file), day));
		}

		return (IReadOnlyList<ErrorLogItem>)items.OrderByDescending(i => i.Time).ToList();
	});

	/// <summary>Reads a file that the application may be appending to at the same moment.</summary>
	private static string[] ReadShared(string path)
	{
		using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
		using var reader = new StreamReader(stream);
		return reader.ReadToEnd().Split('\n').Select(l => l.TrimEnd('\r')).ToArray();
	}

	/// <summary>Splits the lines of one day's file into entries.</summary>
	private static IEnumerable<ErrorLogItem> Parse(string[] lines, DateTime day)
	{
		var block = new List<string>();
		foreach (var line in lines)
		{
			if (line != Separator) { block.Add(line); continue; }

			if (block.Count > 0) yield return ToItem(block, day);
			block = new List<string>();
		}
		// An entry cut short (no closing line) is still shown.
		if (block.Any(l => l.Length > 0)) yield return ToItem(block, day);
	}

	/// <summary>Builds an entry from its lines: header "yyyy-MM-dd HH:mm:ss | source", then the exception.</summary>
	private static ErrorLogItem ToItem(List<string> block, DateTime day)
	{
		var header = block[0];
		var cut = header.IndexOf(" | ", StringComparison.Ordinal);

		// A header that does not parse keeps the day of the file and the whole line as the source.
		var time = day;
		var source = header;
		if (cut > 0 && DateTime.TryParseExact(header[..cut], "yyyy-MM-dd HH:mm:ss",
			    CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
		{
			time = parsed;
			source = header[(cut + 3)..];
		}

		var body = block.Skip(1).ToList();
		return new ErrorLogItem(time, source, body.FirstOrDefault(l => l.Length > 0) ?? "", string.Join(Environment.NewLine, body).Trim());
	}
}
