using System.Text.Json;
using Vardiologio.Application.ShiftCodes;
using Vardiologio.Infrastructure.Persistence;

namespace Vardiologio.Infrastructure.ShiftCodes;

/// <summary>
/// Keeps the user's order of the shift codes in "shift-code-order.json" next to the database
/// (%LOCALAPPDATA%\Vardiologio), so it survives reinstalls like the database does. The file
/// belongs to this computer: it is not part of the database, so a database export / import
/// does not carry it.
/// </summary>
public sealed class ShiftCodeOrderFile : IShiftCodeOrderStore
{
	private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

	/// <inheritdoc/>
	public IReadOnlyList<string> Load()
	{
		var path = AppPaths.ShiftCodeOrderPath;
		if (!File.Exists(path)) return [];

		try
		{
			return JsonSerializer.Deserialize<List<string>>(File.ReadAllText(path)) ?? [];
		}
		catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
		{
			return [];
		}
	}

	/// <inheritdoc/>
	public void Save(IReadOnlyList<string> codes) =>
		File.WriteAllText(AppPaths.ShiftCodeOrderPath, JsonSerializer.Serialize(codes, Options));

	/// <inheritdoc/>
	public void Reset() => File.Delete(AppPaths.ShiftCodeOrderPath);
}
