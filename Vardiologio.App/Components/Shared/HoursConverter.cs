using System.Globalization;
using MudBlazor;
using MudBlazor.Utilities.Exceptions;

namespace Vardiologio.App.Components.Shared;

/// <summary>
/// Text of the hours boxes (MudNumericField): shows hours with the decimal separator of
/// Windows (e.g. "6,5") and reads both "6,5" and "6.5", as the plain number boxes did.
/// MudBlazor's own conversion follows one culture only, where the other separator is taken
/// for a thousands separator ("6.5" would become 65).
/// </summary>
public sealed class HoursConverter : IReversibleConverter<decimal?, string?>, IReversibleConverter<decimal, string?>
{
	/// <summary>The single instance (the converter keeps no state).</summary>
	public static readonly HoursConverter Instance = new();

	private HoursConverter() { }

	/// <summary>Hours as text, up to two decimals ("6,5" / "120"); empty for no value.</summary>
	public string? Convert(decimal? input) => input?.ToString("0.##", CultureInfo.CurrentCulture);

	/// <summary>Hours as text, up to two decimals ("6,5" / "120").</summary>
	public string? Convert(decimal input) => input.ToString("0.##", CultureInfo.CurrentCulture);

	/// <summary>Reads what was typed; an empty box is "no value".</summary>
	decimal? IReversibleConverter<decimal?, string?>.ConvertBack(string? input) =>
		string.IsNullOrWhiteSpace(input) ? null : Parse(input);

	/// <summary>Reads what was typed; an empty box counts as 0.</summary>
	decimal IReversibleConverter<decimal, string?>.ConvertBack(string? input) =>
		string.IsNullOrWhiteSpace(input) ? 0 : Parse(input);

	/// <summary>
	/// Parses a number written with a comma or a dot as the decimal separator (no thousands
	/// separators). Anything else is reported under the box by the field.
	/// </summary>
	private static decimal Parse(string text)
	{
		var normalised = text.Trim().Replace(',', '.');
		if (decimal.TryParse(normalised, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
				CultureInfo.InvariantCulture, out var hours))
			return hours;

		throw new ConversionException("Μη έγκυρος αριθμός ωρών.");
	}
}
